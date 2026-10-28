using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowDesk.Core;
using FlowDesk.Windows;

namespace FlowDesk.App.Hybrid;

public sealed record IpcMessage(int Version, string? Id, string Type, object Payload);
public sealed record DesktopWindow(string Id, int ProcessId, string ProcessPath, string Title, Rect Bounds, bool Minimized);
public sealed record StageScene(string Id, string Name, string[] WindowIds, int Unavailable, bool Active);
public sealed record DockItem(string Id, string Name, string[] WindowIds, bool Running);
public sealed record ViewportState(long Version, PixelPoint Origin, Rect Bounds, double Scale, WindowDpi Display);
public sealed record DesktopState(DesktopWindow[] Windows, StageScene[] Scenes, DockItem[] Dock, ViewportState Viewport);
public sealed record IpcError(string Code, string Message);
public sealed record WindowCommand { public required string WindowId { get; init; } }
public sealed record SceneCommand { public required string SceneId { get; init; } }
public sealed record ThumbnailCommand
{
    public required string WindowId { get; init; }
    public required long ViewportVersion { get; init; }
    public required bool Visible { get; init; }
    public CssBounds? Bounds { get; init; }
}
public sealed record EmptyCommand;
internal sealed record Request
{
    public required int Version { get; init; }
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required JsonElement Payload { get; init; }
}
internal static class IpcJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 12
    };
    internal static Request Parse(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > 16 * 1024) throw new JsonException("Request exceeds 16 KiB.");
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 12 });
        Unique(doc.RootElement);
        var request = JsonSerializer.Deserialize<Request>(json, Options) ?? throw new JsonException("Missing request.");
        if (request.Version != 1 || !Guid.TryParseExact(request.Id, "N", out _) || (string.IsNullOrEmpty(request.Type) || request.Type.Length > 64)
            || request.Payload.ValueKind != JsonValueKind.Object) throw new JsonException("Invalid envelope.");
        return request;
    }
    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject()) { if (!keys.Add(property.Name)) throw new JsonException("Duplicate JSON property."); Unique(property.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var item in value.EnumerateArray()) Unique(item);
    }
    internal static T Payload<T>(Request request) => request.Payload.Deserialize<T>(Options) ?? throw new JsonException("Missing payload.");
}
