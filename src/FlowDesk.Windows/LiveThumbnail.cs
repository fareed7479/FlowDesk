using System.Runtime.InteropServices;
using FlowDesk.Core;
namespace FlowDesk.Windows;
// Own exactly one documented DWM thumbnail relationship; never capture or persist pixels.
public sealed class LiveThumbnail : IDisposable {
    private nint handle;
    public bool Registered=>handle!=0;
    public LiveThumbnail(nint destination,nint source) {Marshal.ThrowExceptionForHR(DwmRegisterThumbnail(destination,source,out handle));}
    public void Update(Rect bounds,bool visible) {
        if(handle==0)return;
        var p=new Properties {Flags=1|4|8|16,Destination=new(){Left=bounds.X,Top=bounds.Y,Right=bounds.X+bounds.Width,Bottom=bounds.Y+bounds.Height},Opacity=255,Visible=visible,ClientOnly=false};
        Marshal.ThrowExceptionForHR(DwmUpdateThumbnailProperties(handle,ref p));
    }
    public void Dispose(){if(handle!=0){DwmUnregisterThumbnail(handle);handle=0;}GC.SuppressFinalize(this);}
    [StructLayout(LayoutKind.Sequential)] private struct Properties {public uint Flags;public Native.R Destination,Source;public byte Opacity;[MarshalAs(UnmanagedType.Bool)]public bool Visible;[MarshalAs(UnmanagedType.Bool)]public bool ClientOnly;}
    [DllImport("dwmapi.dll")]private static extern int DwmRegisterThumbnail(nint destination,nint source,out nint thumbnail);
    [DllImport("dwmapi.dll")]private static extern int DwmUpdateThumbnailProperties(nint thumbnail,ref Properties properties);
    [DllImport("dwmapi.dll")]private static extern int DwmUnregisterThumbnail(nint thumbnail);
}
