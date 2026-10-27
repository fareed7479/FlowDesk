using System.IO.Pipes;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowDesk.FlowWeb;
if(args.Length is <1 or >2 || !Wire.AllowedOrigin(args[0]) || (args.Length==2 && !Regex.IsMatch(args[1],"^--parent-window=[0-9]+$")))return 2;
using var input=Console.OpenStandardInput();using var output=Console.OpenStandardOutput();
Packet? hello=null;
try {
    using var firstTimeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));hello=await Wire.Read(input,firstTimeout.Token);
    if(hello is null || hello.Type!="hello")throw new InvalidDataException();var browser=Wire.Body<BrowserHello>(hello).Browser;if(browser is not ("Chrome" or "Edge" or "Chromium"))throw new InvalidDataException();
    string settingsPath=Path.Combine(AppContext.BaseDirectory,"flowweb-host-settings.json");
    if(!File.Exists(settingsPath) || new FileInfo(settingsPath).Length>8192)throw new InvalidDataException();
    var settings=JsonSerializer.Deserialize<HostSettings>(File.ReadAllText(settingsPath),Wire.Json)??throw new InvalidDataException();
    if(settings.Schema!=1 || !Path.IsPathFullyQualified(settings.BridgeConfigPath) || !File.Exists(settings.BridgeConfigPath))throw new IOException();
    if(new FileInfo(settings.BridgeConfigPath).Length>8192)throw new InvalidDataException();
    var config=JsonSerializer.Deserialize<BridgeConfig>(File.ReadAllText(settings.BridgeConfigPath),Wire.Json)??throw new InvalidDataException();
    if(config.Schema!=1 || !config.Enabled || !Regex.IsMatch(config.PipeName??"","^[a-zA-Z0-9._-]{1,180}$") || !Regex.IsMatch(config.Token??"","^[A-F0-9]{64}$"))throw new IOException();
    using var pipe=new NamedPipeClientStream(".",config.PipeName!,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
    await pipe.ConnectAsync(3000);await Wire.Write(pipe,Wire.Make("attach",new Attach(Wire.Origin,config.Token!,browser),hello.Id));
    var ready=await Wire.Read(pipe,firstTimeout.Token)??throw new IOException();await Wire.Write(output,ready);if(ready.Type!="ready")return 3;
    using var stop=new CancellationTokenSource();
    async Task FromBrowser(){while(await Wire.Read(input,stop.Token) is { } p){if(p.Type is not ("snapshot" or "result"))throw new InvalidDataException();await Wire.Write(pipe,p,stop.Token);}}
    async Task FromDesktop(){while(await Wire.Read(pipe,stop.Token) is { } p){if(p.Type!="command")throw new InvalidDataException();Wire.Validate(Wire.Body<Command>(p));await Wire.Write(output,p,stop.Token);}}
    var incoming=FromBrowser();var outgoing=FromDesktop();var finished=await Task.WhenAny(incoming,outgoing);bool failed=finished.IsFaulted;stop.Cancel();pipe.Dispose();
    _ = incoming.ContinueWith(t=>_ = t.Exception,TaskContinuationOptions.OnlyOnFaulted);_ = outgoing.ContinueWith(t=>_ = t.Exception,TaskContinuationOptions.OnlyOnFaulted);
    return failed?1:0;
} catch(Exception ex)when(ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or OperationCanceledException or ArgumentException) {
    if(hello is not null)try{await Wire.Write(output,Wire.Make("error",new{code="desktop_unavailable",message="Open FlowDesk, enable its FlowWeb bridge, then connect again."},hello.Id));}catch{}
    Console.Error.WriteLine("FlowWeb connection unavailable.");return 1;
}
record HostSettings(int Schema,string BridgeConfigPath);
