using System.Text.Json;
namespace FlowDesk.Core;
public record DockPreferences(int Schema,string[] Pins,bool AutoHide,string? Monitor) {
    public static DockPreferences Empty=>new(1,[],false,null);
    public static DockPreferences Load(string path) {
        if(!File.Exists(path))return Empty;
        if(new FileInfo(path).Length>128*1024)throw new InvalidDataException("Dock preferences are too large.");
        var p=JsonSerializer.Deserialize<DockPreferences>(File.ReadAllText(path))??throw new InvalidDataException("Empty dock preferences.");
        Validate(p);return p;
    }
    public static void Validate(DockPreferences p) {
        if(p.Schema!=1 || p.Pins is null || p.Pins.Length>24 || p.Pins.Any(s=>string.IsNullOrWhiteSpace(s)||!Path.IsPathFullyQualified(s)||!s.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)) || p.Pins.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=p.Pins.Length || p.Monitor?.Length>64)throw new InvalidDataException("Unsupported dock preferences.");
    }
    public void Save(string path) {Validate(this);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllText(path+".tmp",JsonSerializer.Serialize(this));File.Move(path+".tmp",path,true);}
    public DockPreferences Pin(string path) {var next=this with {Pins=Pins.Append(path).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()};Validate(next);return next;}
    public DockPreferences Unpin(string path)=>this with {Pins=Pins.Where(p=>!StringComparer.OrdinalIgnoreCase.Equals(p,path)).ToArray()};
}
