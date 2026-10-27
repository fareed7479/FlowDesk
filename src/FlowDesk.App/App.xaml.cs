using System;
using Microsoft.UI.Xaml;
namespace FlowDesk.App;
public partial class App : Application {
    private static MainWindow? window;
    private static readonly object gate=new();
    private static Guid? pendingNotice;
    public App() {InitializeComponent();}
    internal static void OpenWindow() {lock(gate){window?.DispatcherQueue.TryEnqueue(()=>window?.BringForward());}}
    internal static void OpenNotice(Guid id) {
        lock(gate) {if(window is null) {pendingNotice=id;return;}window.DispatcherQueue.TryEnqueue(()=>window?.ShowNotice(id));}
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        window=new MainWindow();window.Activate();
        lock(gate){if(pendingNotice.HasValue){window.ShowNotice(pendingNotice.Value);pendingNotice=null;}}
        if(Program.RequestedNotice.HasValue)window.ShowNotice(Program.RequestedNotice.Value);
        if(Program.InitialActivation is not null) Program.Route(Program.InitialActivation);
        if(Program.FlowWebChecks)window.DispatcherQueue.TryEnqueue(async()=>await FlowWebWorkflowChecks.Run(window));
        if(Program.HotkeyCheck)window.DispatcherQueue.TryEnqueue(async ()=>await InteractionChecks.Hotkey(window));
        if(Program.ToastProbe)window.DispatcherQueue.TryEnqueue(async ()=>await InteractionChecks.ToastProbe(window));
        if(Program.RecoveryCheck)window.DispatcherQueue.TryEnqueue(()=>DesktopWorkflowChecks.VerifyRecovery(window));
        if(Program.DesktopChecks) window.DispatcherQueue.TryEnqueue(async ()=>await DesktopWorkflowChecks.Run(window));
    }
}
