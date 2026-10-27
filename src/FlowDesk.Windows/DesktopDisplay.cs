using System.ComponentModel;
using System.Runtime.InteropServices;
using FlowDesk.Core;
namespace FlowDesk.Windows;
public record DesktopDisplay(string Device,Rect Bounds,Rect WorkArea,bool Primary) {
    public override string ToString()=>$"{Device.Replace(@"\\.\","")} · {Bounds.Width} × {Bounds.Height}"+(Primary?" · Primary":"");
}
public sealed partial class WindowService {
    public IReadOnlyList<DesktopDisplay> Displays() {
        var displays=new List<DesktopDisplay>();
        Native.MonitorProc callback=(nint h,nint dc,ref Native.R rect,nint state)=>{
            var info=new Native.MonitorInfoEx {Size=Marshal.SizeOf<Native.MonitorInfoEx>(),Device=""};
            if(Native.GetMonitorInfoEx(h,ref info)) {var b=info.Monitor;var w=info.Work;displays.Add(new(info.Device,new(b.Left,b.Top,b.Right-b.Left,b.Bottom-b.Top),new(w.Left,w.Top,w.Right-w.Left,w.Bottom-w.Top),(info.Flags&1)!=0));}
            return true;
        };
        if(!Native.EnumDisplayMonitors(0,0,callback,0))throw new Win32Exception(Marshal.GetLastWin32Error());
        return displays.OrderByDescending(d=>d.Primary).ThenBy(d=>d.Bounds.X).ToArray();
    }
    public DesktopDisplay? DisplayFor(DesktopWindow window)=>Displays().OrderByDescending(d=>Intersection(d.Bounds,window.Bounds)).FirstOrDefault();
    private static long Intersection(Rect a,Rect b)=>(long)Math.Max(0,Math.Min(a.X+a.Width,b.X+b.Width)-Math.Max(a.X,b.X))*Math.Max(0,Math.Min(a.Y+a.Height,b.Y+b.Height)-Math.Max(a.Y,b.Y));
    public uint Dpi(DesktopWindow window)=>Native.GetDpiForWindow(window.Handle);
    public bool IsResponsive(DesktopWindow window)=>!Native.IsHungAppWindow(window.Handle);
}
internal static partial class Native {
    internal delegate bool MonitorProc(nint monitor,nint dc,ref R bounds,nint state);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct MonitorInfoEx {public int Size;public R Monitor,Work;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
    [DllImport("user32.dll",EntryPoint="GetMonitorInfoW",CharSet=CharSet.Unicode)]internal static extern bool GetMonitorInfoEx(nint monitor,ref MonitorInfoEx info);
    [DllImport("user32.dll",SetLastError=true)]internal static extern bool EnumDisplayMonitors(nint dc,nint clip,MonitorProc callback,nint data);
    [DllImport("user32.dll")]internal static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")]internal static extern bool IsHungAppWindow(nint window);
}
