using System;
using System.Collections.Generic;
using System.Linq;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
internal sealed class ModesView : UserControl,IDisposable {
    private readonly WindowService service;
    private readonly DesktopModes modes;
    private readonly NotificationCenter notices;
    private readonly Func<bool> stageActive,hotkeyAvailable;
    private readonly Action toggleShellFullscreen;
    private readonly TextBlock status=new() {TextWrapping=TextWrapping.Wrap};
    private readonly ListView exclusions=new() {Height=80,SelectionMode=ListViewSelectionMode.Multiple};
    private bool automaticRetryBlocked;
    private readonly TextBlock countdown=new(){Text="Ready when you are",FontSize=36,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold};
    private readonly ProgressBar progress=new(){Minimum=0,Maximum=100,Value=0,Height=6};
    private double sessionMinutes=25;
    private readonly DispatcherTimer timer=new() {Interval=TimeSpan.FromSeconds(1)};
    internal readonly ComboBox Target=new() {Header="Target window",HorizontalAlignment=HorizontalAlignment.Stretch};
    internal readonly ListView Distractions=new() {Height=160,SelectionMode=ListViewSelectionMode.Multiple};
    internal readonly NumberBox Minutes=new() {Header="Focus duration (minutes)",Minimum=1,Maximum=240,Value=25,SpinButtonPlacementMode=NumberBoxSpinButtonPlacementMode.Inline};
    internal readonly CheckBox FullscreenTarget=new() {Content="Use borderless fullscreen during focus"};
    internal readonly Dictionary<string,Button> Buttons=new();
    public ModesView(WindowService service,DesktopModes modes,NotificationCenter notices,Func<bool> stageActive,Func<bool> hotkeyAvailable,Action toggleShellFullscreen) {
        this.service=service;this.modes=modes;this.notices=notices;this.stageActive=stageActive;this.hotkeyAvailable=hotkeyAvailable;this.toggleShellFullscreen=toggleShellFullscreen;
        var panel=new StackPanel {Spacing=10,Padding=new Thickness(20)};
        panel.Children.Add(new TextBlock {Text="Fullscreen & focus",FontSize=24});
        panel.Children.Add(new TextBlock {Text="Choose a target and select the distractions to minimize. Excluded apps stay available. Ctrl+Alt+F10 ends the session from any app. Original frame, placement and show state are saved for recovery.",TextWrapping=TextWrapping.Wrap});
        var sessionCard=new StackPanel{Spacing=12};sessionCard.Children.Add(Design.Caption("YOUR FOCUS SESSION"));sessionCard.Children.Add(countdown);sessionCard.Children.Add(progress);panel.Children.Add(Design.Surface(sessionCard));
        panel.Children.Add(Target);panel.Children.Add(new TextBlock {Text="Selected distractions to minimize:"});panel.Children.Add(Distractions);
        panel.Children.Add(Minutes);panel.Children.Add(FullscreenTarget);
        var actions=new StackPanel {Orientation=Orientation.Horizontal,Spacing=8};
        Add(actions,"Refresh windows",RefreshWindows);
        Add(actions,"Start focus",()=>Start(true));Add(actions,"Start fullscreen",()=>Start(false));Add(actions,"End session",()=>EndSession("Session ended"));
        panel.Children.Add(actions);
        var secondary=new StackPanel {Orientation=Orientation.Horizontal,Spacing=8};
        Add(secondary,"Toggle FlowDesk fullscreen",toggleShellFullscreen);
        panel.Children.Add(secondary);panel.Children.Add(status);
        panel.Children.Add(new TextBlock {Text="Apps excluded from distraction minimization (saved by executable path):",TextWrapping=TextWrapping.Wrap});panel.Children.Add(exclusions);
        var excludeActions=new StackPanel {Orientation=Orientation.Horizontal,Spacing=8};
        Add(excludeActions,"Exclude selected apps",()=>{var paths=Distractions.SelectedItems.Cast<DesktopWindow>().Select(w=>w.ProcessPath);notices.SetPreferences(notices.State.Preferences with {ExcludedApps=(notices.State.Preferences.ExcludedApps??Array.Empty<string>()).Concat(paths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()});RefreshStatus();});
        Add(excludeActions,"Remove exclusions",()=>{var removed=exclusions.SelectedItems.Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);notices.SetPreferences(notices.State.Preferences with {ExcludedApps=(notices.State.Preferences.ExcludedApps??Array.Empty<string>()).Where(p=>!removed.Contains(p)).ToArray()});RefreshStatus();});
        panel.Children.Add(excludeActions);Content=Design.Page(panel,"Find your focus","Choose the work that matters. Keep selected distractions out of the way, with a clear route back.","FOCUS & FULLSCREEN");RefreshWindows();
        if(modes.IsActive) EndSession("Recovered interrupted session");
        timer.Tick+=(_,_)=>{try{if(!automaticRetryBlocked && modes.ShouldEnd(DateTimeOffset.UtcNow))EndSession("Focus completed or target closed");RefreshStatus();}catch(Exception ex){status.Text=ex.Message;}};timer.Start();
    }
    internal void RefreshWindows() {var inventory=service.Enumerate();Target.ItemsSource=inventory;Distractions.ItemsSource=inventory;RefreshStatus();}
    private void Start(bool focus) {
        if(stageActive())throw new InvalidOperationException("Leave Stage before starting a fullscreen or focus session.");
        if(!hotkeyAvailable())throw new InvalidOperationException("Ctrl+Alt+F10 is unavailable. Free that shortcut and restart FlowDesk before starting a session.");
        var target=Target.SelectedItem as DesktopWindow ?? throw new InvalidOperationException("Select a target window.");
        if(double.IsNaN(Minutes.Value) || Minutes.Value%1!=0)throw new InvalidOperationException("Enter a whole number of minutes.");
        modes.Start(target,focus?Distractions.SelectedItems.Cast<DesktopWindow>():Array.Empty<DesktopWindow>(),notices.State.Preferences.ExcludedApps??Array.Empty<string>(),focus?(int)Minutes.Value:null,!focus || FullscreenTarget.IsChecked==true);
        sessionMinutes=Minutes.Value;automaticRetryBlocked=false;notices.Quiet=modes.IsActive;notices.Publish(focus?"Focus started":"Fullscreen started","Use End session or Ctrl+Alt+F10 to restore the original windows.");RefreshStatus();
    }
    internal void EndSession(string reason) {
        var hadSession=modes.IsActive;var result=modes.End();automaticRetryBlocked=modes.IsActive;notices.Quiet=modes.IsActive;
        if(hadSession)notices.Publish(reason,$"Restored {result.Restored}; closed or changed windows skipped: {result.Skipped}. "+string.Join(" ",result.Errors));
        RefreshStatus();
    }
    internal void RefreshStatus() {
        status.Text=modes.RecoveryError is not null?"Recovery needs attention: "+modes.RecoveryError:modes.Session is null?"No active session. Recovery shortcut: "+(hotkeyAvailable()?"Ctrl+Alt+F10":"Unavailable"):modes.Session.Kind+" active"+(modes.Session.Ends is not null?$" · Ends {modes.Session.Ends.Value.ToLocalTime():t}":"")+" · Ctrl+Alt+F10 to restore";
        if(modes.Session?.Ends is DateTimeOffset ends){var remaining=ends-DateTimeOffset.UtcNow;countdown.Text=$"{Math.Max(0,(int)remaining.TotalMinutes):00}:{Math.Max(0,remaining.Seconds):00}";progress.Value=Math.Clamp(100-remaining.TotalMinutes/Math.Max(1,sessionMinutes)*100,0,100);}else{countdown.Text=modes.IsActive?"Fullscreen is active":"Ready when you are";progress.Value=0;}
        var paths=notices.State.Preferences.ExcludedApps??Array.Empty<string>();
        if(exclusions.ItemsSource is not string[] shown || !shown.SequenceEqual(paths,StringComparer.OrdinalIgnoreCase))exclusions.ItemsSource=paths.ToArray();
        Buttons["Start focus"].IsEnabled=!modes.IsActive;Buttons["Start fullscreen"].IsEnabled=!modes.IsActive;Buttons["End session"].IsEnabled=modes.IsActive;
    }
    private void Add(StackPanel panel,string label,Action action) {var b=new Button {Content=label};b.Click+=(_,_)=>{try{action();}catch(Exception ex){status.Text=ex.Message;notices.Publish("FlowDesk action needs attention",ex.Message.Length>2000?ex.Message[..2000]:ex.Message);}};Buttons[label]=b;panel.Children.Add(b);}
    public void Dispose()=>timer.Stop();
}
