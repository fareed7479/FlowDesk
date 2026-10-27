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
namespace FlowDesk.App;
internal static class InteractionChecks {
    internal static async Task Hotkey(MainWindow window) {
        var service=new WindowService();var processes=new List<Process>();var before=new List<(DesktopWindow Window,WindowState State)>();
        string result=Path.Combine(Program.ProjectRoot(),".cache","hotkey-result.json");bool success=false;string? error=null;
        try {
            string prefix="FlowDesk recovery shortcut test "+Guid.NewGuid().ToString("N");
            for(int i=0;i<2;i++) {
                var start=new ProcessStartInfo(Path.Combine(Program.ProjectRoot(),"tests","FlowDesk.Checks","bin","Debug","net10.0-windows","FlowDesk.Checks.exe")){UseShellExecute=false};
                start.Environment["DOTNET_ROOT_X64"]=Path.Combine(Program.ProjectRoot(),".tools","dotnet");start.ArgumentList.Add("--fixture");start.ArgumentList.Add(prefix+i);processes.Add(Process.Start(start)!);
            }
            DesktopWindow[] fixtures=Array.Empty<DesktopWindow>();
            for(int i=0;i<80;i++){fixtures=service.Enumerate().Where(w=>w.Title.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(w=>w.Title).ToArray();if(fixtures.Length==2)break;await Task.Delay(100);}
            if(fixtures.Length!=2)throw new InvalidOperationException("Shortcut fixtures did not open.");
            foreach(var fixture in fixtures)before.Add((fixture,service.CaptureState(fixture)));
            window.NoticeCenter.SetPreferences(new NotificationPreferences());window.Tabs.SelectedIndex=3;await Task.Delay(250);window.ModePanel.RefreshWindows();
            var inventory=((System.Collections.IEnumerable)window.ModePanel.Target.ItemsSource).Cast<DesktopWindow>().ToArray();
            window.ModePanel.Target.SelectedItem=inventory.Single(w=>w.Handle==fixtures[0].Handle);window.ModePanel.Distractions.SelectedItems.Add(inventory.Single(w=>w.Handle==fixtures[1].Handle));window.ModePanel.Minutes.Value=2;window.ModePanel.FullscreenTarget.IsChecked=true;
            var peer=new ButtonAutomationPeer(window.ModePanel.Buttons["Start focus"]);((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();await Task.Delay(300);
            if(!window.Modes.IsActive)throw new InvalidOperationException("Shortcut session did not start.");
            File.WriteAllText(result,JsonSerializer.Serialize(new {Ready=true,Complete=false,Target=fixtures[0].Title}));
            for(int i=0;i<900 && window.Modes.IsActive;i++)await Task.Delay(100);
            if(window.Modes.IsActive)throw new TimeoutException("Ctrl+Alt+F10 was not received during the test window.");
            success=before.All(s=>{var p=service.CaptureState(s.Window);return p.ShowCommand==s.State.ShowCommand && p.NormalBounds==s.State.NormalBounds;});
            if(!success)error="Shortcut did not restore fixture placement.";
        } catch(Exception ex){error=ex.ToString();}
        finally {
            window.Modes.End();foreach(var process in processes){if(!process.HasExited){process.CloseMainWindow();if(!process.WaitForExit(2000))process.Kill();}process.Dispose();}
            File.WriteAllText(result,JsonSerializer.Serialize(new {Complete=true,Success=success,Error=error}));Environment.ExitCode=success?0:1;window.Close();
        }
    }
    private sealed record Probe(Guid NoticeId,string Delivery,NotificationPreferences OriginalPreferences,string DataDirectory,Guid? WarmNoticeId=null);
    internal static int CleanupProbe() {
        var probe=JsonSerializer.Deserialize<Probe>(File.ReadAllText(Path.Combine(Program.ProjectRoot(),".cache","activation-probe.json")))!;
        if(!StringComparer.OrdinalIgnoreCase.Equals(probe.DataDirectory,Program.DataDirectory))return 2;
        Program.Notifications.Remove(probe.NoticeId).GetAwaiter().GetResult();
        var center=new NotificationCenter(Path.Combine(Program.DataDirectory,"notifications.json"),Program.Notifications);
        if(center.StorageError is not null)return 3;
        center.Dismiss(probe.NoticeId);if(probe.WarmNoticeId.HasValue){Program.Notifications.Remove(probe.WarmNoticeId.Value).GetAwaiter().GetResult();center.Dismiss(probe.WarmNoticeId.Value);}center.SetPreferences(probe.OriginalPreferences);return center.StorageError is null?0:4;
    }
    internal static async Task ToastProbe(MainWindow window) {
        var original=window.NoticeCenter.State.Preferences;
        window.NoticePanel.Banners.IsOn=true;
        var notice=window.NoticeCenter.Publish("FlowDesk activation test","Click this notification to reopen the FlowDesk notification center.");
        var warm=window.NoticeCenter.Publish("FlowDesk warm activation test","Click to open the existing FlowDesk notification center.");
        File.WriteAllText(Path.Combine(Program.ProjectRoot(),".cache","activation-probe.json"),JsonSerializer.Serialize(new {NoticeId=notice.Id,WarmNoticeId=warm.Id,Delivery=notice.Delivery,OriginalPreferences=original,DataDirectory=Program.DataDirectory}));
        await Task.Delay(1000);window.Close();
    }
}
