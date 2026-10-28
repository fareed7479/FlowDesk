using System.ComponentModel;
using System.Runtime.InteropServices;
using FlowDesk.Core;

namespace FlowDesk.Windows;

public sealed record CssBounds(
    [property: System.Text.Json.Serialization.JsonRequired] double X,
    [property: System.Text.Json.Serialization.JsonRequired] double Y,
    [property: System.Text.Json.Serialization.JsonRequired] double Width,
    [property: System.Text.Json.Serialization.JsonRequired] double Height);
public sealed record PixelPoint(int X, int Y);
public sealed record WindowDpi(uint Dpi, double Scale, int MonitorScalePercent, Rect WorkArea);

/// <summary>Coordinates are destination-client physical pixels, never source-monitor pixels.</summary>
public static class DpiBoundsHelper
{
    public static WindowDpi Inspect(nint destination)
    {
        if (!IsWindow(destination)) throw new ArgumentException("Destination window is unavailable.", nameof(destination));
        uint dpi = GetDpiForWindow(destination);
        // GetDpiForWindow documents zero, not GetLastError, as its failure signal.
        if (dpi == 0) throw new InvalidOperationException("Windows could not determine the destination DPI.");
        nint monitor = MonitorFromWindow(destination, 2);
        if (monitor == 0) throw new InvalidOperationException("Destination monitor is unavailable.");
        int hr = GetScaleFactorForMonitor(monitor, out int percent);
        Marshal.ThrowExceptionForHR(hr);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        var work = info.Work;
        return new(dpi, dpi / 96d, percent, new(work.Left, work.Top, work.Right - work.Left, work.Bottom - work.Top));
    }

    public static Rect ClientArea(nint destination)
    {
        if (!GetClientRect(destination, out var rect)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static PixelPoint ClientScreenOrigin(nint destination)
    {
        var point = new Point();
        if (!ClientToScreen(destination, ref point)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new(point.X, point.Y);
    }

    /// <summary>Pass getBoundingClientRect() values, the native WebView client offset, and
    /// XamlRoot.RasterizationScale. Browser zoom is fixed at 100% by this module's host.</summary>
    public static Rect CssToClientPixels(CssBounds bounds, double scale, PixelPoint webViewOrigin, Rect viewport)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        if (!double.IsFinite(scale) || scale < .5 || scale > 8) throw new ArgumentOutOfRangeException(nameof(scale));
        if (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height)
            || bounds.X < 0 || bounds.Y < 0 || bounds.Width <= 0 || bounds.Height <= 0
            || bounds.X + bounds.Width > 1_000_000 || bounds.Y + bounds.Height > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(bounds), "Thumbnail bounds must be finite, positive viewport coordinates.");
        int left = checked(webViewOrigin.X + (int)Math.Floor(bounds.X * scale));
        int top = checked(webViewOrigin.Y + (int)Math.Floor(bounds.Y * scale));
        int right = checked(webViewOrigin.X + (int)Math.Ceiling((bounds.X + bounds.Width) * scale));
        int bottom = checked(webViewOrigin.Y + (int)Math.Ceiling((bounds.Y + bounds.Height) * scale));
        if (viewport.Width < 1 || viewport.Height < 1 || left < viewport.X || top < viewport.Y
            || right > (long)viewport.X + viewport.Width || bottom > (long)viewport.Y + viewport.Height)
            throw new ArgumentOutOfRangeException(nameof(bounds), "A clipped thumbnail must be hidden, not stretched into a smaller rectangle.");
        return new(left, top, checked(right - left), checked(bottom - top));
    }

    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetClientRect(nint window, out NativeRect rect);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool ClientToScreen(nint window, ref Point point);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("shcore.dll")] private static extern int GetScaleFactorForMonitor(nint monitor, out int scale);
}
