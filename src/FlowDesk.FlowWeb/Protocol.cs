using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace FlowDesk.FlowWeb;
public record Packet(int V,string Id,string Type,JsonElement Data);
public record WebTab(int Id,int WindowId,string Title,string Url,bool Active,bool Audible,bool Muted,bool Pinned) {
    public override string ToString()=>$"{(Pinned?"📌 ":"")}{(Muted?"Muted · ":"")}{Title} — {Url}";
}
public record BrowserSnapshot(string Browser,WebTab[] Tabs);
public record BrowserHello(string Browser);
public record Attach(string Origin,string Token,string Browser);
public record Command(string Action,int? TabId=null,bool? Value=null,string[]? Urls=null);
public record CommandResult(bool Ok,string? Error=null);
public record BridgeConfig(int Schema,bool Enabled,string PipeName,string Token);
public static partial class Wire {
    public const string HostName="com.flowdesk.flowweb";
    public const string ExtensionId="dinpkkjphjbeadjlhndbgaaciimepnom";
    public const string Origin="chrome-extension://"+ExtensionId+"/";
    public const int MaximumBytes=262144;
    public static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    public static Packet Make(string type,object data,string? id=null)=>new(1,id??Guid.NewGuid().ToString("N"),type,JsonSerializer.SerializeToElement(data,Json));
    public static T Body<T>(Packet p)=>p.Data.Deserialize<T>(Json)??throw new InvalidDataException("Missing message body.");
    public static bool AllowedOrigin(string value)=>value==Origin || value==Origin.TrimEnd('/');
    public static bool WebUrl(string value)=>value.Length<=2048 && Uri.TryCreate(value,UriKind.Absolute,out var u) && u.Scheme is "http" or "https" && u.UserInfo.Length==0;
    public static void Validate(Packet p) {
        if(p.V!=1 || p.Id is null || !IdPattern().IsMatch(p.Id) || p.Type is not ("hello" or "attach" or "ready" or "snapshot" or "command" or "result" or "error") || p.Data.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Invalid FlowWeb envelope.");
    }
    public static void Validate(BrowserSnapshot s) {
        if(s.Browser is not ("Chrome" or "Edge" or "Chromium") || s.Tabs is null || s.Tabs.Length>50 || s.Tabs.Any(t=>t is null || t.Id<1 || t.WindowId<1 || t.Title is null || t.Title.Length>200 || t.Url is null || !WebUrl(t.Url)) || s.Tabs.Select(t=>t.Id).Distinct().Count()!=s.Tabs.Length)throw new InvalidDataException("Invalid browser snapshot.");
    }
    public static void Validate(Command c) {
        if(c.Action is "activate" or "mute" or "pin") {
            if(c.TabId is null or <1 || c.Urls is not null || (c.Action=="activate"?c.Value is not null:c.Value is null))throw new InvalidDataException("Invalid tab command.");
        } else if(c.Action=="openWorkspace") {
            if(c.TabId is not null || c.Value is not null || c.Urls is null || c.Urls.Length is <1 or >25 || c.Urls.Any(u=>u is null || !WebUrl(u)))throw new InvalidDataException("Invalid browser workspace.");
        } else throw new InvalidDataException("Unsupported browser command.");
    }
    public static async Task<Packet?> Read(Stream stream,CancellationToken ct=default) {
        byte[] header=new byte[4];int first=await stream.ReadAsync(header,ct);if(first==0)return null;
        await stream.ReadExactlyAsync(header.AsMemory(first),ct);uint length=BinaryPrimitives.ReadUInt32LittleEndian(header);
        if(length is <1 or >MaximumBytes)throw new InvalidDataException("FlowWeb frame exceeds its limit.");
        byte[] body=new byte[length];await stream.ReadExactlyAsync(body,ct);
        var packet=JsonSerializer.Deserialize<Packet>(body,Json)??throw new InvalidDataException("Missing FlowWeb packet.");Validate(packet);return packet;
    }
    public static async Task Write(Stream stream,Packet packet,CancellationToken ct=default) {
        Validate(packet);byte[] body=JsonSerializer.SerializeToUtf8Bytes(packet,Json);if(body.Length>MaximumBytes)throw new InvalidDataException("FlowWeb frame exceeds its limit.");
        byte[] header=new byte[4];BinaryPrimitives.WriteUInt32LittleEndian(header,(uint)body.Length);await stream.WriteAsync(header,ct);await stream.WriteAsync(body,ct);await stream.FlushAsync(ct);
    }
    [GeneratedRegex("^[a-f0-9]{32}$")]private static partial Regex IdPattern();
}
public record WebWorkspace(Guid Id,string Name,string[] Urls) {public override string ToString()=>$"{Name} · {Urls.Length} tabs";}
public record WebLibrary(int Schema,WebWorkspace[] Workspaces) {
    public static WebLibrary Empty=>new(1,[]);
    public WebLibrary Add(string name,IEnumerable<string> urls) {
        var next=new WebLibrary(1,Workspaces.Append(new WebWorkspace(Guid.NewGuid(),name.Trim(),urls.ToArray())).ToArray());Validate(next);return next;
    }
    public WebLibrary Remove(Guid id)=>new(1,Workspaces.Where(w=>w.Id!=id).ToArray());
    public static void Validate(WebLibrary l) {
        if(l.Schema!=1 || l.Workspaces is null || l.Workspaces.Length>20 || l.Workspaces.Any(w=>w is null || w.Id==Guid.Empty || string.IsNullOrWhiteSpace(w.Name) || w.Name.Length>64 || w.Name.Any(char.IsControl) || w.Name!=w.Name.Trim() || w.Urls is null || w.Urls.Length is <1 or >25 || w.Urls.Any(u=>u is null || !Wire.WebUrl(u))) || l.Workspaces.Select(w=>w.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=l.Workspaces.Length || l.Workspaces.Select(w=>w.Id).Distinct().Count()!=l.Workspaces.Length)throw new InvalidDataException("Invalid browser workspace library.");
    }
    public static WebLibrary Load(string path) {if(!File.Exists(path))return Empty;if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException("Browser library is too large.");var l=JsonSerializer.Deserialize<WebLibrary>(File.ReadAllText(path),Wire.Json)??throw new InvalidDataException("Empty browser library.");Validate(l);return l;}
    public void Save(string path) {Validate(this);var json=JsonSerializer.Serialize(this,Wire.Json);if(System.Text.Encoding.UTF8.GetByteCount(json)>2*1024*1024)throw new InvalidDataException("Browser library is too large.");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllText(path+".tmp",json);File.Move(path+".tmp",path,true);}
}
