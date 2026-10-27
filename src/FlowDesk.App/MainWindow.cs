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
    internal readonly FlowWebView WebPanel;
    internal readonly DesktopModes Modes;
    internal readonly ModesView ModePanel;
    internal readonly NotificationsView NoticePanel;
    internal readonly PageSelection Tabs=new();
    internal readonly NavigationView Navigation=new();
    internal readonly WorkspaceView WorkspacePanel;
    internal readonly DesktopOverviewView OverviewPanel;
    private readonly NavigationViewItem noticeTab;
    private readonly RecoveryHotkey recoveryHotkey;
    private Microsoft.UI.Windowing.AppWindowPresenter? shellPresenter;
    internal void BringForward() {AppWindow.Show();if(AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p && p.State==Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)p.Restore();Activate();}
    internal void ShowNotice(Guid id) {NoticeCenter.Read(id);Tabs.SelectedIndex=4;BringForward();}
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
    private readonly WindowEvents inventoryEvents=new();
    private readonly DispatcherTimer inventoryTimer=new(){Interval=TimeSpan.FromMilliseconds(650)};
    private bool inventoryDirty,refreshing;
    private readonly string workspace=Path.Combine(Program.DataDirectory,"workspace.json");
    public MainWindow() {
        Program.MainHandle=WinRT.Interop.WindowNative.GetWindowHandle(this);
        try{SystemBackdrop=new Microsoft.UI.Xaml.Media.MicaBackdrop();}catch(System.Runtime.InteropServices.COMException){}
        controller=new(service);
        NoticeCenter=new NotificationCenter(Path.Combine(Program.DataDirectory,"notifications.json"),Program.Notifications);
        Modes=new DesktopModes(service,Path.Combine(Program.DataDirectory,"mode-recovery.json"));
        ModesView? modePanel=null;
        recoveryHotkey=new RecoveryHotkey(()=>{modePanel?.EndSession("Session ended by shortcut");if(AppWindow.Presenter.Kind==Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen)ToggleShellFullscreen();BringForward();});
        Title="FlowDesk · Your desktop, in flow";
        AppWindow.Resize(new global::Windows.Graphics.SizeInt32(1240,900));
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
        panel.Children.Add(new TextBlock { Text="Choose a workspace from the sidebar. Changes apply only to the windows you select. Use Preview restore to review a saved layout before moving windows.",TextWrapping=TextWrapping.Wrap });
        var history=new List<string>();
        var shellStatus=new TextBlock {Text="Ready",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(16)};
        var activity=new ListView();
        void Report(string message) { shellStatus.Text=message; history.Insert(0,$"{DateTime.Now:HH:mm:ss}  {message}"); if(history.Count>100) history.RemoveAt(history.Count-1); activity.ItemsSource=history.ToArray(); NoticeCenter.Publish("FlowDesk",message.Length>2000?message[..2000]:message); }
        var workspaceView=WorkspacePanel=new WorkspaceView(service,controller,Report,()=>Modes.IsActive);
        WebPanel=new FlowWebView(()=>Modes.IsActive);
        ModePanel=modePanel=new ModesView(service,Modes,NoticeCenter,()=>workspaceView.Stage.IsActive,()=>recoveryHotkey.Available,ToggleShellFullscreen);
        NoticePanel=new NotificationsView(NoticeCenter,Program.Notifications);
        OverviewPanel=new DesktopOverviewView(service,Report,()=>Modes.IsActive || workspaceView.Stage.IsActive);
        var shell=new Grid();shell.RequestedTheme=AppData.LoadTheme();
        var navigation=Navigation;navigation.PaneDisplayMode=NavigationViewPaneDisplayMode.Auto;navigation.IsBackButtonVisible=NavigationViewBackButtonVisible.Collapsed;navigation.IsSettingsVisible=false;navigation.OpenPaneLength=238;navigation.ExpandedModeThresholdWidth=940;navigation.CompactModeThresholdWidth=720;navigation.IsPaneOpen=true;
        var brand=new StackPanel{Spacing=4,Margin=new Thickness(18,16,16,24)};
        brand.Children.Add(new TextBlock{Text="◈  FlowDesk",FontSize=24,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});brand.Children.Add(Design.Caption("YOUR DESKTOP, IN FLOW"));navigation.PaneCustomContent=brand;
        var version=new TextBlock{Text="v0.5.0 · Desktop preview",Margin=new Thickness(20),FontSize=12,Opacity=.6};navigation.PaneFooter=version;
        void UpdatePane(){brand.Visibility=navigation.DisplayMode==NavigationViewDisplayMode.Expanded && navigation.IsPaneOpen?Visibility.Visible:Visibility.Collapsed;version.Visibility=brand.Visibility;}
        navigation.DisplayModeChanged+=(_,_)=>UpdatePane();navigation.PaneOpened+=(_,_)=>UpdatePane();navigation.PaneClosed+=(_,_)=>UpdatePane();navigation.Loaded+=(_,_)=>UpdatePane();
        var labels=new[]{"FlowControl","Workspaces & Stage","Overview & FlowDock","Focus & fullscreen","Notifications","Activity","FlowWeb","Settings"};
        var glyphs=new[]{"\uE80F","\uE8F1","\uE7F4","\uE708","\uE7F4","\uE81C","\uE774","\uE713"};
        var pages=new UIElement[]{Design.Page(panel,"Make space for what matters","Arrange your windows, preview a saved layout, and return to a workspace in a few clicks.","FLOWCONTROL"),workspaceView,OverviewPanel,ModePanel,NoticePanel,Design.Page(new StackPanel{Children={activity}},"Your workspace timeline","Recent actions from this session.","ACTIVITY",0),WebPanel,new AppearanceView(shell)};
        var items=new List<NavigationViewItem>();
        for(int i=0;i<labels.Length;i++){var item=new NavigationViewItem{Content=labels[i],Icon=new FontIcon{Glyph=glyphs[i]},Tag=i};Design.Name(item,labels[i]);items.Add(item);navigation.MenuItems.Add(item);}
        noticeTab=items[4];NoticeCenter.Changed+=()=>noticeTab.Content=$"Notifications ({NoticeCenter.Unread})";
        navigation.SelectionChanged+=(_,e)=>{if(e.SelectedItem is NavigationViewItem item && item.Tag is int index && Tabs.SelectedIndex!=index)Tabs.SelectedIndex=index;};
        Tabs.Changed+=index=>{if(index<0 || index>=pages.Length)return;navigation.SelectedItem=items[index];navigation.Content=pages[index];};
        shell.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});shell.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});shell.Children.Add(navigation);
        shellStatus.FontSize=12;shellStatus.Opacity=.8;Grid.SetRow(shellStatus,1);shell.Children.Add(shellStatus);Content=shell;Tabs.SelectedIndex=0;
        AppWindow.Closing+=(_,e)=>{ if(Modes.IsActive){ModePanel.EndSession("Session ended on close");if(Modes.Session is not null){e.Cancel=true;return;}} if(workspaceView.Stage.IsActive) { var result=workspaceView.Stage.Leave(); if(workspaceView.Stage.IsActive) {e.Cancel=true; Report("Could not restore all staged windows. Try Leave Stage before closing. "+string.Join(" ",result.Errors));} } };
        tray=new System.Windows.Forms.NotifyIcon { Icon=System.Drawing.SystemIcons.Application,Text="FlowDesk — double-click to open",Visible=true };
        tray.DoubleClick+=(_,_)=>DispatcherQueue.TryEnqueue(()=>Activate());
        inventoryEvents.Changed+=()=>inventoryDirty=true;inventoryTimer.Tick+=(_,_)=>{if(inventoryDirty && Tabs.SelectedIndex==0){inventoryDirty=false;Refresh(false);}};inventoryTimer.Start();
        Closed+=(_,_)=>{inventoryTimer.Stop();inventoryEvents.Dispose();if(Modes.Session is not null)ModePanel.EndSession("Session restored on window close");OverviewPanel.Dispose();WebPanel.Dispose();ModePanel.Dispose();recoveryHotkey.Dispose();tray.Dispose();}; Refresh();
    }
    private void ClearPreview() { preview=null; previewText.Text="Preview a saved workspace before applying it."; }
    private void ShowResult(MoveResult result) { status.Text=$"Position requests: {result.Applied}; skipped: {result.Skipped}; errors: {result.Errors.Count}. Apps may enforce their own sizes."+(result.Errors.Count>0 ? "\n"+string.Join("\n",result.Errors):""); }
    private void Add(StackPanel panel,string label,Action action) {
        var button=new Button { Content=label };
        button.Click+=(_,_)=> { try { if((Modes.IsActive || WorkspacePanel.Stage.IsActive) && label is "Split" or "Undo" or "Apply restore")throw new InvalidOperationException("Leave Stage and end focus/fullscreen before changing layouts."); action(); } catch(Exception ex) { status.Text=ex.Message; } };
        panel.Children.Add(button);
    }
    private async void Refresh(bool manual=true) {if(refreshing)return;refreshing=true;try{var selected=windows.SelectedItems.Cast<DesktopWindow>().ToArray();var result=await System.Threading.Tasks.Task.Run(service.Enumerate);windows.ItemsSource=result;foreach(var w in result)if(selected.Any(s=>s.Handle==w.Handle && s.ProcessId==w.ProcessId && s.ProcessPath==w.ProcessPath))windows.SelectedItems.Add(w);if(manual){ClearPreview();status.Text="Ready. Closing FlowControl exits FlowDesk.";}}catch(Exception ex){status.Text=ex.Message;}finally{refreshing=false;}}
}
