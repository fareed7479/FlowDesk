using System.Runtime.InteropServices;
namespace FlowDesk.Windows;
// Register on the caller's message-loop thread. Callback remains rooted for the hook lifetime.
public sealed class WindowEvents : IDisposable {
    private readonly Callback callback;
    private nint hook;
    public event Action? Changed;
    public bool Available=>hook!=0;
    public WindowEvents() {
        callback=(_,kind,window,objectId,child,thread,time)=>{if(window!=0 && objectId==0 && child==0 && kind is 0x8000 or 0x8001 or 0x8002 or 0x8003 or 0x800B or 0x800C)Changed?.Invoke();};
        hook=SetWinEventHook(0x8000,0x800C,0,callback,0,0,2);
    }
    public void Dispose(){if(hook!=0){UnhookWinEvent(hook);hook=0;}GC.KeepAlive(callback);}
    private delegate void Callback(nint hook,uint kind,nint window,int objectId,int child,uint thread,uint time);
    [DllImport("user32.dll")]private static extern nint SetWinEventHook(uint min,uint max,nint module,Callback callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")]private static extern bool UnhookWinEvent(nint hook);
}
