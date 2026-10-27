using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
namespace FlowDesk.App;
internal static class Program {
    internal static readonly NativeNotifications Notifications=new();
    internal static bool AdvancedChecks;
    internal static bool FlowWebChecks,DesktopChecks,RecoveryCheck,HotkeyCheck,ToastProbe;
    internal static string TestPrefix="";
    internal static nint MainHandle;
    internal static string DataDirectory=AppData.Resolve();
    internal static AppActivationArguments? InitialActivation;
    internal static Guid? RequestedNotice;
    [MTAThread]
    private static int Main(string[] args) {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        if(args.Length>0 && args[0]=="--notification-uri") {
            RequestedNotice=args.Length==2?FlowDesk.Core.NotificationCenter.ActivationUri(args[1]):null;
            if(!RequestedNotice.HasValue)return 2;
            args=Array.Empty<string>(); // External URIs cannot select development/test commands.
        }
        AdvancedChecks=args.Contains("--advanced-checks");if(AdvancedChecks)DataDirectory=Path.Combine(ProjectRoot(),".cache","advanced-ui-data-"+Guid.NewGuid().ToString("N"));
        FlowWebChecks=args.Contains("--flowweb-checks");
        if(FlowWebChecks)DataDirectory=Path.Combine(ProjectRoot(),".cache","flowweb-desktop-data");
        DesktopChecks=args.Contains("--desktop-checks");HotkeyCheck=args.Contains("--hotkey-check");ToastProbe=args.Contains("--toast-activation-probe");
        if(HotkeyCheck)DataDirectory=Path.Combine(ProjectRoot(),".cache","hotkey-check-data");
        if(DesktopChecks) DataDirectory=Path.Combine(ProjectRoot(),".cache","desktop-checks-data");
        if(args.Contains("--recovery-seed") || args.Contains("--recovery-ui-check")) {
            DataDirectory=Path.Combine(ProjectRoot(),".cache","desktop-recovery-data");TestPrefix=args.Last();
            if(args.Contains("--recovery-seed"))return DesktopWorkflowChecks.SeedRecovery(TestPrefix);
            RecoveryCheck=true;
        }
        if(args.Length==1 && args[0]=="--uninstall-integrations")return NativeNotifications.Uninstall();
        Notifications.Register();
        if(args.Contains("--cleanup-activation-probe")){int result=InteractionChecks.CleanupProbe();Notifications.Dispose();return result;}
        try{InitialActivation=AppInstance.GetCurrent().GetActivatedEventArgs();}catch(TimeoutException){InitialActivation=null;}
        var instance=AppInstance.FindOrRegisterForKey(AdvancedChecks?"FlowDesk.AdvancedChecks":FlowWebChecks?"FlowDesk.FlowWebChecks":RecoveryCheck?"FlowDesk.RecoveryChecks":HotkeyCheck?"FlowDesk.HotkeyChecks":DesktopChecks?"FlowDesk.DesktopChecks":"FlowDesk.Main");
        if(!instance.IsCurrent) {if(InitialActivation is not null)instance.RedirectActivationToAsync(InitialActivation).AsTask().GetAwaiter().GetResult();Notifications.Dispose();return 0;}
        instance.Activated+=(_,activation)=>Route(activation);
        var ui=new Thread(()=>Application.Start(initialization=>{
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            _=new App();
        }));
        ui.SetApartmentState(ApartmentState.STA);ui.Start();ui.Join();Notifications.Dispose();instance.UnregisterKey();return Environment.ExitCode;
    }
    internal static string ProjectRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../"));
    internal static void Route(AppActivationArguments activation) {
        if(activation.Kind==ExtendedActivationKind.Launch && activation.Data is global::Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs launch) {
            var id=FlowDesk.Core.NotificationCenter.ActivationCommandLine(launch.Arguments);if(id.HasValue)App.OpenNotice(id.Value);else App.OpenWindow();
        } else if(activation.Kind==ExtendedActivationKind.Protocol && activation.Data is global::Windows.ApplicationModel.Activation.IProtocolActivatedEventArgs protocol) {
            var id=FlowDesk.Core.NotificationCenter.ActivationUri(protocol.Uri.AbsoluteUri);if(id.HasValue)App.OpenNotice(id.Value);
        } else if(activation.Kind==ExtendedActivationKind.AppNotification && activation.Data is AppNotificationActivatedEventArgs notice) {
            var id=FlowDesk.Core.NotificationCenter.ActivationId(notice.Argument);if(id.HasValue) App.OpenNotice(id.Value);
        } else App.OpenWindow();
    }
}
