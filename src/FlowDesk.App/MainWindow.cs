using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
public sealed class MainWindow : Window {
    internal readonly NotificationCenter NoticeCenter;
    internal readonly DesktopModes Modes;
    internal readonly ModesView ModePanel;
    internal readonly NotificationsView NoticePanel;
    internal readonly TabView Tabs=new() {IsAddTabButtonVisible=false};
    private readonly TabViewItem noticeTab;
    private readonly RecoveryHotkey recoveryHotkey;
    private Microsoft.UI.Windowing.AppWindowPresenter? shellPresenter;
    internal void BringForward() {AppWindow.Show();if(AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p && p.State==Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)p.Restore();Activate();}
    internal void ShowNotice(Guid id) {NoticeCenter.Read(id);Tabs.SelectedItem=noticeTab;BringForward();}
    private void ToggleShellFullscreen() {
        if(AppWindow.Presenter.Kind==Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen) {if(shellPresenter is not null)AppWindow.SetPresenter(shellPresenter);else AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);shellPresenter=null;}
        else {shellPresenter=AppWindow.Presenter;AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);}
    }
    private readonly WindowService service=new();
    private readonly WorkspaceController controller;
    private RestorePlan? preview;
    private readonly ListView windows=new() { SelectionMode=ListViewSelectionMode.Multiple, Height=260 };
    private readonly TextBlock status=new() { TextWrapping=TextWrapping.Wrap };
    private readonly TextBlock previewText=new() { TextWrapping=TextWrapping.Wrap,Text="Preview a saved workspace before applying it." };
    private readonly System.Windows.Forms.NotifyIcon tray;
    private readonly string workspace=Path.Combine(Program.DataDirectory,"workspace.json");
    public MainWindow() {
        controller=new(service);
        NoticeCenter=new NotificationCenter(Path.Combine(Program.DataDirectory,"notifications.json"),Program.Notifications);
        Modes=new DesktopModes(service,Path.Combine(Program.DataDirectory,"mode-recovery.json"));
        ModesView? modePanel=null;
        recoveryHotkey=new RecoveryHotkey(()=>{modePanel?.EndSession("Session ended by shortcut");if(AppWindow.Presenter.Kind==Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen)ToggleShellFullscreen();BringForward();});
        Title="FlowDesk • FlowControl v0.3.0";
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
        panel.Children.Add(new TextBlock { Text="Named workspaces, Stage Manager and the window overview are available in the other tabs. Notifications and fullscreen/focus are available in their tabs. FlowWeb is planned.",TextWrapping=TextWrapping.Wrap });
        var history=new List<string>();
        var shellStatus=new TextBlock {Text="Ready",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(16)};
        var activity=new ListView();
        void Report(string message) { shellStatus.Text=message; history.Insert(0,$"{DateTime.Now:HH:mm:ss}  {message}"); if(history.Count>100) history.RemoveAt(history.Count-1); activity.ItemsSource=history.ToArray(); NoticeCenter.Publish("FlowDesk",message.Length>2000?message[..2000]:message); }
        var workspaceView=new WorkspaceView(service,controller,Report,()=>Modes.IsActive);
        var tabs=Tabs;
        ModePanel=modePanel=new ModesView(service,Modes,NoticeCenter,()=>workspaceView.Stage.IsActive,()=>recoveryHotkey.Available,ToggleShellFullscreen);
        NoticePanel=new NotificationsView(NoticeCenter,Program.Notifications);
        noticeTab=new TabViewItem {Header=$"Notifications ({NoticeCenter.Unread})",IsClosable=false,Content=NoticePanel};
        NoticeCenter.Changed+=()=>noticeTab.Header=$"Notifications ({NoticeCenter.Unread})";
        tabs.TabItems.Add(new TabViewItem {Header="FlowControl",IsClosable=false,Content=new ScrollViewer {Content=panel}});
        tabs.TabItems.Add(new TabViewItem {Header="Workspaces & Stage",IsClosable=false,Content=workspaceView});
        tabs.TabItems.Add(new TabViewItem {Header="Overview & FlowDock",IsClosable=false,Content=new DesktopOverviewView(service,Report)});
        tabs.TabItems.Add(new TabViewItem {Header="Focus & fullscreen",IsClosable=false,Content=ModePanel});
        tabs.TabItems.Add(noticeTab);
        tabs.TabItems.Add(new TabViewItem {Header="Activity",IsClosable=false,Content=activity});
        var shell=new Grid(); shell.RowDefinitions.Add(new RowDefinition {Height=new GridLength(1,GridUnitType.Star)}); shell.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        shell.Children.Add(tabs); Grid.SetRow(shellStatus,1); shell.Children.Add(shellStatus); Content=shell;
        AppWindow.Closing+=(_,e)=>{ if(Modes.IsActive){ModePanel.EndSession("Session ended on close");if(Modes.Session is not null){e.Cancel=true;return;}} if(workspaceView.Stage.IsActive) { var result=workspaceView.Stage.Leave(); if(workspaceView.Stage.IsActive) {e.Cancel=true; Report("Could not restore all staged windows. Try Leave Stage before closing. "+string.Join(" ",result.Errors));} } };
        tray=new System.Windows.Forms.NotifyIcon { Icon=System.Drawing.SystemIcons.Application,Text="FlowDesk — double-click to open",Visible=true };
        tray.DoubleClick+=(_,_)=>DispatcherQueue.TryEnqueue(()=>Activate());
        Closed+=(_,_)=>{if(Modes.Session is not null)ModePanel.EndSession("Session restored on window close");ModePanel.Dispose();recoveryHotkey.Dispose();tray.Dispose();}; Refresh();
    }
    private void ClearPreview() { preview=null; previewText.Text="Preview a saved workspace before applying it."; }
    private void ShowResult(MoveResult result) { status.Text=$"Position requests: {result.Applied}; skipped: {result.Skipped}; errors: {result.Errors.Count}. Apps may enforce their own sizes."+(result.Errors.Count>0 ? "\n"+string.Join("\n",result.Errors):""); }
    private void Add(StackPanel panel,string label,Action action) {
        var button=new Button { Content=label };
        button.Click+=(_,_)=> { try { if(Modes.IsActive && label is "Split" or "Undo" or "Apply restore")throw new InvalidOperationException("End focus/fullscreen before changing layouts."); action(); } catch(Exception ex) { status.Text=ex.Message; } };
        panel.Children.Add(button);
    }
    private void Refresh() { ClearPreview(); windows.ItemsSource=service.Enumerate(); status.Text="Ready. Closing FlowControl exits FlowDesk."; }
}
