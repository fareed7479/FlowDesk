using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
public sealed class MainWindow : Window {
    private readonly WindowService service=new();
    private readonly ListView windows=new() { SelectionMode=ListViewSelectionMode.Multiple, Height=300 };
    private readonly TextBlock status=new() { TextWrapping=TextWrapping.Wrap };
    private readonly System.Windows.Forms.NotifyIcon tray;
    private readonly string workspace=Path.Combine(AppContext.BaseDirectory,"data","workspace.json");
    public MainWindow() {
        Title="FlowDesk • FlowControl v0.1.0";
        AppWindow.Resize(new global::Windows.Graphics.SizeInt32(760,640));
        var panel=new StackPanel { Padding=new Thickness(24), Spacing=12 };
        panel.Children.Add(new TextBlock { Text="FlowControl",FontSize=28 });
        panel.Children.Add(new TextBlock { Text="Select 1–4 windows, then arrange them. Save captures the selected windows.",TextWrapping=TextWrapping.Wrap });
        panel.Children.Add(windows);
        var actions=new StackPanel { Orientation=Orientation.Horizontal,Spacing=8 };
        Add(actions,"Refresh",()=>Refresh());
        Add(actions,"Split",()=>{
            var selected=windows.SelectedItems.Cast<DesktopWindow>().ToArray();
            if(selected.Length<1 || selected.Length>4) throw new InvalidOperationException("Select between one and four windows.");
            var areas=Layouts.Columns(service.WorkArea(selected[0].Handle),selected.Length);
            for(int i=0;i<selected.Length;i++) service.Move(selected[i],areas[i]);
            status.Text="Position requests sent. Some apps enforce their own minimum size.";
        });
        Add(actions,"Save",()=>{
            var handles=windows.SelectedItems.Cast<DesktopWindow>().Select(w=>w.Handle).ToHashSet();
            var current=service.Enumerate().Where(w=>handles.Contains(w.Handle)).ToArray();
            if(current.Length==0) throw new InvalidOperationException("Select at least one available window.");
            Workspaces.Save(workspace,Workspaces.Capture(current)); status.Text=$"Saved {current.Length} windows locally.";
        });
        Add(actions,"Restore",()=>{
            var saved=Workspaces.Load(workspace); var current=service.Enumerate(); int restored=0;
            foreach(var item in saved.Windows) {
                var match=Workspaces.Match(item,current); if(match is null) continue;
                service.Move(match,Layouts.Clamp(item.Bounds,service.WorkArea(match.Handle))); restored++;
            }
            status.Text=$"Requested {restored} restores; {saved.Windows.Length-restored} missing or ambiguous windows skipped.";
        });
        Add(actions,"Settings",()=>Process.Start(new ProcessStartInfo("ms-settings:"){UseShellExecute=true}));
        panel.Children.Add(actions); panel.Children.Add(status);
        panel.Children.Add(new TextBlock { Text="Prototype: Stage Manager, Mission Control, FlowDock, notification integration and FlowWeb are planned.",TextWrapping=TextWrapping.Wrap });
        Content=new ScrollViewer { Content=panel, VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
        tray=new System.Windows.Forms.NotifyIcon { Icon=System.Drawing.SystemIcons.Application,Text="FlowDesk — double-click to open",Visible=true };
        tray.DoubleClick+=(_,_)=>DispatcherQueue.TryEnqueue(()=>Activate());
        Closed+=(_,_)=>tray.Dispose();
        Refresh();
    }
    private void Add(StackPanel panel,string label,Action action) {
        var button=new Button { Content=label };
        button.Click+=(_,_)=> { try { action(); } catch(Exception ex) { status.Text=ex.Message; } };
        panel.Children.Add(button);
    }
    private void Refresh() { windows.ItemsSource=service.Enumerate(); status.Text="Ready. Closing FlowControl exits FlowDesk."; }
}
