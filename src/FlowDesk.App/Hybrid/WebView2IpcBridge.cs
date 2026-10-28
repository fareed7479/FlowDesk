using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml;

namespace FlowDesk.App.Hybrid;

/// <summary>UI-thread-owned bridge for an app-owned local document. No native handles
/// or arbitrary command execution are accepted from JavaScript.</summary>
public sealed class WebView2IpcBridge : IDisposable
{
    private sealed record Entry(ManagedIdentity Identity, FlowDesk.Core.DesktopWindow Window);
    private readonly CoreWebView2 core;
    private readonly nint destination;
    private readonly Uri document;
    private readonly WindowService service;
    private readonly StageManager stages;
    private readonly Func<IReadOnlyList<WorkspaceGroup>> scenes;
    private readonly Func<bool> mutationsBlocked;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly WindowEvents events = new();
    private readonly Dictionary<string, Entry> windows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LiveThumbnail> thumbnails = new(StringComparer.Ordinal);
    private readonly HashSet<string> requests = new(StringComparer.Ordinal);
    private ViewportState viewport;
    private bool disposed, ready, refreshing, dirty = true;
    private string snapshot = "";
    private nint foreground;
    private DateTimeOffset rateStart = DateTimeOffset.UtcNow;
    private int rateCount;
    public bool IsBusy { get; private set; }
    public bool IsReady => ready && !disposed;
    public int RegisteredThumbnails => thumbnails.Count;
    public string? LastError { get; private set; }

    public WebView2IpcBridge(CoreWebView2 core, nint destination, Uri document, ViewportState viewport,
        WindowService service, StageManager stages, Func<IReadOnlyList<WorkspaceGroup>> scenes, Func<bool> mutationsBlocked)
    {
        this.core = core ?? throw new ArgumentNullException(nameof(core));
        if (document.Scheme != "https" || document.Host != "flowdesk.local" || document.AbsolutePath != "/index.html" || !document.IsDefaultPort || document.Query.Length != 0)
            throw new ArgumentException("Use the app-owned https://flowdesk.local/index.html document.", nameof(document));
        DpiBoundsHelper.Inspect(destination);
        this.destination = destination; this.document = document; this.viewport = viewport;
        this.service = service; this.stages = stages; this.scenes = scenes; this.mutationsBlocked = mutationsBlocked;
        core.WebMessageReceived += Received;
        core.NavigationStarting += Navigating;
        core.NavigationCompleted += Navigated;
        core.FrameNavigationStarting += FrameNavigating;
        core.ProcessFailed += ProcessFailed;
        core.NewWindowRequested += NewWindow;
        core.PermissionRequested += Permission;
        core.DownloadStarting += Download;
        events.Changed += Changed;
        timer.Tick += Tick;
        timer.Start();
    }

    public bool IsTrustedDocument(string source) => Uri.TryCreate(source, UriKind.Absolute, out var uri)
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0
        && Uri.Compare(uri, document, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped, StringComparison.Ordinal) == 0;

    public void UpdateViewport(ViewportState next)
    {
        if (disposed) return;
        if (next.Version <= viewport.Version) throw new ArgumentException("Viewport versions must increase.", nameof(next));
        viewport = next; ClearThumbnails(); Post(null, "DPI_CHANGED", viewport);
    }

    private void FrameNavigating(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e) => e.Cancel = true;
    private void ProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs e) { ready = false; ClearThumbnails(); LastError = "WebView2 process failed: " + e.ProcessFailedKind; }
    private void NewWindow(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs e) => e.Handled = true;
    private void Permission(CoreWebView2 sender, CoreWebView2PermissionRequestedEventArgs e) => e.State = CoreWebView2PermissionState.Deny;
    private void Download(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs e) => e.Cancel = true;
    private void Changed() => dirty = true;
    private void Navigating(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!IsTrustedDocument(e.Uri) || IsBusy || refreshing) { e.Cancel = true; return; }
        ready = false; requests.Clear(); rateCount = 0; rateStart = DateTimeOffset.UtcNow;
        ClearThumbnails(); ReleaseWindows(); snapshot = ""; dirty = true;
    }
    private async void Navigated(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        try
        {
            if (disposed || !e.IsSuccess || !IsTrustedDocument(core.Source)) return;
            ready = true; await RefreshAsync(); Post(null, "BRIDGE_READY", State());
        }
        catch (Exception ex) { LastError = ex.Message; ready = false; }
    }
    private async void Tick(object? sender, object e)
    {
        try
        {
            if (!IsReady || IsBusy || refreshing) return;
            nint current = GetForegroundWindow();
            if (current != foreground)
            {
                foreground = current;
                string? id = windows.FirstOrDefault(w => w.Value.Window.Handle == current).Key;
                Post(null, "WINDOW_ACTIVATED", new { windowId = id });
                Post(null, "FOCUS_CHANGED", new { windowId = id });
            }
            if (dirty || !events.Available) await RefreshAsync();
        }
        catch (Exception ex) { LastError = ex.Message; }
    }

    private async Task RefreshAsync()
    {
        if (refreshing || disposed) return;
        refreshing = true; dirty = false;
        try
        {
            var inventory = await Task.Run(service.Enumerate);
            if (disposed || !ready) return;
            var oldIds = windows.Keys.ToHashSet(StringComparer.Ordinal);
            var present = new HashSet<string>(StringComparer.Ordinal);
            foreach (var live in inventory.Take(128))
            {
                try
                {
                    var previous = windows.FirstOrDefault(w => w.Value.Window.Handle == live.Handle);
                    string id;
                    if (previous.Key is not null && service.ResolveManaged(previous.Value.Identity) is not null)
                    {
                        id = previous.Key; windows[id] = previous.Value with { Window = live };
                    }
                    else
                    {
                        var tracked = service.Track(live);
                        id = Guid.NewGuid().ToString("N"); windows.Add(id, new(tracked.Identity, live));
                    }
                    present.Add(id);
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException) { LastError = ex.Message; }
            }
            foreach (string id in windows.Keys.Where(id => !present.Contains(id)).ToArray())
            {
                RemoveThumbnail(id); service.Forget(windows[id].Identity); windows.Remove(id);
                Post(null, "WINDOW_CLOSED", new { windowId = id });
            }
            foreach (string id in present.Except(oldIds)) Post(null, "WINDOW_CREATED", WindowDto(id, windows[id]));
            var state = State(); string serialized = JsonSerializer.Serialize(state, IpcJson.Options);
            if (serialized != snapshot) { snapshot = serialized; Post(null, "WINDOWS_CHANGED", state); }
        }
        finally { refreshing = false; if (disposed && !IsBusy) ReleaseWindows(); }
    }

    private DesktopWindow WindowDto(string id, Entry entry)
    {
        var live = service.ResolveManaged(entry.Identity) ?? throw new InvalidOperationException("Window is no longer available.");
        return new(id, live.ProcessId, live.ProcessPath, live.Title, live.Bounds, service.IsMinimized(live));
    }
    private DesktopState State()
    {
        var current = new List<DesktopWindow>();
        foreach (var pair in windows)
            try { current.Add(WindowDto(pair.Key, pair.Value)); }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException) { dirty = true; }
        var inventory = windows.Values.Select(entry => entry.Window).ToArray();
        var groups = scenes().Select(g =>
        {
            var matched = stages.Preview(g, null, inventory).Moves.Select(m => windows.FirstOrDefault(w => w.Value.Window.Handle == m.Window.Handle).Key)
                .Where(id => id is not null && current.Any(w => w.Id == id)).Cast<string>().Distinct().ToArray();
            return new StageScene(g.Id.ToString("N"), g.Name, matched, g.Workspace.Windows.Length - matched.Length, stages.ActiveGroupId == g.Id);
        }).ToArray();
        var dock = current.GroupBy(w => w.ProcessPath, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DockItem(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(g.Key.ToUpperInvariant()))).ToLowerInvariant(), System.IO.Path.GetFileNameWithoutExtension(g.Key), g.Select(w => w.Id).ToArray(), true)).ToArray();
        return new(current.ToArray(), groups, dock, viewport);
    }
    private FlowDesk.Core.DesktopWindow Resolve(string id)
    {
        if (id is null || !windows.TryGetValue(id, out var entry)) throw new KeyNotFoundException("Unknown window ID. Refresh desktop state.");
        return service.ResolveManaged(entry.Identity) ?? throw new KeyNotFoundException("Window closed or its identity changed.");
    }

    private async void Received(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string? id = null;
        try
        {
            if (disposed || !IsTrustedDocument(e.Source) || !IsTrustedDocument(core.Source)) return;
            if (!ready) { Error(null, "NOT_READY", "Wait for BRIDGE_READY."); return; }
            var request = IpcJson.Parse(e.WebMessageAsJson); id = request.Id;
            if (DateTimeOffset.UtcNow - rateStart >= TimeSpan.FromSeconds(1)) { rateStart = DateTimeOffset.UtcNow; rateCount = 0; }
            if (++rateCount > 60) { Error(id, "RATE_LIMIT", "At most 60 requests per second."); return; }
            bool mutating = request.Type is "ACTIVATE_WINDOW" or "MINIMIZE_WINDOW" or "ACTIVATE_SCENE" or "LEAVE_SCENE";
            if (mutating && requests.Contains(id)) { Error(id, "DUPLICATE_ID", "Use a new request ID."); return; }
            if (mutating && requests.Count >= 4096) { Error(id, "REQUEST_LIMIT", "Reload the app document to start a new request session."); return; }
            if (mutating) requests.Add(id);
            if (IsBusy || refreshing) { Error(id, "BUSY", "Retry after the current operation with a new request ID."); return; }
            IsBusy = true;
            try
            {
                object result;
                switch (request.Type)
                {
                    case "GET_DESKTOP_STATE":
                        IpcJson.Payload<EmptyCommand>(request); await RefreshAsync(); result = State(); break;
                    case "ACTIVATE_WINDOW":
                        var activate = IpcJson.Payload<WindowCommand>(request);
                        RequireMutation(); result = new { accepted = service.Activate(Resolve(activate.WindowId)) }; dirty = true; break;
                    case "MINIMIZE_WINDOW":
                        var minimize = IpcJson.Payload<WindowCommand>(request); RequireMutation();
                        var target = Resolve(minimize.WindowId); service.Minimize(target);
                        result = new { minimized = service.IsMinimized(target) }; dirty = true; break;
                    case "ACTIVATE_SCENE":
                        var activateScene = IpcJson.Payload<SceneCommand>(request); RequireMutation();
                        if (!Guid.TryParseExact(activateScene.SceneId, "N", out var sceneId)) throw new JsonException("Invalid scene ID.");
                        var scene = scenes().SingleOrDefault(s => s.Id == sceneId) ?? throw new KeyNotFoundException("Unknown saved scene.");
                        result = await Task.Run(() => stages.Activate(scene)); dirty = true; break;
                    case "LEAVE_SCENE":
                        IpcJson.Payload<EmptyCommand>(request);
                        result = await Task.Run(stages.Leave); dirty = true; break;
                    case "UPDATE_THUMBNAIL_BOUNDS":
                        result = Thumbnail(IpcJson.Payload<ThumbnailCommand>(request)); break;
                    default: throw new JsonException("Unsupported command type.");
                }
                if (!disposed) Post(id, "RESULT", result);
            }
            finally { IsBusy = false; if (disposed && !refreshing) ReleaseWindows(); }
        }
        catch (JsonException ex) { Error(id, "INVALID_REQUEST", ex.Message); }
        catch (KeyNotFoundException ex) { Error(id, "NOT_FOUND", ex.Message); }
        catch (UnauthorizedAccessException ex) { Error(id, "FORBIDDEN", ex.Message); }
        catch (Exception ex) when (ex is ArgumentException or OverflowException) { Error(id, "INVALID_BOUNDS", ex.Message); }
        catch (Exception ex) { LastError = ex.Message; Error(id, "NATIVE_FAILURE", ex.Message); }
    }
    private void RequireMutation()
    {
        if (mutationsBlocked()) throw new UnauthorizedAccessException("End the current focus/fullscreen operation first.");
    }
    private object Thumbnail(ThumbnailCommand command)
    {
        Resolve(command.WindowId);
        if (!command.Visible) { RemoveThumbnail(command.WindowId); return new { registered = false }; }
        if (command.ViewportVersion != viewport.Version) throw new ArgumentException("Stale viewport. Use the latest DPI_CHANGED state.");
        if (command.Bounds is null) throw new JsonException("Visible thumbnails require bounds.");
        var bounds = DpiBoundsHelper.CssToClientPixels(command.Bounds, viewport.Scale, viewport.Origin, viewport.Bounds);
        if (!thumbnails.TryGetValue(command.WindowId, out var thumbnail))
        {
            if (thumbnails.Count >= 32) throw new InvalidOperationException("At most 32 visible thumbnails are supported.");
            thumbnail = new LiveThumbnail(destination, Resolve(command.WindowId).Handle); thumbnails.Add(command.WindowId, thumbnail);
        }
        try { thumbnail.Update(bounds, true); }
        catch { RemoveThumbnail(command.WindowId); throw; }
        return new { registered = true, bounds, viewportVersion = viewport.Version };
    }
    private void Error(string? id, string code, string message) => Post(id, "ERROR", new IpcError(code, message));
    private void Post(string? id, string type, object payload)
    {
        if (disposed || (!ready && type != "ERROR")) return;
        try { if (IsTrustedDocument(core.Source)) core.PostWebMessageAsJson(JsonSerializer.Serialize(new IpcMessage(1, id, type, payload), IpcJson.Options)); }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or ObjectDisposedException) { LastError = ex.Message; }
    }
    private void RemoveThumbnail(string id) { if (thumbnails.Remove(id, out var thumbnail)) thumbnail.Dispose(); }
    private void ClearThumbnails() { foreach (var thumbnail in thumbnails.Values) thumbnail.Dispose(); thumbnails.Clear(); }
    private void ReleaseWindows()
    {
        foreach (var entry in windows.Values)
            try { service.Forget(entry.Identity); } catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException) { LastError = ex.Message; }
        windows.Clear();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; ready = false; timer.Stop(); events.Changed -= Changed; events.Dispose();
        core.WebMessageReceived -= Received; core.NavigationStarting -= Navigating; core.NavigationCompleted -= Navigated;
        core.FrameNavigationStarting -= FrameNavigating; core.ProcessFailed -= ProcessFailed;
        core.NewWindowRequested -= NewWindow; core.PermissionRequested -= Permission; core.DownloadStarting -= Download;
        ClearThumbnails(); if (!IsBusy && !refreshing) ReleaseWindows();
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
}
