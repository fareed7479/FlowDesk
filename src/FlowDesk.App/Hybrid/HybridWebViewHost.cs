using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace FlowDesk.App.Hybrid;

/// <summary>Complete WinUI initialization for a locally built React distribution.
/// Call InitializeAsync after Loaded; dispose before closing the owner.</summary>
public sealed class HybridWebViewHost : Grid, IDisposable
{
    public WebView2 WebView { get; } = new();
    public WebView2IpcBridge? Bridge { get; private set; }
    public string? LastError { get; private set; }
    private readonly Window owner;
    private readonly string assetDirectory, profileDirectory;
    private readonly WindowService service;
    private readonly StageManager stages;
    private readonly Func<IReadOnlyList<WorkspaceGroup>> scenes;
    private readonly Func<bool> blocked;
    private ViewportState? viewport;
    private long version;
    private bool disposed, initializing;

    public HybridWebViewHost(Window owner, string assetDirectory, string profileDirectory,
        WindowService service, StageManager stages, Func<IReadOnlyList<WorkspaceGroup>> scenes, Func<bool> blocked)
    {
        this.owner = owner; this.assetDirectory = Path.GetFullPath(assetDirectory);
        this.profileDirectory = Path.GetFullPath(profileDirectory);
        if (this.profileDirectory.Equals(this.assetDirectory, StringComparison.OrdinalIgnoreCase)
            || this.profileDirectory.StartsWith(Path.TrimEndingDirectorySeparator(this.assetDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Keep browser profile data outside the published web assets.", nameof(profileDirectory));
        this.service = service; this.stages = stages; this.scenes = scenes; this.blocked = blocked;
        Children.Add(WebView);
        LayoutUpdated += Layout;
        owner.AppWindow.Changed += WindowChanged;
    }

    public async Task InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (Bridge is not null || initializing) throw new InvalidOperationException("Initialize this host once.");
        if (!IsLoaded || XamlRoot is null) throw new InvalidOperationException("Initialize after the host Loaded event.");
        if (!File.Exists(Path.Combine(assetDirectory, "index.html"))) throw new FileNotFoundException("The local web build must contain index.html.");
        initializing = true;
        try
        {
            Directory.CreateDirectory(profileDirectory);
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, profileDirectory, null);
            if (disposed) return;
            await WebView.EnsureCoreWebView2Async(environment, null);
            if (disposed) { WebView.Close(); return; }
            var core = WebView.CoreWebView2 ?? throw new InvalidOperationException("WebView2 initialization did not create a browser.");
            core.Settings.IsWebMessageEnabled = true;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsPinchZoomEnabled = false;
            core.SetVirtualHostNameToFolderMapping("flowdesk.local", assetDirectory, CoreWebView2HostResourceAccessKind.DenyCors);
            viewport = CaptureViewport();
            Bridge = new(core, WinRT.Interop.WindowNative.GetWindowHandle(owner), new Uri("https://flowdesk.local/index.html"),
                viewport, service, stages, scenes, blocked);
            core.Navigate("https://flowdesk.local/index.html");
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Bridge?.Dispose(); Bridge = null;
            throw new InvalidOperationException("The hybrid workspace could not start. Check the local build and WebView2 Runtime. " + ex.Message, ex);
        }
        finally { initializing = false; }
    }

    private ViewportState CaptureViewport()
    {
        if (WebView.XamlRoot is null) throw new InvalidOperationException("WebView2 is not attached to a XAML root.");
        double scale = WebView.XamlRoot.RasterizationScale;
        var offset = WebView.TransformToVisual(null).TransformPoint(new global::Windows.Foundation.Point());
        int x = (int)Math.Round(offset.X * scale), y = (int)Math.Round(offset.Y * scale);
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(owner);
        var client = DpiBoundsHelper.ClientArea(handle);
        var bounds = new Rect(x, y, Math.Max(0, Math.Min((int)Math.Floor(WebView.ActualWidth * scale), client.Width - x)),
            Math.Max(0, Math.Min((int)Math.Floor(WebView.ActualHeight * scale), client.Height - y)));
        return new(++version, new(x, y), bounds, scale, DpiBoundsHelper.Inspect(handle));
    }
    private void WindowChanged(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs e) => UpdateViewport();
    private void Layout(object? sender, object e) => UpdateViewport();
    private void UpdateViewport()
    {
        if (disposed || Bridge is null || viewport is null) return;
        try
        {
            var next = CaptureViewport();
            if (next.Origin == viewport.Origin && next.Bounds == viewport.Bounds && next.Scale == viewport.Scale && next.Display == viewport.Display) return;
            viewport = next; Bridge.UpdateViewport(next);
        }
        catch (Exception ex) { LastError = ex.Message; }
    }
    public void Dispose()
    {
        if (disposed) return;
        if (Bridge is { IsBusy: true, IsReady: true }) throw new InvalidOperationException("Wait for the native command to finish before disposing the host.");
        disposed = true; LayoutUpdated -= Layout; owner.AppWindow.Changed -= WindowChanged;
        Bridge?.Dispose(); WebView.Close();
    }
}
