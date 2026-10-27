namespace FlowDesk.Core;
public record PlannedMove(DesktopWindow Window, Rect Target);
public record RestorePlan(IReadOnlyList<PlannedMove> Moves, int Skipped);
public static class RestorePlanner {
    public static RestorePlan Create(Workspace workspace, IReadOnlyList<DesktopWindow> windows, Func<DesktopWindow,Rect> workArea) {
        var moves=new List<PlannedMove>();
        var used=new HashSet<nint>(); int skipped=0;
        foreach(var saved in workspace.Windows) {
            var match=Workspaces.Match(saved,windows);
            if(match is null || !used.Add(match.Handle)) { skipped++; continue; }
            moves.Add(new(match,Layouts.Clamp(saved.Bounds,workArea(match))));
        }
        return new(moves,skipped);
    }
}
