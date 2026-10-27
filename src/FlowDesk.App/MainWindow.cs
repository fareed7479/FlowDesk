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
    private readonly WorkspaceController controller;
    private RestorePlan? preview;
    private readonly ListView windows=new() { SelectionMode=ListViewSelectionMode.Multiple, Height=260 };
    private readonly TextBlock status=new() { TextWrapping=TextWrapping.Wrap };
    private readonly TextBlock previewText=new() { TextWrapping=TextWrapping.Wrap,Text="Preview a saved workspace before applying it." };
    private readonly System.Windows.Forms.NotifyIcon tray;
    private readonly string workspace=Path.Combine(AppContext.BaseDirectory,"data","workspace.json");
    public MainWindow() {
        controller=new(service);
        Title="FlowDesk • FlowControl v0.1.1";
        AppWindow.Resize(new global::Windows.Graphics.SizeInt32(850,750));
        var panel=new StackPanel { Padding=new Thickness(24), Spacing=12 };
        panel.Children.Add(new TextBlock { Text="FlowControl",FontSize=28 });
        panel.Children.Add(new TextBlock { Text="Select 1–4 windows to split. Save captures the selected windows. Undo reverses the last successful layout operation.",TextWrapping=TextWrapping.Wrap });
        panel.Children.Add(windows);
        var actions=new StackPanel { Orientation=Orientation.Horizontal,Spacing=8 };
        Add(actions,"Refresh",()=>Refresh());
        Add(actions,"Split",()=>{
            var selected=windows.SelectedItems.Cast<DesktopWindow>().ToArray();
            if(selected.Length<1 || selected.Length>4) throw new InvalidOperationException("Select between one and four windows.");
            var areas=Layouts.Columns(service.WorkArea(selected[0].Handle),selected.Length);
            ClearPreview();
            ShowResult(controller.Apply(new RestorePlan(selected.Select((w,i)=>new PlannedMove(w,areas[i])).ToArray(),0),clampToCurrentMonitor:false));
        });
        Add(actions,"Save",()=>{
            var handles=windows.SelectedItems.Cast<DesktopWindow>().Select(w=>w.Handle).ToHashSet();
            var current=service.Enumerate().Where(w=>handles.Contains(w.Handle)).ToArray();
            if(current.Length==0) throw new InvalidOperationException("Select at least one available window.");
            Workspaces.Save(workspace,Workspaces.Capture(current)); ClearPreview(); status.Text=$"Saved {current.Length} windows locally.";
        });
        Add(actions,"Undo",()=>{
            if(!controller.CanUndo) throw new InvalidOperationException("No layout operation available to undo.");
            ClearPreview(); ShowResult(controller.Undo());
        });
        panel.Children.Add(actions);
        var restoreActions=new StackPanel { Orientation=Orientation.Horizontal,Spacing=8 };
        Add(restoreActions,"Preview restore",()=>{
            preview=controller.Preview(Workspaces.Load(workspace));
            previewText.Text=$"{preview.Moves.Count} windows matched; {preview.Skipped} skipped.\n"+string.Join("\n",preview.Moves.Select(m=>$"{m.Window.Title} → {m.Target.X}, {m.Target.Y} · {m.Target.Width} × {m.Target.Height}"));
            status.Text="Review the preview, then apply. Window identities will be checked again.";
        });
        Add(restoreActions,"Apply restore",()=>{
            var plan=preview ?? throw new InvalidOperationException("Preview the saved workspace first.");
            ClearPreview(); ShowResult(controller.Apply(plan));
        });
        Add(restoreActions,"Settings",()=>Process.Start(new ProcessStartInfo("ms-settings:"){UseShellExecute=true}));
        panel.Children.Add(restoreActions); panel.Children.Add(previewText); panel.Children.Add(status);
        panel.Children.Add(new TextBlock { Text="Stage Manager, Mission Control, FlowDock, notification integration and FlowWeb are planned.",TextWrapping=TextWrapping.Wrap });
        Content=new ScrollViewer { Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
        tray=new System.Windows.Forms.NotifyIcon { Icon=System.Drawing.SystemIcons.Application,Text="FlowDesk — double-click to open",Visible=true };
        tray.DoubleClick+=(_,_)=>DispatcherQueue.TryEnqueue(()=>Activate());
        Closed+=(_,_)=>tray.Dispose(); Refresh();
    }
    private void ClearPreview() { preview=null; previewText.Text="Preview a saved workspace before applying it."; }
    private void ShowResult(MoveResult result) { status.Text=$"Position requests: {result.Applied}; skipped: {result.Skipped}; errors: {result.Errors.Count}. Apps may enforce their own sizes."+(result.Errors.Count>0 ? "\n"+string.Join("\n",result.Errors):""); }
    private void Add(StackPanel panel,string label,Action action) {
        var button=new Button { Content=label };
        button.Click+=(_,_)=> { try { action(); } catch(Exception ex) { status.Text=ex.Message; } };
        panel.Children.Add(button);
    }
    private void Refresh() { ClearPreview(); windows.ItemsSource=service.Enumerate(); status.Text="Ready. Closing FlowControl exits FlowDesk."; }
}
