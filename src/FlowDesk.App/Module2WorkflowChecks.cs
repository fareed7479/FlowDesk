using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowDesk.App.Hybrid;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;

namespace FlowDesk.App;

internal static class Module2WorkflowChecks
{
    internal static async Task Run()
    {
        var passed = new List<string>(); var fixtures = new List<Process>();
        string root = Program.ProjectRoot(), work = Program.DataDirectory;
        var service = new WindowService(); var stages = new StageManager(service, Path.Combine(work, "stage.json"));
        WorkspaceGroup[] groups = Array.Empty<WorkspaceGroup>(); bool blocked = false;
        string assets = Path.Combine(work, "assets");
        var window = new HybridWorkspaceWindow(assets, work, service, stages, () => groups, () => blocked) { Title = "FlowDesk · Module 2 verification" };
        HybridWebViewHost? host = window.Host;
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException(label); passed.Add(label); }
        async Task Wait(Func<Task<bool>> predicate)
        {
            for (int i = 0; i < 200; i++) { if (await predicate()) return; await Task.Delay(50); }
            throw new TimeoutException("Module 2 did not reach the expected state.");
        }
        async Task<JsonElement[]> Messages()
        {
            string encoded = await host!.WebView.CoreWebView2.ExecuteScriptAsync("JSON.stringify(window.probe.messages)");
            string json = JsonSerializer.Deserialize<string>(encoded) ?? "[]";
            return JsonSerializer.Deserialize<JsonElement[]>(json)!;
        }
        async Task<JsonElement> Send(string type, object payload, string? id = null)
        {
            bool retryBusy = id is null;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                string requestId = id ?? Guid.NewGuid().ToString("N");
                var message = JsonSerializer.Serialize(new { version = 1, id = requestId, type, payload }, IpcJson.Options);
                await host!.WebView.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.postMessage(" + message + ")");
                JsonElement result = default;
                await Wait(async () => { result = (await Messages()).LastOrDefault(m => m.GetProperty("id").GetString() == requestId); return result.ValueKind != JsonValueKind.Undefined; });
                if (retryBusy && result.GetProperty("type").GetString() == "ERROR" && result.GetProperty("payload").GetProperty("code").GetString() == "BUSY") { await Task.Delay(100); continue; }
                return result;
            }
            throw new TimeoutException("Native command remained busy.");
        }
        async Task<JsonElement> State()
        {
            for (int i = 0; i < 50; i++)
            {
                var answer = await Send("GET_DESKTOP_STATE", new { });
                if (answer.GetProperty("type").GetString() == "RESULT") return answer.GetProperty("payload");
                if (answer.GetProperty("payload").GetProperty("code").GetString() != "BUSY") throw new InvalidOperationException(answer.ToString());
                await Task.Delay(100);
            }
            throw new TimeoutException("Inventory remained busy.");
        }
        try
        {
            Directory.CreateDirectory(assets);
            File.WriteAllText(Path.Combine(assets, "index.html"), "<!doctype html><html><head><meta charset='utf-8'><meta http-equiv='Content-Security-Policy' content=\"default-src 'none';script-src 'self';style-src 'self'\"><link rel='stylesheet' href='probe.css'></head><body><h1>FlowDesk Module 2</h1><p>Native WebView2 bridge verification</p><div id='preview'></div><script src='probe.js'></script></body></html>");
            File.WriteAllText(Path.Combine(assets, "probe.css"), "body{margin:0;background:#182133;color:#f3f6fd;font:16px 'Segoe UI';padding:24px}#preview{width:220px;height:120px;margin-top:24px;border:1px solid #79a9ff}");
            File.WriteAllText(Path.Combine(assets, "probe.js"), "window.probe={messages:[]};window.chrome.webview.addEventListener('message',event=>window.probe.messages.push(event.data));");
            window.AppWindow.Resize(new(980, 760)); window.Activate();
            await Wait(() => Task.FromResult(host.IsLoaded && host.ActualWidth > 100));
            await window.Initialization;
            await Wait(async () => host.Bridge?.IsReady == true && (await Messages()).Any(m => m.GetProperty("type").GetString() == "BRIDGE_READY"));
            Check(true, "local WebView2 initializes and receives a native BRIDGE_READY message");
            Check(!host.Bridge!.IsTrustedDocument("https://flowdesk.local.evil/index.html") && !host.Bridge.IsTrustedDocument("https://flowdesk.local:444/index.html") && !host.Bridge.IsTrustedDocument("file:///index.html"), "bridge rejects lookalike, nondefault-port and file origins");
            string prefix = "FlowDesk module2 " + Guid.NewGuid().ToString("N");
            string fixture = Path.Combine(root, "tests/FlowDesk.Checks/bin/Debug/net10.0-windows/FlowDesk.Checks.exe");
            for (int i = 0; i < 2; i++)
            {
                var start = new ProcessStartInfo(fixture) { UseShellExecute = false };
                start.Environment["DOTNET_ROOT_X64"] = Path.Combine(root, ".tools/dotnet");
                start.ArgumentList.Add("--fixture"); start.ArgumentList.Add(prefix + i); fixtures.Add(Process.Start(start)!);
            }
            await Wait(() => Task.FromResult(service.Enumerate().Count(w => w.Title.StartsWith(prefix, StringComparison.Ordinal)) == 2));
            var native = service.Enumerate().Where(w => w.Title.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(w => w.Title).ToArray();
            groups = new[] { new WorkspaceGroup(Guid.NewGuid(), "Module2 scene", Workspaces.Capture(new[] { native[0] })) };
            var state = await State();
            var dto = state.GetProperty("windows").EnumerateArray().Single(w => w.GetProperty("title").GetString() == native[0].Title);
            string windowId = dto.GetProperty("id").GetString()!;
            Check(Guid.TryParseExact(windowId, "N", out _) && !dto.TryGetProperty("handle", out _), "desktop state exposes opaque IDs instead of native handles");
            await Wait(async () => (await Messages()).Any(m => m.GetProperty("type").GetString() == "WINDOW_CREATED" && m.GetProperty("payload").GetProperty("id").GetString() == windowId));
            Check(true, "native window creation reaches JavaScript as WINDOW_CREATED");
            var rectJson = await host.WebView.CoreWebView2.ExecuteScriptAsync("(()=>{const r=document.querySelector('#preview').getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height,dpr:devicePixelRatio};})()");
            using var rectDoc = JsonDocument.Parse(rectJson); var css = rectDoc.RootElement;
            var bounds = new CssBounds(css.GetProperty("x").GetDouble(), css.GetProperty("y").GetDouble(), css.GetProperty("width").GetDouble(), css.GetProperty("height").GetDouble());
            var vp = state.GetProperty("viewport"); long epoch = vp.GetProperty("version").GetInt64();
            Check(Math.Abs(css.GetProperty("dpr").GetDouble() - vp.GetProperty("scale").GetDouble()) < .001, "WebView devicePixelRatio matches native rasterization scale at fixed zoom");
            var thumbnail = await Send("UPDATE_THUMBNAIL_BOUNDS", new { windowId, viewportVersion = epoch, visible = true, bounds });
            Check(thumbnail.GetProperty("type").GetString() == "RESULT" && host.Bridge.RegisteredThumbnails == 1, "JavaScript container bounds register a real native DWM relationship");
            var physical = thumbnail.GetProperty("payload").GetProperty("bounds");
            Check(physical.GetProperty("x").GetInt32() == vp.GetProperty("origin").GetProperty("x").GetInt32() + (int)Math.Floor(bounds.X * css.GetProperty("dpr").GetDouble()), "DWM destination includes the native WebView client offset and DPI scale");
            await Send("UPDATE_THUMBNAIL_BOUNDS", new { windowId, viewportVersion = epoch, visible = false });
            Check(host.Bridge.RegisteredThumbnails == 0, "hidden containers release their DWM relationship");
            var invalid = await Send("MINIMIZE_WINDOW", new { windowId, unexpected = true });
            Check(invalid.GetProperty("payload").GetProperty("code").GetString() == "INVALID_REQUEST" && !service.IsMinimized(native[0]), "unknown payload fields are rejected before native mutation");
            blocked = true; var forbidden = await Send("MINIMIZE_WINDOW", new { windowId }); blocked = false;
            Check(forbidden.GetProperty("payload").GetProperty("code").GetString() == "FORBIDDEN" && !service.IsMinimized(native[0]), "host focus/fullscreen guard blocks mutations");
            string replay = Guid.NewGuid().ToString("N"); var minimize = await Send("MINIMIZE_WINDOW", new { windowId }, replay);
            Check(minimize.GetProperty("type").GetString() == "RESULT" && service.IsMinimized(native[0]), "web-to-native minimize changes the fixture's real show state");
            await Send("MINIMIZE_WINDOW", new { windowId }, replay);
            await Wait(async () => (await Messages()).Any(m => m.GetProperty("id").GetString() == replay && m.GetProperty("type").GetString() == "ERROR" && m.GetProperty("payload").GetProperty("code").GetString() == "DUPLICATE_ID"));
            Check(true, "duplicate request IDs cannot repeat a native command");
            var activate = await Send("ACTIVATE_WINDOW", new { windowId });
            Check(activate.GetProperty("type").GetString() == "RESULT" && !service.IsMinimized(native[0]), "web-to-native activation restores a minimized fixture");
            if (activate.GetProperty("payload").GetProperty("accepted").GetBoolean())
            {
                await Wait(async () => (await Messages()).Any(m => m.GetProperty("type").GetString() == "WINDOW_ACTIVATED" && m.GetProperty("payload").GetProperty("windowId").GetString() == windowId));
                Check((await Messages()).Any(m => m.GetProperty("type").GetString() == "FOCUS_CHANGED" && m.GetProperty("payload").GetProperty("windowId").GetString() == windowId), "native foreground change reaches JavaScript as activation and focus events");
            }
            var original = service.CaptureState(native[0]);
            var sceneAnswer = await Send("ACTIVATE_SCENE", new { sceneId = groups[0].Id.ToString("N") });
            Check(sceneAnswer.GetProperty("type").GetString() == "RESULT" && stages.IsActive && File.Exists(Path.Combine(work, "stage.json")), "scene activation uses the actual journaled stage service");
            await Send("LEAVE_SCENE", new { });
            Check(!stages.IsActive && service.CaptureState(native[0]).NormalBounds == original.NormalBounds, "scene exit restores the native fixture layout");
            window.AppWindow.Resize(new(1040, 800)); await Task.Delay(300); state = await State();
            Check(state.GetProperty("viewport").GetProperty("version").GetInt64() > epoch, "native resize invalidates the frontend viewport epoch");
            var stale = await Send("UPDATE_THUMBNAIL_BOUNDS", new { windowId, viewportVersion = epoch, visible = true, bounds });
            Check(stale.GetProperty("type").GetString() == "ERROR" && host.Bridge.RegisteredThumbnails == 0, "stale thumbnail geometry cannot be applied after resize");
            var oversized = await Send("UPDATE_THUMBNAIL_BOUNDS", new { windowId, viewportVersion = state.GetProperty("viewport").GetProperty("version").GetInt64(), visible = true, bounds = new { x = 0, y = 0, width = 100000, height = 100000 } });
            Check(oversized.GetProperty("type").GetString() == "ERROR" && host.Bridge.RegisteredThumbnails == 0, "off-viewport thumbnail rectangles are rejected");
            host.WebView.CoreWebView2.Navigate("https://example.invalid/blocked"); await Task.Delay(200);
            Check(host.WebView.CoreWebView2.Source.StartsWith("https://flowdesk.local/index.html", StringComparison.Ordinal), "external top-level navigation is canceled before leaving the trusted document");
            fixtures[0].CloseMainWindow(); await fixtures[0].WaitForExitAsync();
            await Wait(async () => (await Messages()).Any(m => m.GetProperty("type").GetString() == "WINDOW_CLOSED" && m.GetProperty("payload").GetProperty("windowId").GetString() == windowId));
            Check(true, "native window destruction reaches JavaScript and removes its identity");
            var missing = await Send("ACTIVATE_WINDOW", new { windowId });
            Check(missing.GetProperty("payload").GetProperty("code").GetString() == "NOT_FOUND", "closed-window IDs cannot activate another native handle");
            groups = new[] { new WorkspaceGroup(Guid.NewGuid(), "Close recovery scene", Workspaces.Capture(new[] { native[1] })) };
            await Send("ACTIVATE_SCENE", new { sceneId = groups[0].Id.ToString("N") });
            window.Close();
            Check(!stages.IsActive && !service.IsMinimized(native[1]), "initialized hybrid window restores its active scene before closing");
            File.WriteAllText(Path.Combine(root, ".cache/module2-results.json"), JsonSerializer.Serialize(new { Success = true, Passed = passed }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(root, ".cache/module2-results.json"), JsonSerializer.Serialize(new { Success = false, Passed = passed, Error = ex.ToString(), HostError = host?.LastError, BridgeError = host?.Bridge?.LastError }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.ExitCode = 1;
        }
        finally
        {
            stages.Leave(); host?.Dispose();
            foreach (var fixture in fixtures) { if (!fixture.HasExited) { fixture.CloseMainWindow(); if (!fixture.WaitForExit(3000)) fixture.Kill(); } fixture.Dispose(); }
            window.Close();
        }
    }
}
