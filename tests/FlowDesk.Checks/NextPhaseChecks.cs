using System.Diagnostics;
using FlowDesk.Core;
using FlowDesk.Windows;
static class NextPhaseChecks {
    public static void Library(Action<bool,string> check,Workspace workspace) {
        var library=WorkspaceLibrary.Empty.Upsert("Writing",workspace);
        var id=library.Groups[0].Id;
        library=library.Upsert("Research",workspace,id);
        check(library.Groups.Length==1 && library.Groups[0].Name=="Research" && library.Groups[0].Id==id,"library replacement preserves group ID");
        bool rejected=false; try {library.Upsert("research",workspace);} catch(InvalidDataException){rejected=true;}
        check(rejected,"library rejects duplicate names regardless of case");
        rejected=false; try {library.Upsert(" ",workspace);} catch(InvalidDataException){rejected=true;}
        check(rejected,"library rejects blank names");
        rejected=false; try {LibraryStore.Validate(new WorkspaceLibrary(999,library.Groups));} catch(InvalidDataException){rejected=true;}
        check(rejected,"library rejects unsupported schemas");
        var path=Path.Combine(AppContext.BaseDirectory,"library-test.json");
        try {
            LibraryStore.Save(path,library); var loaded=LibraryStore.Load(path);
            check(loaded.Groups[0].Id==id && loaded.Groups[0].Workspace.Windows[0]==workspace.Windows[0],"named workspace persistence roundtrip");
            check(loaded.Remove(id).Groups.Length==0 && loaded.Groups.Length==1,"library removal leaves prior snapshot unchanged");
        } finally {if(File.Exists(path)) File.Delete(path);}
    }
    public static void Stage(Action<bool,string> check,WindowService service,DesktopWindow[] fixtures,List<Process> processes) {
        var start=new ProcessStartInfo(Environment.ProcessPath!) {UseShellExecute=false};
        string unrelatedTitle="FlowDesk unrelated fixture "+Guid.NewGuid().ToString("N");
        start.ArgumentList.Add("--fixture"); start.ArgumentList.Add(unrelatedTitle); processes.Add(Process.Start(start)!);
        DesktopWindow? unrelated=null;
        for(int i=0;i<50 && unrelated is null;i++) {unrelated=service.Enumerate().SingleOrDefault(w=>w.Title==unrelatedTitle);if(unrelated is null) Thread.Sleep(100);}
        check(unrelated is not null,"unrelated controlled test window detected");
        var unrelatedState=service.CaptureState(unrelated!);
        var initial=fixtures.Select(w=>service.CaptureState(w)).ToArray();
        var first=new WorkspaceGroup(Guid.NewGuid(),"First",Workspaces.Capture([fixtures[0]]));
        var second=new WorkspaceGroup(Guid.NewGuid(),"Second",Workspaces.Capture([fixtures[1]]));
        var shared=new WorkspaceGroup(Guid.NewGuid(),"Shared",Workspaces.Capture(fixtures));
        var stage=new StageManager(service);
        try {
            var result=stage.Activate(first);
            check(result.Presented==1 && result.Errors.Count==0 && !service.IsMinimized(fixtures[0]),"stage presents first group");
            result=stage.Activate(second);
            check(result.Presented==1 && result.Minimized==1 && service.IsMinimized(fixtures[0]) && !service.IsMinimized(fixtures[1]),"stage switch minimizes only previously staged inactive group");
            check(service.CaptureState(unrelated!)==unrelatedState,"stage leaves unrelated windows unchanged");
            result=stage.Activate(new WorkspaceGroup(Guid.NewGuid(),"Missing",new Workspace(1,[new("missing","missing",new(0,0,100,100))])));
            check(result.Presented==0 && stage.ActiveGroupId==second.Id && service.IsMinimized(fixtures[0]),"missing group does not disturb active stage");
            result=stage.Activate(shared);
            check(result.Presented==2 && result.Minimized==0 && fixtures.All(w=>!service.IsMinimized(w)),"shared group membership keeps both windows visible");
            result=stage.Leave(); Thread.Sleep(200);
            check(result.Presented==2 && result.Errors.Count==0 && !stage.IsActive && fixtures.Select((w,i)=>service.CaptureState(w)==initial[i]).All(x=>x),"leave stage restores original WINDOWPLACEMENT states");
            // Verify minimized state restoration, independently of normal rectangles.
            service.Minimize(fixtures[0]); var minimized=service.CaptureState(fixtures[0]);
            stage.Activate(first); stage.Leave();
            check(service.IsMinimized(fixtures[0]) && service.CaptureState(fixtures[0]).ShowCommand==minimized.ShowCommand,"leave stage preserves an initially minimized window");
            service.RestoreState(fixtures[0],initial[0]);
            service.RestoreState(fixtures[1],initial[1] with {ShowCommand=3});
            stage.Activate(second); stage.Leave();
            check(service.CaptureState(fixtures[1]).ShowCommand==3,"leave stage preserves an initially maximized window");
            service.RestoreState(fixtures[1],initial[1]);
            // Windows may reject foreground focus; activation still must restore a minimized window.
            service.Minimize(fixtures[1]); bool focused=service.Activate(fixtures[1]);
            check(!service.IsMinimized(fixtures[1]),"overview activation restores a minimized native window");
            Console.WriteLine("Foreground request accepted: "+focused+" (Windows policy dependent)");
            service.RestoreState(fixtures[1],initial[1]);
        } finally {
            stage.Leave();
            for(int i=0;i<fixtures.Length;i++) if(service.Resolve(fixtures[i]) is not null) service.RestoreState(fixtures[i],initial[i]);
        }
    }
}
