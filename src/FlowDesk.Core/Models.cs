using System.Text.Json;
namespace FlowDesk.Core;
public record Rect(int X, int Y, int Width, int Height);
public record DesktopWindow(nint Handle, int ProcessId, string ProcessPath, string Title, Rect Bounds) {
    public override string ToString() => $"{Path.GetFileNameWithoutExtension(ProcessPath)} — {Title}";
}
public record SavedWindow(string ProcessPath, string Title, Rect Bounds);
public record Workspace(int Schema, SavedWindow[] Windows);
public static class Layouts {
    public static Rect[] Columns(Rect area, int count) {
        if (count < 1 || count > 4 || area.Width < count || area.Height < 1) throw new ArgumentOutOfRangeException(nameof(count));
        return Enumerable.Range(0,count).Select(i => {
            int left=(int)((long)area.Width*i/count), right=(int)((long)area.Width*(i+1)/count);
            return new Rect(area.X+left,area.Y,right-left,area.Height);
        }).ToArray();
    }
    public static Rect Clamp(Rect r, Rect area) {
        int w=Math.Clamp(r.Width,1,area.Width), h=Math.Clamp(r.Height,1,area.Height);
        return new(Math.Clamp(r.X,area.X,area.X+area.Width-w),Math.Clamp(r.Y,area.Y,area.Y+area.Height-h),w,h);
    }
}
public static class Workspaces {
    public static Workspace Capture(IEnumerable<DesktopWindow> windows) => new(1,windows.Select(w=>new SavedWindow(w.ProcessPath,w.Title,w.Bounds)).ToArray());
    public static DesktopWindow? Match(SavedWindow saved, IEnumerable<DesktopWindow> current) {
        var matches=current.Where(w=>StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,saved.ProcessPath) && w.Title==saved.Title).ToArray();
        return matches.Length==1 ? matches[0] : null;
    }
    public static void Save(string path, Workspace workspace) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(workspace,new JsonSerializerOptions {WriteIndented=true}));
        File.Move(path+".tmp",path,true);
    }
    public static Workspace Load(string path) {
        var w=JsonSerializer.Deserialize<Workspace>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty workspace");
        if(w.Schema!=1 || w.Windows is null || w.Windows.Length>100 || w.Windows.Any(x=>x is null || string.IsNullOrWhiteSpace(x.ProcessPath) || x.Title is null || x.Bounds is null || x.Bounds.Width<1 || x.Bounds.Height<1)) throw new InvalidDataException("Unsupported or invalid workspace");
        return w;
    }
}