using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FlowDesk.Core;
namespace FlowDesk.Windows;
public record ManagedIdentity(long Handle,int ProcessId,string ProcessPath,long ProcessStarted,string Marker);
public record ManagedSnapshot(ManagedIdentity Identity,WindowState Placement,long Style,Rect Bounds);
public sealed partial class WindowService {
    public ManagedSnapshot Track(DesktopWindow window) {
        RequireLive(window);
        using var process=Process.GetProcessById(window.ProcessId);
        var id=new ManagedIdentity(window.Handle.ToInt64(),window.ProcessId,window.ProcessPath,process.StartTime.ToUniversalTime().Ticks,"FlowDesk.Mode."+Guid.NewGuid().ToString("N"));
        var placement=CaptureState(window); long style=Native.ReadStyle(window.Handle);
        if(!Native.SetProp(window.Handle,id.Marker,1)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new(id,placement,style,window.Bounds);
    }
    // A per-window property disappears with the HWND, so recycled handles cannot receive recovery changes.
    public DesktopWindow? ResolveManaged(ManagedIdentity id) {
        nint h=(nint)id.Handle;
        if(!Native.IsWindow(h) || Native.GetProp(h,id.Marker)!=1) return null;
        Native.GetWindowThreadProcessId(h,out uint pid);if(pid!=id.ProcessId) return null;
        try {
            using var p=Process.GetProcessById(id.ProcessId);
            if(p.StartTime.ToUniversalTime().Ticks!=id.ProcessStarted || !StringComparer.OrdinalIgnoreCase.Equals(p.MainModule?.FileName,id.ProcessPath)) return null;
            if(!Native.GetWindowRect(h,out var r)) return null;
            var title=new StringBuilder(1024);Native.GetWindowText(h,title,title.Capacity);
            return new(h,id.ProcessId,id.ProcessPath,title.ToString(),new(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top));
        } catch(ArgumentException){return null;} catch(InvalidOperationException){return null;}
    }
    public void Forget(ManagedIdentity id) {if(ResolveManaged(id) is not null) Native.RemoveProp((nint)id.Handle,id.Marker);}
    public long Style(DesktopWindow window) {RequireLive(window);return Native.ReadStyle(window.Handle);}
    public Rect MonitorBounds(nint window) {
        var info=new Native.MonitorInfo {Size=Marshal.SizeOf<Native.MonitorInfo>()};
        if(!Native.GetMonitorInfo(Native.MonitorFromWindow(window,2),ref info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        var r=info.Monitor;return new(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top);
    }
    public void MakeFullscreen(ManagedSnapshot snapshot) {
        var live=ResolveManaged(snapshot.Identity) ?? throw new InvalidOperationException("Fullscreen target is unavailable.");
        if(Native.IsIconic(live.Handle) || Native.IsZoomed(live.Handle)) Native.ShowWindow(live.Handle,9);
        Native.WriteStyle(live.Handle,Native.ReadStyle(live.Handle) & ~0x00CF0000L);
        var r=MonitorBounds(live.Handle);
        if(!Native.SetWindowPos(live.Handle,0,r.X,r.Y,r.Width,r.Height,0x0030)) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public bool RestoreManaged(ManagedSnapshot snapshot) {
        var live=ResolveManaged(snapshot.Identity);if(live is null) return false;
        long currentStyle=Native.ReadStyle(live.Handle);
        long restoredStyle=(currentStyle & ~0x00CF0000L) | (snapshot.Style & 0x00CF0000L);
        if(currentStyle!=restoredStyle) {
            Native.WriteStyle(live.Handle,restoredStyle);
            if(!Native.SetWindowPos(live.Handle,0,0,0,0,0,0x0037)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        // Use the live title; a document title may have changed during a focus session.
        RestoreState(live,snapshot.Placement);Forget(snapshot.Identity);return true;
    }
}
internal static partial class Native {
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern nint ReadStyle64(nint h,int index);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] private static extern int ReadStyle32(nint h,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] private static extern nint WriteStyle64(nint h,int index,nint value);
    [DllImport("user32.dll",EntryPoint="SetWindowLongW",SetLastError=true)] private static extern int WriteStyle32(nint h,int index,int value);
    internal static long ReadStyle(nint h) => nint.Size==8?ReadStyle64(h,-16).ToInt64():ReadStyle32(h,-16);
    internal static void WriteStyle(nint h,long value) {
        Marshal.SetLastPInvokeError(0);
        nint previous=nint.Size==8?WriteStyle64(h,-16,(nint)value):(nint)WriteStyle32(h,-16,(int)value);
        if(previous==0 && Marshal.GetLastWin32Error()!=0) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    [DllImport("user32.dll",EntryPoint="SetPropW",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool SetProp(nint h,string name,nint value);
    [DllImport("user32.dll",EntryPoint="GetPropW",CharSet=CharSet.Unicode)] internal static extern nint GetProp(nint h,string name);
    [DllImport("user32.dll",EntryPoint="RemovePropW",CharSet=CharSet.Unicode)] internal static extern nint RemoveProp(nint h,string name);
}
