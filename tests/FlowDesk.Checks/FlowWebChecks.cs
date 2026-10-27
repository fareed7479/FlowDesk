using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using FlowDesk.FlowWeb;
using FlowDesk.Windows;
internal static class FlowWebChecks {
    internal static async Task Run(Action<bool,string> check) {
        check(Wire.AllowedOrigin(Wire.Origin) && !Wire.AllowedOrigin("chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/") && !Wire.AllowedOrigin("https://example.test/"),"FlowWeb allows only its pinned extension origin");
        foreach(string url in new[]{"javascript:alert(1)","file:///C:/secret","https://user:pass@example.test/","data:text/html,x"})check(!Wire.WebUrl(url),"FlowWeb rejects restricted URL scheme or credentials");
        bool rejected=false;try{Wire.Validate(new Command("execute"));}catch(InvalidDataException){rejected=true;}check(rejected,"FlowWeb rejects arbitrary native commands");
        rejected=false;try{Wire.Validate(new Command("openWorkspace",Urls:["javascript:alert(1)"]));}catch(InvalidDataException){rejected=true;}check(rejected,"FlowWeb rejects unsafe browser workspace URLs");
        rejected=false;try{Wire.Validate(new Command("mute",1));}catch(InvalidDataException){rejected=true;}check(rejected,"FlowWeb requires an explicit mute value");
        using var memory=new MemoryStream();var packet=Wire.Make("hello",new BrowserHello("Chrome"));await Wire.Write(memory,packet);var raw=memory.ToArray();check(BinaryPrimitives.ReadUInt32LittleEndian(raw)==raw.Length-4,"FlowWeb native framing uses a bounded little-endian byte length");memory.Position=0;var read=await Wire.Read(memory);check(read!.Id==packet.Id && Wire.Body<BrowserHello>(read).Browser=="Chrome","FlowWeb frame roundtrip");
        using var huge=new MemoryStream(new byte[]{255,255,255,127});rejected=false;try{await Wire.Read(huge);}catch(InvalidDataException){rejected=true;}check(rejected,"FlowWeb rejects oversized frames before allocation");
        using var partial=new MemoryStream(new byte[]{5,0,0,0,123});rejected=false;try{await Wire.Read(partial);}catch(EndOfStreamException){rejected=true;}check(rejected,"FlowWeb rejects truncated native messages");
        rejected=false;try{Wire.Validate(packet with{V=2});}catch(InvalidDataException){rejected=true;}check(rejected,"FlowWeb rejects unknown protocol versions");
        var library=WebLibrary.Empty.Add("Research",["https://example.test/path?q=value"]);rejected=false;try{library.Add("research",["https://example.test/"]);}catch(InvalidDataException){rejected=true;}check(rejected,"FlowWeb workspace names are unique without case sensitivity");
        string path=Path.Combine(AppContext.BaseDirectory,"flowweb-test-"+Guid.NewGuid().ToString("N")+".json");
        try{library.Save(path);check(WebLibrary.Load(path).Workspaces[0].Urls[0]==library.Workspaces[0].Urls[0],"FlowWeb browser workspace persistence");var before=File.ReadAllText(path);var large=WebLibrary.Empty;for(int i=0;i<20;i++)large=large.Add("Group "+i,Enumerable.Repeat("https://example.test/?"+new string('&',2000),25));bool tooLarge=false;try{large.Save(path);}catch(InvalidDataException){tooLarge=true;}check(tooLarge && File.ReadAllText(path)==before,"FlowWeb preserves the existing library when serialized data exceeds its limit");}finally{if(File.Exists(path))File.Delete(path);}
        string configPath=Path.Combine(AppContext.BaseDirectory,"flowweb-pipe-"+Guid.NewGuid().ToString("N")+".json");string pipeName="FlowWeb.Checks."+Guid.NewGuid().ToString("N");
        using var bridge=new FlowWebBridge(configPath,pipeName);
        try {
            bridge.Enable();var config=System.Text.Json.JsonSerializer.Deserialize<BridgeConfig>(File.ReadAllText(configPath),Wire.Json)!;
            using(var bad=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly)) {await bad.ConnectAsync(2000);await Wire.Write(bad,Wire.Make("attach",new Attach(Wire.Origin,new string('0',64),"Chrome")));var denied=await Wire.Read(bad);check(denied?.Type=="error" && bridge.Sessions.Length==0,"FlowWeb rejects an unauthenticated local pipe client");}
            using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);await client.ConnectAsync(2000);await Wire.Write(client,Wire.Make("attach",new Attach(Wire.Origin,config.Token,"Chrome")));var ready=await Wire.Read(client);string id=ready!.Data.GetProperty("sessionId").GetString()!;
            await Wire.Write(client,Wire.Make("snapshot",new BrowserSnapshot("Chrome",[new(1,1,"Fixture","https://example.test/",true,false,false,false)])));
            for(int i=0;i<50 && bridge.Sessions.FirstOrDefault()?.Tabs.Length!=1;i++)await Task.Delay(20);
            check(bridge.Sessions.Single().Tabs.Length==1,"FlowWeb accepts an authenticated tab snapshot");
            var pending=bridge.Send(id,new Command("mute",1,true));var command=await Wire.Read(client);check(command?.Type=="command" && Wire.Body<Command>(command).Value==true,"FlowWeb routes a validated desktop command");await Wire.Write(client,Wire.Make("result",new CommandResult(true),command!.Id));check((await pending).Ok,"FlowWeb correlates browser command results");
            using(var edge=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly)) {
                await edge.ConnectAsync(2000);await Wire.Write(edge,Wire.Make("attach",new Attach(Wire.Origin,config.Token,"Edge")));var edgeReady=await Wire.Read(edge);string edgeId=edgeReady!.Data.GetProperty("sessionId").GetString()!;
                await Wire.Write(edge,Wire.Make("snapshot",new BrowserSnapshot("Edge",[new(1,1,"Edge fixture","https://edge.example.test/",true,false,false,false)])));
                for(int i=0;i<50 && bridge.Sessions.Single(s=>s.Id==edgeId).Tabs.Length!=1;i++)await Task.Delay(20);
                check(bridge.Sessions.Length==2 && edgeId!=id,"FlowWeb separates Chrome and Edge sessions with overlapping tab IDs");
                var edgePending=bridge.Send(edgeId,new Command("pin",1,true));var edgeCommand=await Wire.Read(edge);await Wire.Write(edge,Wire.Make("result",new CommandResult(true),edgeCommand!.Id));
                check((await edgePending).Ok && Wire.Body<Command>(edgeCommand).Action=="pin" && bridge.Sessions.Single(s=>s.Id==id).Tabs[0].Url=="https://example.test/","FlowWeb routes commands to the selected browser session");
            }
            for(int i=0;i<50 && bridge.Sessions.Length!=1;i++)await Task.Delay(20);
            check(bridge.Sessions.Single().Id==id,"FlowWeb disconnect clears only the departing browser session");
            bridge.Disable();check(bridge.Sessions.Length==0 && !bridge.Enabled,"FlowWeb disable clears shared browser metadata");var disabled=System.Text.Json.JsonSerializer.Deserialize<BridgeConfig>(File.ReadAllText(configPath),Wire.Json)!;check(!disabled.Enabled && disabled.Token!=config.Token,"FlowWeb disable rotates the local pairing token");
        }finally{bridge.Dispose();if(File.Exists(configPath))File.Delete(configPath);}
    }
}
