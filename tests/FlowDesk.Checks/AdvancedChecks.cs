using System.Diagnostics;
using System.Runtime.InteropServices;
using FlowDesk.Core;
using FlowDesk.Windows;
internal static class AdvancedChecks {
    internal static void Preferences(Action<bool,string> check) {
        var p=DockPreferences.Empty.Pin(@"C:\Example\Editor.exe").Pin(@"c:\example\EDITOR.exe");
        check(p.Pins.Length==1,"dock pins deduplicate executable paths");
        check(p.Unpin(@"c:\example\editor.exe").Pins.Length==0 && p.Pins.Length==1,"dock unpin preserves earlier snapshot");
        bool rejected=false;try{p.Pin("relative.exe");}catch(InvalidDataException){rejected=true;}check(rejected,"dock rejects relative executable paths");
        rejected=false;try{p.Pin(@"C:\Example\document.txt");}catch(InvalidDataException){rejected=true;}check(rejected,"dock rejects non-executable pins");
        rejected=false;try{DockPreferences.Validate(new(99,[],false,null));}catch(InvalidDataException){rejected=true;}check(rejected,"dock rejects unsupported schemas");
        string path=Path.Combine(AppContext.BaseDirectory,"dock-check-"+Guid.NewGuid().ToString("N")+".json");
        try {
            (p with {AutoHide=true,Monitor="DISPLAY-TEST"}).Save(path);var loaded=DockPreferences.Load(path);
            check(loaded.AutoHide && loaded.Monitor=="DISPLAY-TEST" && loaded.Pins.SequenceEqual(p.Pins),"dock preferences persist pins, auto-hide and display");
            File.WriteAllText(path,"{bad");rejected=false;try{DockPreferences.Load(path);}catch(System.Text.Json.JsonException){rejected=true;}check(rejected && File.ReadAllText(path)=="{bad","invalid dock storage is preserved");
        }finally{if(File.Exists(path))File.Delete(path);}
    }
    internal static void Seed(string journal,string prefix) {
        if(!prefix.StartsWith("FlowDesk fixture ",StringComparison.Ordinal))throw new InvalidOperationException("Only controlled fixtures may be staged by this check.");
        var service=new WindowService();var fixtures=service.Enumerate().Where(w=>w.Title.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(w=>w.Title).ToArray();
        if(fixtures.Length!=2)throw new InvalidOperationException("Expected exactly two fixtures.");
        var stage=new StageManager(service,journal);
        stage.Activate(new(Guid.NewGuid(),"First",Workspaces.Capture([fixtures[0]])));
        var r=stage.Activate(new(Guid.NewGuid(),"Second",Workspaces.Capture([fixtures[1]])));
        if(r.Errors.Count>0 || !service.IsMinimized(fixtures[0]))throw new InvalidOperationException("Could not seed interrupted stage.");
    }
    internal static void Native(Action<bool,string> check,WindowService service,DesktopWindow[] fixtures) {
        var displays=service.Displays();check(displays.Count>0 && displays.Count(d=>d.Primary)==1,"display inventory identifies a primary display");
        check(displays.All(d=>d.Bounds.Width>0 && d.Bounds.Height>0 && d.WorkArea.Width>0 && d.WorkArea.Height>0),"display inventory returns usable physical bounds and work areas");
        check(service.DisplayFor(fixtures[0]) is not null && service.Dpi(fixtures[0])>0,"window display and per-window DPI resolve on native fixtures");
        var initial=fixtures.Select(service.CaptureState).ToArray();
        string path=Path.Combine(AppContext.BaseDirectory,"stage-check-"+Guid.NewGuid().ToString("N")+".json");
        try {
            var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};
            start.ArgumentList.Add("--stage-seed");start.ArgumentList.Add(path);start.ArgumentList.Add(fixtures[0].Title[..^1]);
            using(var child=Process.Start(start)!){if(!child.WaitForExit(20000))throw new TimeoutException("Stage seed timed out.");check(child.ExitCode==0 && File.Exists(path) && service.IsMinimized(fixtures[0]),"separate process leaves a durable interrupted stage journal");}
            var recovered=new StageManager(service,path);check(recovered.IsActive && recovered.RecoveryError is null,"fresh stage manager loads the interrupted journal");
            SetWindowText(fixtures[0].Handle,fixtures[0].Title+" changed");
            var result=recovered.Leave();
            Thread.Sleep(300);
            var live=service.Enumerate().Where(w=>fixtures.Any(f=>f.Handle==w.Handle)).OrderBy(w=>w.Handle==fixtures[0].Handle?0:1).ToArray();
            check(result.Presented==2 && !recovered.IsActive && !File.Exists(path),"stage recovery restores renamed managed windows and clears the journal");
            check(live.Select((w,i)=>service.CaptureState(w).ShowCommand==initial[i].ShowCommand && service.CaptureState(w).NormalBounds==initial[i].NormalBounds).All(b=>b),"stage restart recovery restores native show states and normal bounds");
            SetWindowText(fixtures[0].Handle,fixtures[0].Title);
            File.WriteAllText(path,"{\"Schema\":99,\"Windows\":[]}");var invalid=new StageManager(service,path);
            check(invalid.RecoveryError is not null && invalid.IsActive && invalid.Leave().Errors.Count==1 && File.ReadAllText(path).Contains("99"),"unsupported stage journal is preserved and blocks mutations");
            File.Delete(path);File.WriteAllText(path,"blocked");
            var beforeFailure=service.CaptureState(fixtures[0]);var blockedStage=new StageManager(service,Path.Combine(path,"journal.json"));bool failed=false;
            try{blockedStage.Activate(new(Guid.NewGuid(),"Unwritable",Workspaces.Capture([fixtures[0]])));}catch(IOException){failed=true;}
            check(failed && service.CaptureState(fixtures[0])==beforeFailure,"stage refuses mutation when original states cannot be journaled");
        }finally {
            SetWindowText(fixtures[0].Handle,fixtures[0].Title);
            var stage=new StageManager(service,path);if(stage.RecoveryError is null)stage.Leave();
            for(int i=0;i<fixtures.Length;i++)if(service.Resolve(fixtures[i]) is not null)service.RestoreState(fixtures[i],initial[i]);
            if(File.Exists(path))File.Delete(path);
        }
    }
    [DllImport("user32.dll",EntryPoint="SetWindowTextW",CharSet=CharSet.Unicode)]private static extern bool SetWindowText(nint h,string text);
}
