using System.Text.Json;
using FlowDesk.Core;
namespace FlowDesk.Windows;
public record ModeJournal(int Schema,string Kind,long Target,DateTimeOffset? Ends,ManagedSnapshot[] Windows);
public record ModeResult(int Restored,int Skipped,string[] Errors);
public sealed class DesktopModes {
    private readonly WindowService service;
    private readonly string path;
    private bool unreadable;
    public ModeJournal? Session {get;private set;}
    public string? RecoveryError {get;private set;}
    public bool IsActive => Session is not null || RecoveryError is not null;
    public bool IsFocus => Session?.Kind=="Focus";
    public DesktopModes(WindowService service,string path) {
        this.service=service;this.path=path;
        try {
            if(File.Exists(path)) {
                if(new FileInfo(path).Length>1024*1024) throw new InvalidDataException("Recovery journal is too large.");
                var j=JsonSerializer.Deserialize<ModeJournal>(File.ReadAllText(path)) ?? throw new InvalidDataException("Recovery journal is empty.");
                if(j.Schema!=1 || j.Kind is not ("Focus" or "Fullscreen") || j.Windows is null || j.Windows.Length is <1 or >32 || j.Windows.Any(w=>w is null || w.Identity is null || w.Placement is null || w.Bounds is null || w.Identity.Handle==0 || w.Identity.ProcessId<1 || w.Identity.ProcessStarted<1 || string.IsNullOrWhiteSpace(w.Identity.ProcessPath) || w.Identity.Marker is null || !w.Identity.Marker.StartsWith("FlowDesk.Mode.",StringComparison.Ordinal) || !Guid.TryParseExact(w.Identity.Marker[14..],"N",out _) || w.Placement.NormalBounds is null || w.Placement.NormalBounds.Width<1 || w.Placement.NormalBounds.Height<1)) throw new InvalidDataException("Unsupported recovery journal; preserve it for manual recovery.");
                Session=j;
            }
        } catch(Exception ex) when(ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException) {RecoveryError=ex.Message;unreadable=true;}
    }
    private void Save() {
        if(Session is null) {if(File.Exists(path)) File.Delete(path);return;}
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(Session));File.Move(path+".tmp",path,true);
    }
    public void Start(DesktopWindow target,IEnumerable<DesktopWindow> distractions,IEnumerable<string> exclusions,int? minutes,bool fullscreen) {
        if(IsActive) throw new InvalidOperationException("End or recover the current session first.");
        if(minutes is <1 or >240 || (minutes is null && !fullscreen)) throw new ArgumentOutOfRangeException(nameof(minutes));
        var protectedApps=exclusions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected=distractions.Where(w=>w.Handle!=target.Handle && !protectedApps.Contains(w.ProcessPath)).Prepend(target).DistinctBy(w=>w.Handle).ToArray();
        if(selected.Length>32) throw new InvalidOperationException("Select at most 31 distractions.");
        var captured=new List<ManagedSnapshot>();
        try {foreach(var window in selected) captured.Add(service.Track(window));}
        catch {foreach(var snapshot in captured) service.Forget(snapshot.Identity);throw;}
        Session=new(1,minutes.HasValue?"Focus":"Fullscreen",target.Handle.ToInt64(),minutes.HasValue?DateTimeOffset.UtcNow.AddMinutes(minutes.Value):null,captured.ToArray());
        try {Save();} catch {foreach(var s in captured) service.Forget(s.Identity);Session=null;throw;}
        try {
            if(fullscreen) service.MakeFullscreen(captured[0]);
            foreach(var snapshot in captured.Skip(1)) {var live=service.ResolveManaged(snapshot.Identity);if(live is not null) service.Minimize(live);}
            var active=service.ResolveManaged(captured[0].Identity);if(active is not null) service.Activate(active);
        } catch {End();throw;}
    }
    public ModeResult End() {
        if(unreadable) return new(0,0,[RecoveryError!]);
        RecoveryError=null;
        if(Session is null) {try{Save();return new(0,0,[]);}catch(Exception ex) when(ex is InvalidDataException or IOException or UnauthorizedAccessException){RecoveryError=ex.Message;return new(0,0,[ex.Message]);}}
        int restored=0,skipped=0;var errors=new List<string>();var pending=new List<ManagedSnapshot>();
        foreach(var snapshot in Session.Windows) {
            try {if(service.RestoreManaged(snapshot)) restored++;else skipped++;}
            catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) {pending.Add(snapshot);errors.Add(ex.Message);}
        }
        Session=pending.Count==0?null:Session with {Windows=pending.ToArray()};
        try {Save();} catch(Exception ex) when(ex is InvalidDataException or IOException or UnauthorizedAccessException) {RecoveryError=ex.Message;errors.Add(ex.Message);}
        return new(restored,skipped,errors.ToArray());
    }
    public bool ShouldEnd(DateTimeOffset now) => Session is not null && ((Session.Ends.HasValue && now>=Session.Ends.Value) || !Session.Windows.Any(w=>w.Identity.Handle==Session.Target && service.ResolveManaged(w.Identity) is not null));
}
