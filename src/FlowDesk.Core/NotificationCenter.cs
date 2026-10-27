using System.Text.Json;
namespace FlowDesk.Core;
public record NotificationPreferences(bool WindowsBanners=false, bool Sound=false, string[]? ExcludedApps=null);
public record Notice(Guid Id, string Title, string Message, DateTimeOffset Created, bool Read=false, string Delivery="In app") {
    public override string ToString() => $"{(Read?"":"● ")}{Title} — {Message}";
}
public record NoticeState(int Schema, NotificationPreferences Preferences, Notice[] Items);
public interface INoticeDelivery { string Send(Notice notice,bool sound); }
public sealed class NotificationCenter {
    private readonly string path;
    private readonly INoticeDelivery delivery;
    private bool readOnly;
    public bool Quiet {get;set;}
    public NoticeState State {get;private set;}=new(1,new(),[]);
    public string? StorageError {get;private set;}
    public int Unread => State.Items.Count(n=>!n.Read);
    public event Action? Changed;
    public NotificationCenter(string path,INoticeDelivery delivery) {
        this.path=path;this.delivery=delivery;
        try {
            if(File.Exists(path)) {
                if(new FileInfo(path).Length>2*1024*1024) throw new InvalidDataException("Notification storage exceeds its limit.");
                var state=JsonSerializer.Deserialize<NoticeState>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty notification storage.");
                Validate(state); State=state;
            }
        } catch(Exception ex) when(ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException) {readOnly=true;StorageError=ex.Message;}
    }
    private static void Validate(NoticeState s) {
        if(s.Schema!=1 || s.Preferences is null || s.Items is null || s.Items.Length>100 || (s.Preferences.ExcludedApps?.Length??0)>100 || s.Preferences.ExcludedApps?.Any(x=>string.IsNullOrWhiteSpace(x))==true || s.Items.Any(n=>n is null || n.Id==Guid.Empty || n.Title is null || n.Message is null || n.Title.Length>200 || n.Message.Length>2000 || n.Delivery is null) || s.Items.Select(n=>n.Id).Distinct().Count()!=s.Items.Length) throw new InvalidDataException("Unsupported notification storage.");
    }
    private void Commit() {
        if(!readOnly) {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                File.WriteAllText(path+".tmp",JsonSerializer.Serialize(State));File.Move(path+".tmp",path,true);StorageError=null;
            } catch(Exception ex) when(ex is InvalidDataException or IOException or UnauthorizedAccessException) {StorageError=ex.Message;}
        }
        Changed?.Invoke();
    }
    public void SetPreferences(NotificationPreferences preferences) {
        var next=State with {Preferences=preferences with {ExcludedApps=(preferences.ExcludedApps??[]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()}};
        Validate(next);State=next;Commit();
    }
    public Notice Publish(string title,string message) {
        if(string.IsNullOrWhiteSpace(title) || title.Length>200 || message.Length>2000) throw new ArgumentException("Invalid notification content.");
        var notice=new Notice(Guid.NewGuid(),title,message,DateTimeOffset.UtcNow);
        string result=Quiet?"Quiet mode":!State.Preferences.WindowsBanners?"In app":"Unavailable";
        if(!Quiet && State.Preferences.WindowsBanners) {try{result=delivery.Send(notice,State.Preferences.Sound);}catch(Exception ex){result="Delivery failed: "+ex.GetType().Name;}}
        notice=notice with {Delivery=result};State=State with {Items=State.Items.Prepend(notice).Take(100).ToArray()};Commit();return notice;
    }
    public void Read(Guid id) {State=State with {Items=State.Items.Select(n=>n.Id==id?n with {Read=true}:n).ToArray()};Commit();}
    public void ReadAll() {State=State with {Items=State.Items.Select(n=>n with {Read=true}).ToArray()};Commit();}
    public void Dismiss(Guid id) {State=State with {Items=State.Items.Where(n=>n.Id!=id).ToArray()};Commit();}
    public void Clear() {State=State with {Items=[]};Commit();}
    public static Guid? ActivationUri(string value) {
        if(value.Length>256 || !Uri.TryCreate(value,UriKind.Absolute,out var uri) || uri.Scheme!="flowdesk-control" || uri.Host!="notification" || uri.Query!="" || uri.Fragment!="" || uri.UserInfo!="" || uri.Port!=-1 || uri.AbsolutePath.Length!=33)return null;
        return Guid.TryParseExact(uri.AbsolutePath[1..],"N",out var id)?id:null;
    }
    public static Guid? ActivationCommandLine(string commandLine) {
        const string prefix="--notification-uri ";int index=commandLine.IndexOf(prefix,StringComparison.Ordinal);
        if(index<0 || (index>0 && !char.IsWhiteSpace(commandLine[index-1])))return null;
        string value=commandLine[(index+prefix.Length)..].Trim();if(value.Length>=2 && value[0]=='"' && value[^1]=='"')value=value[1..^1];
        return ActivationUri(value);
    }
    public static Guid? ActivationId(string arguments) {
        if(arguments.Length>512) return null;
        var parts=arguments.Split('&');
        if(parts.Length!=2 || parts[0]!="action=open" || !parts[1].StartsWith("id=",StringComparison.Ordinal)) return null;
        return Guid.TryParseExact(parts[1][3..],"N",out var id)?id:null;
    }
}
