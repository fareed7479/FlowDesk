using System.Text.Json;
using FlowDesk.Core;
namespace FlowDesk.Windows;
public record StageResult(int Presented,int Minimized,int Skipped,IReadOnlyList<string> Errors,bool? FocusAccepted=null);
public record StageJournal(int Schema,Guid? ActiveGroup,ManagedSnapshot[] Windows);
public sealed class StageManager {
    private readonly WindowService service;
    private readonly string? journalPath;
    private readonly Dictionary<nint,ManagedSnapshot> original=[];
    private bool unreadable;
    public string? RecoveryError {get;private set;}
    public bool IsActive=>original.Count>0 || RecoveryError is not null;
    public Guid? ActiveGroupId {get;private set;}
    public StageManager(WindowService service,string? journalPath=null) {
        this.service=service;this.journalPath=journalPath;
        try {
            if(journalPath is not null && File.Exists(journalPath)) {
                if(new FileInfo(journalPath).Length>1024*1024)throw new InvalidDataException("Stage journal is too large.");
                var data=JsonSerializer.Deserialize<StageJournal>(File.ReadAllText(journalPath))??throw new InvalidDataException("Stage journal is empty.");
                if(data.Schema!=1 || data.Windows is null || data.Windows.Length>100 || data.Windows.Any(s=>s is null || s.Identity is null || s.Placement?.NormalBounds is null || s.Bounds is null || s.Identity.Handle==0 || s.Identity.ProcessId<1 || s.Identity.ProcessStarted<1 || string.IsNullOrWhiteSpace(s.Identity.ProcessPath) || s.Identity.Marker is null || !s.Identity.Marker.StartsWith("FlowDesk.Mode.",StringComparison.Ordinal) || !Guid.TryParseExact(s.Identity.Marker[14..],"N",out _) || s.Placement.NormalBounds.Width<1 || s.Placement.NormalBounds.Height<1) || data.Windows.Select(s=>s.Identity.Handle).Distinct().Count()!=data.Windows.Length)throw new InvalidDataException("Unsupported stage recovery journal. Preserve it for manual recovery.");
                foreach(var s in data.Windows)original.Add((nint)s.Identity.Handle,s);
                ActiveGroupId=data.ActiveGroup;
            }
        } catch(Exception ex)when(ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException){RecoveryError=ex.Message;unreadable=true;}
    }
    private void Save() {
        if(journalPath is null)return;
        if(original.Count==0){if(File.Exists(journalPath))File.Delete(journalPath);return;}
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(journalPath))!);
        File.WriteAllText(journalPath+".tmp",JsonSerializer.Serialize(new StageJournal(1,ActiveGroupId,original.Values.ToArray())));
        File.Move(journalPath+".tmp",journalPath,true);
    }
    public StageResult Activate(WorkspaceGroup group) {
        if(RecoveryError is not null)throw new InvalidOperationException("Resolve stage recovery before switching groups. "+RecoveryError);
        var plan=RestorePlanner.Create(group.Workspace,service.Enumerate(),w=>service.WorkArea(w.Handle));
        if(plan.Moves.Count==0)return new(0,0,plan.Skipped,["No matching windows. Existing stage is unchanged."]);
        if(original.Count+plan.Moves.Count(m=>!original.ContainsKey(m.Window.Handle))>100)throw new InvalidOperationException("Leave Stage before managing more than 100 windows.");
        var added=new List<ManagedSnapshot>();
        try {
            foreach(var move in plan.Moves) {
                if(original.TryGetValue(move.Window.Handle,out var old) && service.ResolveManaged(old.Identity) is null)original.Remove(move.Window.Handle);
                if(!original.ContainsKey(move.Window.Handle)){var s=service.Track(move.Window);original.Add(move.Window.Handle,s);added.Add(s);}
            }
            Save(); // Durable original states precede every first mutation.
        } catch {foreach(var s in added){original.Remove((nint)s.Identity.Handle);service.Forget(s.Identity);}throw;}
        var errors=new List<string>();var desired=plan.Moves.Select(m=>m.Window.Handle).ToHashSet();int presented=0,minimized=0,skipped=plan.Skipped;DesktopWindow? focus=null;bool? focused=null;
        foreach(var move in plan.Moves) {
            try {var live=service.ResolveManaged(original[move.Window.Handle].Identity);if(live is null){skipped++;continue;}service.Move(live,move.Target);focus??=live;presented++;}
            catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException){errors.Add(ex.Message);}
        }
        if(presented>0) {
            foreach(var pair in original.ToArray()) {
                if(desired.Contains(pair.Key))continue;
                try {var live=service.ResolveManaged(pair.Value.Identity);if(live is null){original.Remove(pair.Key);skipped++;continue;}service.Minimize(live);minimized++;}
                catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException){errors.Add(ex.Message);}
            }
            ActiveGroupId=group.Id;
            try{focused=service.Activate(focus!);}catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException){errors.Add(ex.Message);}
        }
        try{Save();}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){RecoveryError=ex.Message;errors.Add(ex.Message);}
        return new(presented,minimized,skipped,errors,focused);
    }
    public StageResult Leave() {
        if(unreadable)return new(0,0,0,[RecoveryError!]);
        RecoveryError=null;int restored=0,skipped=0;var errors=new List<string>();
        foreach(var pair in original.ToArray()) {
            try {if(service.RestoreManaged(pair.Value))restored++;else skipped++;original.Remove(pair.Key);}
            catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException){errors.Add(ex.Message);}
        }
        if(original.Count==0)ActiveGroupId=null;
        try{Save();}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){RecoveryError=ex.Message;errors.Add(ex.Message);}
        return new(restored,0,skipped,errors);
    }
}
