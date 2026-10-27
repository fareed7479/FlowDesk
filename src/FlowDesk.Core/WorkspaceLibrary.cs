using System.Text.Json;
namespace FlowDesk.Core;
public record WorkspaceGroup(Guid Id, string Name, Workspace Workspace) {
    public override string ToString() => $"{Name} · {Workspace.Windows.Length} windows";
}
public record WorkspaceLibrary(int Schema, WorkspaceGroup[] Groups) {
    public static WorkspaceLibrary Empty => new(1,[]);
    public WorkspaceLibrary Upsert(string name, Workspace workspace, Guid? id=null) {
        name=name.Trim();
        var group=new WorkspaceGroup(id ?? Guid.NewGuid(),name,workspace);
        var next=new WorkspaceLibrary(1,Groups.Where(g=>g.Id!=group.Id).Append(group).ToArray());
        LibraryStore.Validate(next); return next;
    }
    public WorkspaceLibrary Remove(Guid id) => new(1,Groups.Where(g=>g.Id!=id).ToArray());
}
public static class LibraryStore {
    public static void Validate(WorkspaceLibrary library) {
        if(library.Schema!=1 || library.Groups is null || library.Groups.Length>32) throw new InvalidDataException("Unsupported workspace library (maximum 32 groups).");
        var ids=new HashSet<Guid>(); var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var group in library.Groups) {
            if(group is null || group.Id==Guid.Empty || !ids.Add(group.Id) || string.IsNullOrWhiteSpace(group.Name) || group.Name.Length>64 || group.Name.Any(char.IsControl) || group.Name!=group.Name.Trim() || !names.Add(group.Name)) throw new InvalidDataException("Workspace names must be unique and contain 1–64 characters.");
            var w=group.Workspace;
            if(w is null || w.Schema!=1 || w.Windows is null || w.Windows.Length is <1 or >100 || w.Windows.Any(x=>x is null || string.IsNullOrWhiteSpace(x.ProcessPath) || x.Title is null || x.Bounds is null || x.Bounds.Width<1 || x.Bounds.Height<1)) throw new InvalidDataException("Invalid workspace content.");
        }
    }
    public static WorkspaceLibrary Load(string path) {
        if(!File.Exists(path)) return WorkspaceLibrary.Empty;
        if(new FileInfo(path).Length>4*1024*1024) throw new InvalidDataException("Workspace library is too large.");
        var library=JsonSerializer.Deserialize<WorkspaceLibrary>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty workspace library.");
        Validate(library); return library;
    }
    public static void Save(string path, WorkspaceLibrary library) {
        Validate(library);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var json=JsonSerializer.Serialize(library,new JsonSerializerOptions { WriteIndented=true });
        if(System.Text.Encoding.UTF8.GetByteCount(json)>4*1024*1024) throw new InvalidDataException("Workspace library is too large.");
        File.WriteAllText(path+".tmp",json);
        File.Move(path+".tmp",path,true);
    }
}
