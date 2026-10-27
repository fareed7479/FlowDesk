using FlowDesk.Core;
using FlowDesk.Windows;
if(args.Contains("--fixture")) {
    var thread=new Thread(()=> {
        using var form=new System.Windows.Forms.Form { Text=args[1],Width=420,Height=320,StartPosition=System.Windows.Forms.FormStartPosition.Manual,Left=100,Top=100 };
        System.Windows.Forms.Application.Run(form);
    });
    thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join(); return;
}
void Check(bool condition,string name) { if(!condition) throw new Exception(name); Console.WriteLine("PASS "+name); }
var area=new Rect(-1920,0,1919,1080);
var cols=Layouts.Columns(area,3);
Check(cols.Sum(x=>x.Width)==area.Width && cols[2].X+cols[2].Width==-1,"split preserves odd width and negative monitor origin");
Check(cols[1].X==cols[0].X+cols[0].Width,"split is contiguous");
Check(Layouts.Clamp(new Rect(9000,-500,3000,2000),area)==area,"offscreen workspace bounds clamp");
bool invalid=false; try { Layouts.Columns(area,0); } catch(ArgumentOutOfRangeException) {invalid=true;}
Check(invalid,"empty selection rejected");
var a=new DesktopWindow(1,42,"C:\\test.exe","Test",new(0,0,400,300));
var saved=Workspaces.Capture([a]);
Check(Workspaces.Match(saved.Windows[0],[a,a]) is null,"ambiguous identities skipped");
var planned=RestorePlanner.Create(new Workspace(1,[saved.Windows[0],saved.Windows[0],new("missing","Absent",new(0,0,100,100))]),[a],_=>new(0,0,1920,1080));
Check(planned.Moves.Count==1 && planned.Skipped==2,"restore preview skips duplicates and missing windows");
Check(a.Bounds==new Rect(0,0,400,300),"restore planning leaves input unchanged");
var path=Path.Combine(AppContext.BaseDirectory,"workspace-test.json");
try { Workspaces.Save(path,saved); Check(Workspaces.Load(path).Windows[0]==saved.Windows[0],"workspace JSON roundtrip"); } finally { if(File.Exists(path)) File.Delete(path); }
if(args.Contains("--native")) {
    var service=new WindowService(); var inventory=service.Enumerate();
    Console.WriteLine($"Native inventory returned {inventory.Count} windows (titles omitted).");
    Check(service.WorkArea(0).Width>0,"native monitor work area");
}
if(args.Contains("--movement")) {
    var processes=new List<System.Diagnostics.Process>();
    try {
        var service=new WindowService();
        string prefix="FlowDesk fixture "+Guid.NewGuid().ToString("N");
        foreach(int i in new[]{0,1}) {
            var start=new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) {UseShellExecute=false};
            start.ArgumentList.Add("--fixture"); start.ArgumentList.Add(prefix+i);
            processes.Add(System.Diagnostics.Process.Start(start)!);
        }
        DesktopWindow[] fixtures=[];
        for(int attempt=0;attempt<50;attempt++) {
            fixtures=service.Enumerate().Where(w=>w.Title.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(w=>w.Title).ToArray();
            if(fixtures.Length==2) break;
            Thread.Sleep(100);
        }
        Check(fixtures.Length==2,"two controlled native test windows detected");
        var before=Workspaces.Capture(fixtures);
        var controller=new WorkspaceController(service);
var plan=controller.Preview(before);
Check(plan.Moves.Count==2 && plan.Skipped==0,"native restore preview matches test windows");
var targets=Layouts.Columns(service.WorkArea(fixtures[0].Handle),2);
        var applied=controller.Apply(new RestorePlan(fixtures.Select((w,i)=>new PlannedMove(w,targets[i])).ToArray(),0));
Check(applied.Applied==2 && applied.Errors.Count==0 && controller.CanUndo,"controller records successful split for undo");
        Thread.Sleep(250);
        var after=service.Enumerate();
        Check(fixtures.Select((w,i)=>after.Single(x=>x.Handle==w.Handle).Bounds==targets[i]).All(x=>x),"native split positions verified from OS");
        var undone=controller.Undo();
        Check(undone.Applied==2 && !controller.CanUndo,"undo consumes successful operation");
        Thread.Sleep(250);
        Check(before.Windows.All(w=>Workspaces.Match(w,service.Enumerate())?.Bounds==w.Bounds),"native undo rectangles verified from OS");
        foreach(var fixture in service.Enumerate().Where(w=>w.Title.StartsWith(prefix,StringComparison.Ordinal))) service.Move(fixture,new Rect(200,200,500,400));
        var restoreResult=controller.Apply(plan);
        Check(restoreResult.Applied==2 && restoreResult.Errors.Count==0,"preview plan applies after windows move");
        Thread.Sleep(250);
        Check(before.Windows.All(w=>Workspaces.Match(w,service.Enumerate())?.Bounds==w.Bounds),"preview restore rectangles verified from OS");
        processes[0].CloseMainWindow(); processes[0].WaitForExit(2000);
        var stale=controller.Apply(plan);
        Check(stale.Applied==1 && stale.Skipped==1,"stale preview skips closed window");
    } finally {
        foreach(var process in processes) {
            if(!process.HasExited) { process.CloseMainWindow(); if(!process.WaitForExit(2000)) process.Kill(); }
            process.Dispose();
        }
    }
}
