using FlowDesk.Core;
namespace FlowDesk.Windows;
public record StageResult(int Presented, int Minimized, int Skipped, IReadOnlyList<string> Errors, bool? FocusAccepted=null);
public sealed class StageManager(WindowService service) {
    private sealed record Tracked(DesktopWindow Window, WindowState State);
    private readonly Dictionary<nint,Tracked> original=[];
    public bool IsActive => original.Count>0;
    public Guid? ActiveGroupId { get; private set; }
    public StageResult Activate(WorkspaceGroup group) {
        var plan=RestorePlanner.Create(group.Workspace,service.Enumerate(),w=>service.WorkArea(w.Handle));
        if(plan.Moves.Count==0) return new(0,0,plan.Skipped,["No matching windows. Existing stage is unchanged."]);
        var errors=new List<string>(); var desired=plan.Moves.Select(m=>m.Window.Handle).ToHashSet(); int presented=0,minimized=0,skipped=plan.Skipped; DesktopWindow? focus=null; bool? focusAccepted=null;
        // Present the requested group before minimizing any previously staged windows.
        foreach(var move in plan.Moves) {
            try {
                var live=service.Resolve(move.Window);
                if(live is null) { skipped++; continue; }
                if(original.TryGetValue(live.Handle,out var previous) && service.Resolve(previous.Window) is null) original.Remove(live.Handle);
                if(!original.ContainsKey(live.Handle)) original.Add(live.Handle,new(live,service.CaptureState(live)));
                service.Move(live,move.Target); focus ??=live; presented++;
            } catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { errors.Add(ex.Message); }
        }
        if(presented>0) {
            foreach(var pair in original.ToArray()) {
                if(desired.Contains(pair.Key)) continue;
                try {
                    var live=service.Resolve(pair.Value.Window);
                    if(live is null) { original.Remove(pair.Key); skipped++; continue; }
                    service.Minimize(live); minimized++;
                } catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { errors.Add(ex.Message); }
            }
            ActiveGroupId=group.Id;
            try {focusAccepted=service.Activate(focus!);} catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) {errors.Add(ex.Message);}
        }
        return new(presented,minimized,skipped,errors,focusAccepted);
    }
    public StageResult Leave() {
        int restored=0,skipped=0; var errors=new List<string>();
        foreach(var pair in original.ToArray()) {
            try {
                var live=service.Resolve(pair.Value.Window);
                if(live is null) { original.Remove(pair.Key); skipped++; continue; }
                service.RestoreState(live,pair.Value.State); original.Remove(pair.Key); restored++;
            } catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { errors.Add(ex.Message); }
        }
        if(!IsActive) ActiveGroupId=null;
        return new(restored,0,skipped,errors);
    }
}
