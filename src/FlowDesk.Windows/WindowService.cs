using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FlowDesk.Core;
namespace FlowDesk.Windows;
public sealed partial class WindowService {
    public IReadOnlyList<DesktopWindow> Enumerate() {
        var result=new List<DesktopWindow>();
        Native.EnumProc callback=(h,_)=>{
            try {
                if(!Native.IsWindowVisible(h) || Native.GetWindow(h,4)!=0 || h==Native.GetShellWindow()) return true;
                if((Native.ExtendedStyles(h) & 0x08000080L)!=0) return true;
                if(Native.DwmGetWindowAttribute(h,14,out int cloaked,4)==0 && cloaked!=0) return true;
                var title=new StringBuilder(1024); Native.GetWindowText(h,title,title.Capacity);
                if(title.Length==0 || !Native.GetWindowRect(h,out var r)) return true;
                Native.GetWindowThreadProcessId(h,out uint pid);
                if(pid==Environment.ProcessId) return true;
                using var process=Process.GetProcessById((int)pid);
                string? path=process.MainModule?.FileName;
                if(path is not null && r.Right>r.Left && r.Bottom>r.Top) result.Add(new(h,(int)pid,path,title.ToString(),new(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top)));
            } catch(Win32Exception) {} catch(InvalidOperationException) {} catch(ArgumentException) {}
            return true;
        };
        if(!Native.EnumWindows(callback,0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return result;
    }
    public Rect WorkArea(nint h) {
        var info=new Native.MonitorInfo { Size=Marshal.SizeOf<Native.MonitorInfo>() };
        if(!Native.GetMonitorInfo(Native.MonitorFromWindow(h,2),ref info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        var r=info.Work; return new(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top);
    }
    public void Move(DesktopWindow window, Rect bounds) {
        Native.GetWindowThreadProcessId(window.Handle,out uint pid);
        if(!Native.IsWindow(window.Handle) || pid!=window.ProcessId) throw new InvalidOperationException("Window closed or changed; refresh first.");
        using var process=Process.GetProcessById((int)pid);
        if(!StringComparer.OrdinalIgnoreCase.Equals(process.MainModule?.FileName,window.ProcessPath)) throw new InvalidOperationException("Window identity changed.");
        if(Native.IsIconic(window.Handle) || Native.IsZoomed(window.Handle)) Native.ShowWindow(window.Handle,9);
        if(!Native.SetWindowPos(window.Handle,0,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0014)) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
}
internal static partial class Native {
    internal delegate bool EnumProc(nint h,nint p);
    [StructLayout(LayoutKind.Sequential)] internal struct R { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public int Size; public R Monitor,Work; public uint Flags; }
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool EnumWindows(EnumProc cb,nint p);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint h);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint h);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint h);
    [DllImport("user32.dll")] internal static extern bool IsZoomed(nint h);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(nint h,int cmd);
    [DllImport("user32.dll")] internal static extern nint GetWindow(nint h,uint cmd);
    [DllImport("user32.dll")] internal static extern nint GetShellWindow();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetWindowText(nint h,StringBuilder text,int size);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool GetWindowRect(nint h,out R r);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint h,out uint pid);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool SetWindowPos(nint h,nint after,int x,int y,int w,int height,uint flags);
    [DllImport("user32.dll")] internal static extern nint MonitorFromWindow(nint h,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool GetMonitorInfo(nint h,ref MonitorInfo info);
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(nint h,int attr,out int value,int size);
}
