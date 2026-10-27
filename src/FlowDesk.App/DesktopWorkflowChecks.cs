using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
internal static class DesktopWorkflowChecks {
    private static readonly List<string> passed=new();
    private static string ResultPath=>Path.Combine(Program.ProjectRoot(),".cache","desktop-workflow-results.json");
    private static void Check(bool value,string name) {if(!value)throw new InvalidOperationException(name);passed.Add(name);File.WriteAllText(ResultPath,JsonSerializer.Serialize(new {Complete=false,Passed=passed}));}
    private static async Task Wait(Func<Task<bool>> predicate,int seconds=8) {for(int i=0;i<seconds*10;i++){if(await predicate())return;await Task.Delay(100);}throw new TimeoutException("Desktop workflow did not reach the expected state.");}
    private static async Task Click(Button button) {var peer=new ButtonAutomationPeer(button);((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();await Task.Delay(200);}
    private static bool Equivalent(WindowState a,WindowState b)=>a.ShowCommand==b.ShowCommand && a.NormalBounds==b.NormalBounds;
    internal sealed record ExpectedRecovery(long Handle,WindowState Placement,long Style);
    internal static int SeedRecovery(string prefix) {
        if(!prefix.StartsWith("FlowDesk desktop workflow ",StringComparison.Ordinal))return 92;
        var service=new WindowService();var fixtures=service.Enumerate().Where(w=>w.Title.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(w=>w.Title).ToArray();
        if(fixtures.Length!=2)return 92;
        Directory.CreateDirectory(Program.DataDirectory);
        File.WriteAllText(Path.Combine(Program.DataDirectory,"expected.json"),JsonSerializer.Serialize(fixtures.Select(w=>new ExpectedRecovery(w.Handle.ToInt64(),service.CaptureState(w),service.Style(w))).ToArray()));
        var modes=new DesktopModes(service,Path.Combine(Program.DataDirectory,"mode-recovery.json"));if(modes.IsActive)modes.End();
        modes.Start(fixtures[0],[fixtures[1]],Array.Empty<string>(),1,true);
        return 91; // Deliberately leave the session journal and per-window markers intact on process exit.
    }
    internal static void VerifyRecovery(MainWindow window) {
        bool success=false;string? error=null;
        try {
            var expected=JsonSerializer.Deserialize<ExpectedRecovery[]>(File.ReadAllText(Path.Combine(Program.DataDirectory,"expected.json")))!;
            var service=new WindowService();var windows=service.Enumerate();
            success=!window.Modes.IsActive && !File.Exists(Path.Combine(Program.DataDirectory,"mode-recovery.json")) && expected.All(e=>{
                var live=windows.SingleOrDefault(w=>w.Handle.ToInt64()==e.Handle);
                return live is not null && service.Style(live)==e.Style && Equivalent(service.CaptureState(live),e.Placement);
            });
            if(!success)error="Startup recovery did not restore all fixture states.";
        } catch(Exception ex){error=ex.ToString();}
        File.WriteAllText(Path.Combine(Program.DataDirectory,"result.json"),JsonSerializer.Serialize(new {Success=success,Error=error}));Environment.ExitCode=success?0:1;window.Close();
    }
    internal static async Task Run(MainWindow window) {
        var processes=new List<Process>();var service=new WindowService();var snapshots=new List<(DesktopWindow Window,WindowState State)>();
        void Cleanup() {foreach(var process in processes){try{if(!process.HasExited){process.CloseMainWindow();if(!process.WaitForExit(2000))process.Kill();}process.Dispose();}catch{}}}
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath)!);
            string prefix="FlowDesk desktop workflow "+Guid.NewGuid().ToString("N");
            for(int i=0;i<2;i++) {
                string fixture=Path.Combine(Program.ProjectRoot(),"tests","FlowDesk.Checks","bin","Debug","net10.0-windows","FlowDesk.Checks.exe");
                var start=new ProcessStartInfo(fixture){UseShellExecute=false};start.Environment["DOTNET_ROOT_X64"]=Path.Combine(Program.ProjectRoot(),".tools","dotnet");start.ArgumentList.Add("--fixture");start.ArgumentList.Add(prefix+i);processes.Add(Process.Start(start)!);
            }
            DesktopWindow[] fixtures=Array.Empty<DesktopWindow>();
            await Wait(()=>{fixtures=service.Enumerate().Where(w=>w.Title.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(w=>w.Title).ToArray();return Task.FromResult(fixtures.Length==2);});
            foreach(var fixture in fixtures)snapshots.Add((fixture,service.CaptureState(fixture)));
            var hub=window.NoticeCenter;var notices=window.NoticePanel;var mode=window.ModePanel;
            hub.SetPreferences(new NotificationPreferences());hub.Clear();
            window.Tabs.SelectedIndex=4;await Task.Delay(250);
            await Click(notices.Buttons["Test notification"]);var item=hub.State.Items[0];
            Check(item.Title=="FlowDesk test" && hub.Unread==1,"notification UI creates an unread local card");
            await Click(notices.Buttons["Read "+item.Id.ToString("N")]);Check(hub.Unread==0,"notification UI marks a card read");
            notices.UnreadOnly.IsOn=true;Check(notices.CardCount==0,"notification UI filters read cards");notices.UnreadOnly.IsOn=false;
            await Click(notices.Buttons["Dismiss "+item.Id.ToString("N")]);await Wait(()=>Task.FromResult(hub.State.Items.Length==0));Check(hub.State.Items.Length==0,"notification UI dismisses a card");
            notices.Banners.IsOn=true;await Click(notices.Buttons["Test notification"]);var native=hub.State.Items[0];
            Check(native.Delivery=="Submitted to Windows","notification UI submits a real Windows banner");
            await Wait(()=>Program.Notifications.Contains(native.Id));Check(await Program.Notifications.Contains(native.Id),"submitted notification appears in Windows notification history");
            Check(await Program.Notifications.HasProtocolLink(native.Id),"Windows notification payload contains the validated activation protocol");
            await Click(notices.Buttons["Dismiss "+native.Id.ToString("N")]);await Wait(async ()=>!await Program.Notifications.Contains(native.Id));Check(!await Program.Notifications.Contains(native.Id),"dismiss removes the Windows notification as well as the local card");
            notices.Banners.IsOn=false;
            string app=Path.Combine(AppContext.BaseDirectory,"FlowDesk.App.exe");
            foreach(string phase in new[]{"--recovery-seed","--recovery-ui-check"}) {
                var start=new ProcessStartInfo(app){UseShellExecute=false};start.ArgumentList.Add(phase);start.ArgumentList.Add(prefix);
                using var child=Process.Start(start)!;await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
                Check(child.ExitCode==(phase=="--recovery-seed"?91:0),phase=="--recovery-seed"?"separate process leaves an interrupted fullscreen/focus journal":"fresh app process automatically recovers interrupted fullscreen/focus");
            }
            window.Tabs.SelectedIndex=3;await Task.Delay(250);mode.RefreshWindows();
            var inventory=((System.Collections.IEnumerable)mode.Target.ItemsSource).Cast<DesktopWindow>().ToArray();
            mode.Target.SelectedItem=inventory.Single(w=>w.Handle==fixtures[0].Handle);mode.Distractions.SelectedItems.Add(inventory.Single(w=>w.Handle==fixtures[1].Handle));mode.Minutes.Value=1;
            await Click(mode.Buttons["Start focus"]);
            Check(window.Modes.IsFocus && service.IsMinimized(fixtures[1]) && hub.Quiet,"focus UI minimizes chosen distraction and quiets notifications");
            hub.SetPreferences(hub.State.Preferences with {WindowsBanners=true});var quiet=hub.Publish("Quiet check","This must stay in FlowDesk.");
            Check(quiet.Delivery=="Quiet mode" && !await Program.Notifications.Contains(quiet.Id),"focus session suppresses actual Windows notification delivery");
            hub.SetPreferences(hub.State.Preferences with {WindowsBanners=false});
            await Click(mode.Buttons["End session"]);Check(!window.Modes.IsActive && !hub.Quiet && Equivalent(service.CaptureState(fixtures[1]),snapshots[1].State),"focus UI end restores distraction and notification behavior");
            await Click(mode.Buttons["Exclude selected apps"]);await Click(mode.Buttons["Start focus"]);
            Check(window.Modes.IsFocus && !service.IsMinimized(fixtures[1]),"focus UI respects saved app exclusions");await Click(mode.Buttons["End session"]);
            hub.SetPreferences(hub.State.Preferences with {ExcludedApps=Array.Empty<string>()});
            await Click(mode.Buttons["Start fullscreen"]);var full=service.ResolveManaged(window.Modes.Session!.Windows[0].Identity)!;
            Check(full.Bounds==service.MonitorBounds(full.Handle) && (service.Style(full)&0x00CF0000L)==0,"fullscreen UI removes frame and fills the target monitor");
            await Click(mode.Buttons["End session"]);Check(Equivalent(service.CaptureState(fixtures[0]),snapshots[0].State),"fullscreen UI exit restores original placement");
            var size=window.AppWindow.Size;await Click(mode.Buttons["Toggle FlowDesk fullscreen"]);Check(window.AppWindow.Presenter.Kind==Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen,"FlowDesk fullscreen presenter enters through UI");
            await Click(mode.Buttons["Toggle FlowDesk fullscreen"]);Check(window.AppWindow.Presenter.Kind==Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped && window.AppWindow.Size==size,"FlowDesk fullscreen presenter restores window size");
            await Click(mode.Buttons["Start focus"]);
            Check(window.Modes.IsFocus,"one-minute focus timer started through UI");
            await Wait(()=>Task.FromResult(!window.Modes.IsActive),70);
            Check(!hub.Quiet && !service.IsMinimized(fixtures[1]),"one-minute focus timer automatically restores windows and quiet mode");
            window.Tabs.SelectedIndex=4;await Task.Delay(200);await Click(notices.Buttons["Mark all read"]);Check(hub.Unread==0,"mark-all-read UI updates notification badge");
            await Click(notices.Buttons["Clear notifications"]);await Wait(()=>Task.FromResult(hub.State.Items.Length==0));Check(hub.State.Items.Length==0,"clear-notifications UI clears local history");
            window.Tabs.SelectedIndex=3;await Task.Delay(200);await Click(mode.Buttons["Start focus"]);
            window.Closed+=(_,_)=>{
                try {System.Threading.Thread.Sleep(200);Check(!window.Modes.IsActive && !service.IsMinimized(fixtures[1]),"closing FlowDesk restores an active focus session");File.WriteAllText(ResultPath,JsonSerializer.Serialize(new {Complete=true,Passed=passed,Error=(string?)null}));Environment.ExitCode=0;}
                catch(Exception ex){File.WriteAllText(ResultPath,JsonSerializer.Serialize(new {Complete=true,Passed=passed,Error=ex.ToString()}));Environment.ExitCode=1;}
                finally{Cleanup();}
            };
            window.Close();
        } catch(Exception ex) {
            try{window.Modes.End();}catch{}
            foreach(var snapshot in snapshots){try{var live=service.Resolve(snapshot.Window);if(live is not null)service.RestoreState(live,snapshot.State);}catch{}}
            Cleanup();File.WriteAllText(ResultPath,JsonSerializer.Serialize(new {Complete=true,Passed=passed,Error=ex.ToString()}));Environment.ExitCode=1;window.Close();
        }
    }
}
