using FlowDesk.Core;
namespace FlowDesk.Windows;
public record MoveResult(int Applied, int Skipped, IReadOnlyList<string> Errors);
public sealed class WorkspaceController(WindowService service) {
    private readonly List<PlannedMove> undo=[];
    public bool CanUndo => undo.Count>0;
    public RestorePlan Preview(Workspace workspace) => RestorePlanner.Create(workspace,service.Enumerate(),w=>service.WorkArea(w.Handle));
    private DesktopWindow? Resolve(DesktopWindow expected) => service.Enumerate().SingleOrDefault(w=>w.Handle==expected.Handle && w.ProcessId==expected.ProcessId && StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,expected.ProcessPath) && w.Title==expected.Title);
    public MoveResult Apply(RestorePlan plan, bool clampToCurrentMonitor=true) {
        var captured=new List<PlannedMove>(); var errors=new List<string>(); int skipped=plan.Skipped;
        foreach(var move in plan.Moves) {
            try {
                var live=Resolve(move.Window);
                if(live is null) { skipped++; continue; }
                // Keep the live pre-move rectangle: a user may have moved it since preview.
                service.Move(live,clampToCurrentMonitor ? Layouts.Clamp(move.Target,service.WorkArea(live.Handle)) : move.Target);
                captured.Add(new(live,live.Bounds));
            } catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { errors.Add(ex.Message); }
        }
        if(captured.Count>0) { undo.Clear(); undo.AddRange(captured); }
        return new(captured.Count,skipped,errors);
    }
    public MoveResult Undo() {
        int restored=0,skipped=0; var errors=new List<string>(); var retry=new List<PlannedMove>();
        foreach(var move in undo) {
            try {
                var live=Resolve(move.Window);
                if(live is null) { skipped++; continue; }
                service.Move(live,Layouts.Clamp(move.Target,service.WorkArea(live.Handle))); restored++;
            } catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { retry.Add(move); errors.Add(ex.Message); }
        }
        undo.Clear(); undo.AddRange(retry);
        return new(restored,skipped,errors);
    }
}
