using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FlowDesk.Core;
namespace FlowDesk.Windows;
// NormalBounds uses WINDOWPLACEMENT workspace coordinates, not screen coordinates.
public record WindowState(uint Flags, uint ShowCommand, int MinX, int MinY, int MaxX, int MaxY, Rect NormalBounds);
public sealed partial class WindowService {
    public DesktopWindow? Resolve(DesktopWindow expected) => Enumerate().SingleOrDefault(w=>w.Handle==expected.Handle && w.ProcessId==expected.ProcessId && StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,expected.ProcessPath) && w.Title==expected.Title);
    private void RequireLive(DesktopWindow window) {
        if(Resolve(window) is null) throw new InvalidOperationException("Window closed or changed; refresh first.");
    }
    public WindowState CaptureState(DesktopWindow window) {
        RequireLive(window);
        var p=new Native.Placement { Length=Marshal.SizeOf<Native.Placement>() };
        if(!Native.GetWindowPlacement(window.Handle,ref p)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new(p.Flags,p.ShowCommand,p.Min.X,p.Min.Y,p.Max.X,p.Max.Y,new(p.Normal.Left,p.Normal.Top,p.Normal.Right-p.Normal.Left,p.Normal.Bottom-p.Normal.Top));
    }
    public void RestoreState(DesktopWindow window, WindowState state) {
        RequireLive(window);
        var r=state.NormalBounds;
        var p=new Native.Placement { Length=Marshal.SizeOf<Native.Placement>(), Flags=state.Flags, ShowCommand=state.ShowCommand, Min=new(){X=state.MinX,Y=state.MinY},Max=new(){X=state.MaxX,Y=state.MaxY},Normal=new(){Left=r.X,Top=r.Y,Right=r.X+r.Width,Bottom=r.Y+r.Height} };
        if(!Native.SetWindowPlacement(window.Handle,ref p)) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public bool IsMinimized(DesktopWindow window) { RequireLive(window); return Native.IsIconic(window.Handle); }
    public void Minimize(DesktopWindow window) {
        RequireLive(window); Native.ShowWindow(window.Handle,6);
        if(!Native.IsIconic(window.Handle)) throw new InvalidOperationException("Application did not minimize.");
    }
    public bool Activate(DesktopWindow window) {
        RequireLive(window);
        if(Native.IsIconic(window.Handle)) Native.ShowWindow(window.Handle,9);
        return Native.SetForegroundWindow(window.Handle);
    }
}
internal static partial class Native {
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern nint GetWindowLong64(nint h,int index);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] private static extern int GetWindowLong32(nint h,int index);
    internal static long ExtendedStyles(nint h) => nint.Size==8 ? GetWindowLong64(h,-20).ToInt64() : GetWindowLong32(h,-20);
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Placement { public int Length; public uint Flags,ShowCommand; public Point Min,Max; public R Normal; }
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool GetWindowPlacement(nint h,ref Placement placement);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool SetWindowPlacement(nint h,ref Placement placement);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint h);
}
