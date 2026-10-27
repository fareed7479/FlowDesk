using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using FlowDesk.FlowWeb;
namespace FlowDesk.Windows;
public record BrowserSession(string Id,string Browser,WebTab[] Tabs) {public override string ToString()=>$"{Browser} · {Tabs.Length} web tabs";}
public sealed class FlowWebBridge : IDisposable {
    private sealed class Peer(string id,string browser,NamedPipeServerStream pipe) {
        public string Id=id,Browser=browser;public WebTab[] Tabs=[];public NamedPipeServerStream Pipe=pipe;public SemaphoreSlim Writer=new(1,1);
        public ConcurrentDictionary<string,TaskCompletionSource<CommandResult>> Pending=new();
        public async Task Send(Packet p,CancellationToken ct){await Writer.WaitAsync(ct);try{await Wire.Write(Pipe,p,ct);}finally{Writer.Release();}}
    }
    private readonly ConcurrentDictionary<string,Peer> peers=new();
    private readonly string path,pipeName;
    private CancellationTokenSource? stop;
    private string token="";
    public bool Enabled=>stop is not null;
    public event Action? Changed;
    public BrowserSession[] Sessions=>peers.Values.Select(p=>new BrowserSession(p.Id,p.Browser,p.Tabs.ToArray())).OrderBy(p=>p.Id).ToArray();
    public FlowWebBridge(string path,string? pipeName=null) {this.path=path;this.pipeName=pipeName??("FlowDesk.FlowWeb."+WindowsIdentity.GetCurrent().User!.Value+"."+Process.GetCurrentProcess().SessionId);}
    private void Store(bool enabled) {Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllText(path+".tmp",JsonSerializer.Serialize(new BridgeConfig(1,enabled,pipeName,token),Wire.Json));File.Move(path+".tmp",path,true);}
    public void Enable() {if(Enabled)return;token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));Store(true);stop=new();_ = Listen(stop.Token);Changed?.Invoke();}
    public void Disable() {var old=stop;stop=null;old?.Cancel();foreach(var peer in peers.Values)peer.Pipe.Dispose();peers.Clear();token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));Store(false);Changed?.Invoke();}
    private async Task Listen(CancellationToken ct) {
        try {while(!ct.IsCancellationRequested) {var pipe=new NamedPipeServerStream(pipeName,PipeDirection.InOut,8,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);try{await pipe.WaitForConnectionAsync(ct);_ = Serve(pipe,ct);}catch{pipe.Dispose();throw;}}}
        catch(OperationCanceledException){}catch(IOException){if(!ct.IsCancellationRequested){try{Disable();}catch{}}}
    }
    private bool Authorized(Attach attach)=>Wire.AllowedOrigin(attach.Origin) && attach.Browser is "Chrome" or "Edge" or "Chromium" && attach.Token is not null && attach.Token.Length==64 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(attach.Token),Encoding.UTF8.GetBytes(token));
    private async Task Serve(NamedPipeServerStream pipe,CancellationToken ct) {
        Peer? peer=null;
        try {
            using var authTimeout=CancellationTokenSource.CreateLinkedTokenSource(ct);authTimeout.CancelAfter(TimeSpan.FromSeconds(5));
            var first=await Wire.Read(pipe,authTimeout.Token)??throw new InvalidDataException();if(first.Type!="attach")throw new InvalidDataException();var attach=Wire.Body<Attach>(first);
            if(!Authorized(attach) || peers.Count>=4) {await Wire.Write(pipe,Wire.Make("error",new{code="denied",message="Bridge connection denied."},first.Id),ct);return;}
            peer=new(Guid.NewGuid().ToString("N"),attach.Browser,pipe);peers[peer.Id]=peer;
            await peer.Send(Wire.Make("ready",new{sessionId=peer.Id},first.Id),ct);Changed?.Invoke();
            while(await Wire.Read(pipe,ct) is { } packet) {
                if(packet.Type=="snapshot") {var snapshot=Wire.Body<BrowserSnapshot>(packet);Wire.Validate(snapshot);if(snapshot.Browser!=peer.Browser)throw new InvalidDataException();peer.Tabs=snapshot.Tabs;Changed?.Invoke();}
                else if(packet.Type=="result") {var result=Wire.Body<CommandResult>(packet);if(result.Error?.Length>200)throw new InvalidDataException();if(peer.Pending.TryRemove(packet.Id,out var pending))pending.TrySetResult(result);}
                else throw new InvalidDataException();
            }
        } catch(Exception ex) when(ex is InvalidDataException or IOException or OperationCanceledException or JsonException or ObjectDisposedException or ArgumentException) {}
        finally {pipe.Dispose();if(peer is not null){peers.TryRemove(peer.Id,out _);foreach(var p in peer.Pending.Values)p.TrySetResult(new(false,"Browser disconnected."));Changed?.Invoke();}}
    }
    public async Task<CommandResult> Send(string session,Command command) {
        Wire.Validate(command);if(!Enabled || !peers.TryGetValue(session,out var peer))return new(false,"Browser is disconnected.");
        if(command.TabId.HasValue && !peer.Tabs.Any(t=>t.Id==command.TabId.Value))return new(false,"Tab is not in the shared snapshot.");
        var packet=Wire.Make("command",command);var done=new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);peer.Pending[packet.Id]=done;
        try {using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));await peer.Send(packet,deadline.Token);return await done.Task.WaitAsync(deadline.Token);}
        catch(Exception ex) when(ex is InvalidDataException or IOException or OperationCanceledException or ObjectDisposedException){return new(false,"Browser did not complete the request.");}
        finally{peer.Pending.TryRemove(packet.Id,out _);}
    }
    public void Dispose(){try{Disable();}catch(IOException){}catch(UnauthorizedAccessException){}}
}
