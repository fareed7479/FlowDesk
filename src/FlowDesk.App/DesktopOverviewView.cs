using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
public sealed class DesktopOverviewView : UserControl,IDisposable {
    private readonly WindowService service;
    private readonly Action<string> report;
    private readonly Func<bool> blocked;
    private readonly WorkspaceController controller;
    internal readonly TextBox Search=new(){PlaceholderText="Search windows or applications",Header="Find a window"};
    internal readonly ComboBox Monitor=new(){Header="Display",MinWidth=230};
    private readonly StackPanel cards=new(){Spacing=18};
    private readonly StackPanel dock=new(){Orientation=Orientation.Horizontal,Spacing=8};
    private readonly TextBlock summary=Design.Caption("Discovering your desktop…");
    private readonly Dictionary<string,int> cycle=new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<DesktopWindow> inventory=Array.Empty<DesktopWindow>();
    private readonly DispatcherTimer debounce=new(){Interval=TimeSpan.FromMilliseconds(650)};
    private readonly WindowEvents events=new();
    private readonly List<LivePreview> previews=new();
    private DockPreferences? preferences;
    private readonly string preferencesPath=Path.Combine(Program.DataDirectory,"dock.json");
    private FloatingDock? floating;
    private bool refreshing,dirty,disposed;
    internal int VisibleCount {get;private set;}
    internal DesktopWindow? Selected {get;private set;}
    internal int RegisteredPreviews=>previews.Count(p=>p.Registered);
    internal FloatingDock? Dock=>floating;
    internal readonly ListView Pins=new(){Height=100,SelectionMode=ListViewSelectionMode.Single};
    internal readonly ToggleSwitch AutoHide=new(){Header="Auto-hide floating dock"};
    internal readonly Dictionary<string,Button> Buttons=new();
    public DesktopOverviewView(WindowService service,Action<string> report,Func<bool>? blocked=null) {
        this.service=service;this.report=report;this.blocked=blocked??(()=>false);controller=new(service);
        var panel=new StackPanel{Spacing=16};
        panel.Children.Add(Design.Heading("Overview"));panel.Children.Add(Design.Caption(""));
        var filters=new Grid{ColumnSpacing=16};
        filters.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});filters.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        filters.Children.Add(Search);Grid.SetColumn(Monitor,1);filters.Children.Add(Monitor);panel.Children.Add(filters);
        Search.TextChanged+=(_,_)=>Render();Monitor.SelectionChanged+=(_,_)=>Render();
        panel.Children.Add(summary);panel.Children.Add(cards);
        var toolbar=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Add(toolbar,"Refresh overview & dock",async()=>await Refresh());
        Add(toolbar,"Move selected to display",()=>{if(this.blocked())throw new InvalidOperationException("End focus/fullscreen before moving windows.");var window=Selected??throw new InvalidOperationException("Choose a window card first.");var display=Monitor.SelectedItem as DesktopDisplay??throw new InvalidOperationException("Choose a target display in the filter.");var bounds=Layouts.Clamp(window.Bounds,display.WorkArea);bounds=bounds with {X=display.WorkArea.X+(display.WorkArea.Width-bounds.Width)/2,Y=display.WorkArea.Y+(display.WorkArea.Height-bounds.Height)/2};var result=controller.Apply(new RestorePlan([new(window,bounds)],0),false);report($"Display move requests: {result.Applied}. "+string.Join(" ",result.Errors));return Task.CompletedTask;});
        Add(toolbar,"Undo display move",()=>{if(this.blocked())throw new InvalidOperationException("Leave Stage and end focus/fullscreen before undoing display moves.");var r=controller.Undo();report($"Restored {r.Applied} window(s).");return Task.CompletedTask;});panel.Children.Add(toolbar);
        panel.Children.Add(Design.Heading("FlowDock",22));panel.Children.Add(Design.Caption("Pin an app from a window card. Running apps cycle through their windows; a closed pinned app launches only when you click it."));
        panel.Children.Add(new ScrollViewer{Content=dock,HorizontalScrollMode=ScrollMode.Enabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollMode=ScrollMode.Disabled,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled});
        var dockActions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Add(dockActions,"Show floating dock",()=>{if(preferences is null)throw new InvalidOperationException("Repair dock preferences before using the floating dock.");if(floating is null){var selectedDisplay=Monitor.SelectedItem as DesktopDisplay;SavePreferences(preferences with {Monitor=selectedDisplay?.Device});floating=new FloatingDock(service,()=>preferences!,report,selectedDisplay);floating.Closed+=(_,_)=>floating=null;}floating.ShowDock();return Task.CompletedTask;});
        Add(dockActions,"Hide floating dock",()=>{floating?.Close();return Task.CompletedTask;});
        var autoHide=AutoHide;
        try{preferences=DockPreferences.Load(preferencesPath);autoHide.IsOn=preferences.AutoHide;}catch(Exception ex){report("Dock preferences preserved: "+ex.Message);}
        autoHide.Toggled+=(_,_)=>{try{SavePreferences((preferences??throw new InvalidOperationException("Dock storage unavailable.")) with {AutoHide=autoHide.IsOn});}catch(Exception ex){report(ex.Message);}};
        panel.Children.Add(dockActions);panel.Children.Add(autoHide);panel.Children.Add(Design.Caption("Pinned applications — select a pin to remove it, including apps that are closed or moved."));panel.Children.Add(Pins);
        var pinActions=new StackPanel{Orientation=Orientation.Horizontal};Add(pinActions,"Unpin selected app",()=>{var path=Pins.SelectedItem as string??throw new InvalidOperationException("Select a pinned app first.");SavePreferences(preferences!.Unpin(path));Render();return Task.CompletedTask;});panel.Children.Add(pinActions);
        Content=Design.Page(panel,"A clearer view of your desktop","Live window previews, display-aware organization, and a dock that stays within reach.","MISSION CONTROL");
        events.Changed+=()=>dirty=true;
        debounce.Tick+=async(_,_)=>{if(dirty && IsLoaded && !refreshing){dirty=false;await Refresh();}};debounce.Start();
        Loaded+=async(_,_)=>await Refresh();Unloaded+=(_,_)=>{foreach(var p in previews)p.Dispose();};
    }
    private void Add(StackPanel panel,string name,Func<Task> action){var b=new Button{Content=name};Design.Name(b,name);b.Click+=async(_,_)=>{try{await action();}catch(Exception ex){report(ex.Message);}};Buttons[name]=b;panel.Children.Add(b);}
    internal async Task Refresh() {
        if(refreshing||disposed)return;refreshing=true;
        try {
            var result=await Task.Run(()=> (Windows:service.Enumerate(),Displays:service.Displays()));
            if(disposed)return;inventory=result.Windows;
            var selected=Monitor.SelectedItem is DesktopDisplay selectedDisplay?selectedDisplay.Device:Monitor.SelectedItem is null?preferences?.Monitor:null;
            Monitor.Items.Clear();Monitor.Items.Add("All displays");foreach(var display in result.Displays)Monitor.Items.Add(display);
            Monitor.SelectedItem=Monitor.Items.OfType<DesktopDisplay>().FirstOrDefault(d=>d.Device==selected)??(object)"All displays";
            Render();RenderDock();floating?.Refresh();
        }catch(Exception ex){report(ex.Message);}finally{refreshing=false;}
    }
    private void Activate(DesktopWindow w){try{report(service.Activate(w)?"Window activated.":"Windows declined foreground activation; select the app from the taskbar.");}catch(Exception ex){report(ex.Message);}}
    private void SavePreferences(DockPreferences next){next.Save(preferencesPath);preferences=next;RenderDock();floating?.Refresh();}
    private void Render() {
        foreach(var p in previews)p.Dispose();previews.Clear();cards.Children.Clear();foreach(var key in Buttons.Keys.Where(k=>k.StartsWith("Select ") || k.StartsWith("Pin ") || k.StartsWith("Open ")).ToArray())Buttons.Remove(key);
        var term=Search.Text.Trim();var display=Monitor.SelectedItem as DesktopDisplay;
        var shown=inventory.Where(w=>w.Title.Contains(term,StringComparison.OrdinalIgnoreCase)||Path.GetFileNameWithoutExtension(w.ProcessPath).Contains(term,StringComparison.OrdinalIgnoreCase)).Where(w=>display is null || service.DisplayFor(w)?.Device==display.Device).OrderBy(w=>w.ProcessPath).ThenBy(w=>w.Title).ToArray();
        VisibleCount=shown.Length;summary.Text=$"{shown.Length} matching windows · {service.Displays().Count} display(s) · live previews"+(events.Available?" · automatically updated":" · manual refresh");
        if(shown.Length==0){cards.Children.Add(Design.Caption("No matching windows. Try a different search or display."));return;}
        foreach(var group in shown.Take(36).GroupBy(w=>service.DisplayFor(w)?.Device??"Display unavailable")) {
            cards.Children.Add(Design.Heading(group.Key.Replace(@"\\.\",""),16));var grid=new Grid{ColumnSpacing=14,RowSpacing=14};
            for(int i=0;i<2;i++)grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            int index=0;
            foreach(var w in group) {
                if(index%2==0)grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
                var stack=new StackPanel{Spacing=8};var preview=new LivePreview(w.Handle);previews.Add(preview);stack.Children.Add(preview);
                stack.Children.Add(Design.Caption(Path.GetFileNameWithoutExtension(w.ProcessPath).ToUpperInvariant()));stack.Children.Add(Design.Heading(w.Title,16));
                var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=6};
                var choose=new Button{Content="Select"};choose.Click+=(_,_)=>{Selected=w;summary.Text=$"Selected: {w.Title}. Choose a display to move it.";};actions.Children.Add(choose);Buttons["Select "+w.Handle]=choose;
                var activate=new Button{Content="Open"};activate.Click+=(_,_)=>Activate(w);actions.Children.Add(activate);Buttons["Open "+w.Handle]=activate;
                bool pinned=preferences?.Pins.Contains(w.ProcessPath,StringComparer.OrdinalIgnoreCase)==true;
                var pin=new Button{Content=pinned?"Unpin":"Pin app"};
                pin.Click+=(_,_)=>{try{var p=preferences??throw new InvalidOperationException("Dock storage unavailable.");SavePreferences(pinned?p.Unpin(w.ProcessPath):p.Pin(w.ProcessPath));Render();}catch(Exception ex){report(ex.Message);}};actions.Children.Add(pin);Buttons["Pin "+w.Handle]=pin;stack.Children.Add(actions);
                var card=Design.Surface(stack);Grid.SetRow(card,index/2);Grid.SetColumn(card,index%2);grid.Children.Add(card);index++;
            }
            cards.Children.Add(grid);
        }
        if(shown.Length>36)cards.Children.Add(Design.Caption("Showing the first 36 previews. Narrow your search to see another window."));
    }
    private void RenderDock() {
        dock.Children.Clear();var chosenPin=Pins.SelectedItem as string;Pins.ItemsSource=preferences?.Pins??Array.Empty<string>();if(chosenPin is not null)Pins.SelectedItem=(preferences?.Pins??Array.Empty<string>()).FirstOrDefault(p=>StringComparer.OrdinalIgnoreCase.Equals(p,chosenPin));
        var paths=(preferences?.Pins??Array.Empty<string>()).Concat(inventory.Select(w=>w.ProcessPath).Distinct(StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).Take(32);
        foreach(var path in paths) {
            var name=Path.GetFileNameWithoutExtension(path);int count=inventory.Count(w=>StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,path));
            var button=new Button{Content=$"{name} · {count}",MinHeight=44};Design.Name(button,$"{name}, {count} running windows. Activate or launch pinned app.");
            button.Click+=(_,_)=>{try{var current=service.Enumerate().Where(w=>StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,path)).OrderBy(w=>w.Handle).ToArray();if(current.Length>0){int i=cycle.GetValueOrDefault(path)%current.Length;cycle[path]=i+1;Activate(current[i]);}else if(preferences?.Pins.Contains(path,StringComparer.OrdinalIgnoreCase)==true){if(!File.Exists(path))throw new FileNotFoundException("Pinned executable was moved or removed.");Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}}catch(Exception ex){report(ex.Message);}};
            dock.Children.Add(button);
        }
    }
    public void Dispose(){disposed=true;debounce.Stop();events.Dispose();floating?.Close();foreach(var p in previews)p.Dispose();}
}
