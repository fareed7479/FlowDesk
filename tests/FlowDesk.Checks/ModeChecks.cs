using System.Diagnostics;
using System.Runtime.InteropServices;
using FlowDesk.Core;
using FlowDesk.Windows;
internal static class ModeChecks {
    private sealed class Delivery : INoticeDelivery {public int Count;public bool Throw;public string Send(Notice n,bool sound){Count++;if(Throw)throw new IOException("test failure");return "Submitted";}}
    internal static void Notifications(Action<bool,string> check) {
        string path=Path.Combine(AppContext.BaseDirectory,"notification-test-"+Guid.NewGuid().ToString("N")+".json");var sink=new Delivery();
        try {
            var center=new NotificationCenter(path,sink);var first=center.Publish("Test","Local");
            check(sink.Count==0 && center.Unread==1,"notifications default to in-app delivery");
            center.SetPreferences(new(true,false,["C:\\app.exe","c:\\APP.exe"]));
            check(center.State.Preferences.ExcludedApps!.Length==1,"focus exclusions deduplicate paths without case sensitivity");
            var native=center.Publish("Test","Native");check(sink.Count==1 && native.Delivery=="Submitted","enabled notifications reach transport");
            center.Quiet=true;var quiet=center.Publish("Test","Quiet");check(sink.Count==1 && quiet.Delivery=="Quiet mode","quiet sessions suppress banner delivery");
            center.Quiet=false;sink.Throw=true;var failed=center.Publish("Test","Transport failure");check(failed.Delivery.StartsWith("Delivery failed") && center.State.Items.Contains(failed),"failed banner delivery retains local notification");
            center.Read(first.Id);check(center.State.Items.Single(n=>n.Id==first.Id).Read,"notification read action");
            center.Dismiss(native.Id);check(!center.State.Items.Any(n=>n.Id==native.Id),"notification dismissal");
            var loaded=new NotificationCenter(path,sink);check(loaded.State.Items.Length==center.State.Items.Length && loaded.State.Preferences.WindowsBanners,"notification history and preferences survive restart");
            loaded.ReadAll();check(loaded.Unread==0,"mark all notifications read");
            loaded.SetPreferences(new(false));for(int i=0;i<105;i++)loaded.Publish("Bounded",i.ToString());check(loaded.State.Items.Length==100,"notification history has a bounded capacity");
            var id=Guid.NewGuid();check(NotificationCenter.ActivationId("action=open&id="+id.ToString("N"))==id && NotificationCenter.ActivationId("action=execute&id="+id.ToString("N")) is null && NotificationCenter.ActivationId("action=open&id=invalid") is null,"notification activation accepts only valid open identifiers");
            check(NotificationCenter.ActivationUri("flowdesk-control://notification/"+id.ToString("N"))==id && NotificationCenter.ActivationUri("https://notification/"+id.ToString("N")) is null && NotificationCenter.ActivationUri("flowdesk-control://notification/"+id.ToString("N")+"?execute=x") is null,"notification protocol rejects other schemes and injected parameters");
            check(NotificationCenter.ActivationCommandLine("app.exe --notification-uri \"flowdesk-control://notification/"+id.ToString("N")+"\"")==id && NotificationCenter.ActivationCommandLine("--notification-uri flowdesk-control://notification/"+id.ToString("N")+" extra") is null,"notification command line accepts only a complete validated URI");
            loaded.Clear();check(loaded.State.Items.Length==0 && new NotificationCenter(path,sink).State.Items.Length==0,"notification clear persists");
            File.WriteAllText(path,System.Text.Json.JsonSerializer.Serialize(new NoticeState(99,new(),[])));var unsupported=new NotificationCenter(path,sink);unsupported.Publish("Unsaved","Preserve schema");check(unsupported.StorageError is not null && File.ReadAllText(path).Contains("99"),"unsupported notification schemas are preserved without startup failure");
            File.WriteAllText(path,"{broken");var corrupt=new NotificationCenter(path,sink);corrupt.Publish("Unsaved","History remains protected");check(corrupt.StorageError is not null && File.ReadAllText(path)=="{broken","invalid notification storage is preserved");
        } finally {if(File.Exists(path))File.Delete(path);}
    }
    internal static void Native(Action<bool,string> check,WindowService service,DesktopWindow[] fixtures,List<Process> processes) {
        string path=Path.Combine(AppContext.BaseDirectory,"mode-test-"+Guid.NewGuid().ToString("N")+".json");
        var modes=new DesktopModes(service,path);var initial=fixtures.Select(service.CaptureState).ToArray();long style=service.Style(fixtures[0]);
        try {
            modes.Start(fixtures[0],[fixtures[1]],[],1,false);
            check(modes.IsFocus && File.Exists(path) && service.IsMinimized(fixtures[1]),"focus persists recovery journal and minimizes selected distraction");
            check(modes.ShouldEnd(DateTimeOffset.UtcNow.AddMinutes(2)),"focus expiry is detected without waiting for real time");
            bool rejected=false;try{modes.Start(fixtures[0],[],[],1,false);}catch(InvalidOperationException){rejected=true;}check(rejected,"overlapping desktop sessions are rejected");
            var firstIdentity=modes.Session!.Windows[0].Identity;
            check(service.ResolveManaged(firstIdentity with {Marker="FlowDesk.Mode."+Guid.NewGuid().ToString("N")}) is null,"recovery rejects a window without its unique marker");
            SetWindowText(fixtures[0].Handle,fixtures[0].Title+" changed");
            var restarted=new DesktopModes(service,path);var result=restarted.End();
            check(result.Restored==2 && result.Errors.Length==0 && !restarted.IsActive && !File.Exists(path),"reloaded recovery journal restores interrupted focus session");
            SetWindowText(fixtures[0].Handle,fixtures[0].Title);
            Thread.Sleep(250); check(fixtures.Select((w,i)=>service.CaptureState(w).NormalBounds==initial[i].NormalBounds && service.CaptureState(w).ShowCommand==initial[i].ShowCommand).All(x=>x),"focus recovery restores placements despite a changed document title");
            modes=new(service,path);modes.Start(fixtures[0],[fixtures[1]],[fixtures[1].ProcessPath.ToUpperInvariant()],1,false);
            check(!service.IsMinimized(fixtures[1]) && modes.Session!.Windows.Length==1,"focus preserves excluded applications");modes.End();
            modes.Start(fixtures[0],[],[],null,true);var live=service.ResolveManaged(modes.Session!.Windows[0].Identity)!;
            check(live.Bounds==service.MonitorBounds(live.Handle) && (service.Style(live)&0x00CF0000L)==0,"borderless fullscreen fills monitor and removes window frame");
            result=modes.End();check(result.Errors.Length==0 && service.Style(fixtures[0])==style && service.CaptureState(fixtures[0]).NormalBounds==initial[0].NormalBounds && service.CaptureState(fixtures[0]).ShowCommand==initial[0].ShowCommand,"fullscreen exit restores original frame and WINDOWPLACEMENT");
            modes.Start(fixtures[0],[],[],null,true);var dynamicWindow=service.ResolveManaged(modes.Session!.Windows[0].Identity)!;SetWindowLongPtr(dynamicWindow.Handle,-16,(nint)(service.Style(dynamicWindow)|0x08000000L));modes.End();check((service.Style(fixtures[0])&0x08000000L)!=0,"fullscreen restoration preserves application-owned dynamic style flags");SetWindowLongPtr(fixtures[0].Handle,-16,(nint)style);
            service.RestoreState(fixtures[0],initial[0] with {ShowCommand=3});modes.Start(fixtures[0],[],[],null,true);modes.End();check(service.CaptureState(fixtures[0]).ShowCommand==3,"fullscreen restores originally maximized target");service.RestoreState(fixtures[0],initial[0]);
            service.Minimize(fixtures[0]);modes.Start(fixtures[0],[],[],1,false);modes.End();check(service.IsMinimized(fixtures[0]),"focus restores an initially minimized target");service.RestoreState(fixtures[0],initial[0]);
            service.Minimize(fixtures[0]);modes.Start(fixtures[0],[],[],null,true);modes.End();check(service.IsMinimized(fixtures[0]),"fullscreen restores an initially minimized target");service.RestoreState(fixtures[0],initial[0]);
            var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};string title="FlowDesk modes closed "+Guid.NewGuid().ToString("N");start.ArgumentList.Add("--fixture");start.ArgumentList.Add(title);var process=Process.Start(start)!;processes.Add(process);
            DesktopWindow? target=null;for(int i=0;i<50 && target is null;i++){target=service.Enumerate().SingleOrDefault(w=>w.Title==title);if(target is null)Thread.Sleep(100);}
            check(target is not null,"focus target-close fixture detected");modes.Start(target!,[fixtures[1]],[],1,false);process.CloseMainWindow();process.WaitForExit(2000);
            check(modes.ShouldEnd(DateTimeOffset.UtcNow),"closing focus target requests session restoration");result=modes.End();check(result.Skipped==1 && !service.IsMinimized(fixtures[1]),"closed target is skipped while distractions are restored");
            File.WriteAllText(path,System.Text.Json.JsonSerializer.Serialize(new ModeJournal(99,"Focus",0,null,[])));var unsupportedJournal=new DesktopModes(service,path);check(unsupportedJournal.RecoveryError is not null && unsupportedJournal.IsActive,"unsupported recovery schemas block mutations without startup failure");
            File.WriteAllText(path,"{broken");var invalid=new DesktopModes(service,path);check(invalid.IsActive && invalid.RecoveryError is not null && invalid.End().Errors.Length==1,"corrupt recovery journal blocks new mutations");
        } finally {modes.End();SetWindowLongPtr(fixtures[0].Handle,-16,(nint)style);SetWindowText(fixtures[0].Handle,fixtures[0].Title);for(int i=0;i<fixtures.Length;i++)if(service.Resolve(fixtures[i]) is not null)service.RestoreState(fixtures[i],initial[i]);if(File.Exists(path))File.Delete(path);}
    }
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]private static extern nint SetWindowLongPtr(nint window,int index,nint value);
    [DllImport("user32.dll",EntryPoint="SetWindowTextW",CharSet=CharSet.Unicode)] private static extern bool SetWindowText(nint window,string text);
}
