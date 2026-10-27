using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace FlowDesk.App;
internal sealed class RecoveryHotkey : NativeWindow,IDisposable {
    private readonly Action action;
    public bool Available {get;}
    public RecoveryHotkey(Action action) {this.action=action;CreateHandle(new CreateParams());Available=RegisterHotKey(Handle,0x4644,0x4003,0x79);}
    protected override void WndProc(ref Message message) {if(message.Msg==0x0312 && message.WParam==(nint)0x4644)action();base.WndProc(ref message);}
    public void Dispose() {if(Available)UnregisterHotKey(Handle,0x4644);DestroyHandle();}
    [DllImport("user32.dll",SetLastError=true)] private static extern bool RegisterHotKey(nint window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(nint window,int id);
}
