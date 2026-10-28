# Module 2 · WebView2 IPC and DPI-aware thumbnail bounds

[Documentation](../README.md) · [Architecture](../architecture.md) · [Verification](module-2-verification.md)

This module adds the native bridge for the planned React frontend. It does not replace the existing WinUI pages or implement Modules 3–6. The repository remains an unpackaged .NET 10 / Windows App SDK 1.8 application; it has not been retargeted or qualified on .NET 8. Windows 10 support is not established by the Windows 11 test run.

## Complete implementation files

| File | Responsibility |
| --- | --- |
| `src/FlowDesk.App/Hybrid/WebView2IpcBridge.cs` | CoreWebView2 message/event wiring, native commands, origin validation, identity tracking, DWM lifetimes and cleanup |
| `src/FlowDesk.Windows/DpiBoundsHelper.cs` | Checked user32/shcore queries and CSS-to-destination-client physical-pixel conversion |
| `src/FlowDesk.App/Hybrid/IpcContracts.cs` | Canonical C# envelope, request payloads and desktop/scene/dock/viewport DTOs |
| `src/FlowDesk.App/Hybrid/HybridWebViewHost.cs` | Full WebView2 environment, local mapping, settings, initialization and viewport tracking |
| `src/FlowDesk.App/Hybrid/HybridWorkspaceWindow.cs` | Complete runnable window initialization, startup recovery and guarded close paths |

These files compile against the repository's existing native services; no additional NuGet package was needed. TypeScript interfaces and `useFlowDeskIpc.ts` belong to Module 3 and must mirror the contract below.

## Initialization and ownership

`HybridWorkspaceWindow` receives a compiled web-build folder, a data folder, the existing `WindowService`/`StageManager`, a scene-provider callback and a focus/fullscreen guard. Set it as the single owner of that stage manager. Call `Activate()`, then await its `Initialization` task. A browser `BRIDGE_READY` event follows successful navigation and the initial inventory snapshot.

The compiled constructor and Loaded handler show the complete initialization. `HybridWebViewHost.InitializeAsync()` creates a WebView2 environment with its profile under the supplied data folder, awaits `EnsureCoreWebView2Async`, maps the build folder to `https://flowdesk.local`, constructs `WebView2IpcBridge` with the initialized `CoreWebView2`, and navigates to `/index.html`. The profile must be outside the mapped build folder. No global WebView2 environment variable or user browser profile is changed.

Use a trusted local build with `index.html` and local scripts/styles. A suitable application Content Security Policy is `default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:`; add only capabilities the actual frontend requires. Development servers and arbitrary remote pages are not allowed by this bridge. Host objects, zoom gestures, popups, downloads and permission requests are disabled or denied; frame navigation is canceled. Module 4 supplies the custom title bar, not this initialization window.

The window restores an existing journal before initialization and refuses ordinary close requests while initializing or running a native command. Both its programmatic `Close()` and caption close guard restoration; a Closed fallback preserves recovery information if a forced close interrupts an operation. Do not manage the same windows concurrently through a second stage/focus owner. Recovery errors stay visible and the journal is preserved.

## Wire contract · version 1

Every request is a JSON object with exactly `version`, `id`, `type`, `payload`. `version` is `1`; `id` is a fresh lowercase or uppercase 32-hex-character GUID without separators. Payloads are objects. Native responses use the same envelope and correlate `id`; unsolicited events use `id: null`. Malformed envelopes may receive an uncorrelated error.

All property names are **camelCase**, case-sensitive. Unknown members, duplicate JSON properties, missing required fields, non-object payloads and unsupported versions/types are rejected. Requests are limited to 16 KiB and depth 12. There are no raw HWND, executable-launch, shell-command or file-access commands.

| Command | Required payload | RESULT payload |
| --- | --- | --- |
| `GET_DESKTOP_STATE` | `{}` | `DesktopState` |
| `ACTIVATE_WINDOW` | `{windowId}` | `{accepted: boolean}`; Windows may decline foreground activation |
| `MINIMIZE_WINDOW` | `{windowId}` | `{minimized: boolean}` after checking native state |
| `ACTIVATE_SCENE` | `{sceneId}` | `{presented, minimized, skipped, errors, focusAccepted}` from the journaled stage service |
| `LEAVE_SCENE` | `{}` | The stage restoration result, including skips/errors |
| `UPDATE_THUMBNAIL_BOUNDS` | `{windowId, viewportVersion, visible, bounds?}` | `{registered: true, bounds, viewportVersion}` or `{registered: false}` |

Visible thumbnail `bounds` requires all four finite numeric members: `x`, `y`, `width`, `height`, taken from `getBoundingClientRect()`. Width/height must be positive; the rectangle must be wholly inside the WebView viewport. Hide a clipped/offscreen/animating container with `visible: false` instead of stretching its thumbnail. A false visibility command may omit bounds and releases the native DWM relationship.

`ERROR` payloads are `{code, message}`. Codes include `INVALID_REQUEST`, `INVALID_BOUNDS`, `NOT_FOUND`, `FORBIDDEN`, `BUSY`, `RATE_LIMIT`, `DUPLICATE_ID`, `REQUEST_LIMIT`, `NOT_READY` and `NATIVE_FAILURE`. A `RESULT` from a stage operation can still report partial native success through its counts/errors; it is not an unconditional success flag.

Wait for `BRIDGE_READY` before requests. Handle `BUSY` by waiting and retrying with a **new** ID. Coalesce geometry updates; the bridge allows 60 requests per second. Native activate/minimize/scene request IDs are retained for duplicate suppression, up to 4,096 per document session; reload the trusted document to start a new session. Inventory and thumbnail traffic do not consume that mutation ledger. Client request timeouts and unmount cleanup are Module 3 responsibilities.

## Shared DTO shapes for Module 3

| Type | Fields |
| --- | --- |
| `IpcMessage` | `version: number`, `id: string or null`, `type: string`, `payload: object` |
| `DesktopWindow` | `id: string`, `processId: number`, `processPath: string`, `title: string`, `bounds: Rect`, `minimized: boolean` |
| `StageScene` | `id: string`, `name: string`, `windowIds: string[]`, `unavailable: number`, `active: boolean` |
| `DockItem` | `id: string`, `name: string`, `windowIds: string[]`, `running: boolean` |
| `DesktopState` | `windows: DesktopWindow[]`, `scenes: StageScene[]`, `dock: DockItem[]`, `viewport: ViewportState` |
| `ViewportState` | `version: number`, `origin: {x,y}`, `bounds: Rect`, `scale: number`, `display: WindowDpi` |
| `WindowDpi` | `dpi: number`, `scale: number`, `monitorScalePercent: number`, `workArea: Rect` |
| `Rect` | `x: number`, `y: number`, `width: number`, `height: number` |

Window IDs are ephemeral opaque GUIDs backed by a per-window marker plus process/start-time identity, preventing recycled handles from accepting old commands. Scene IDs are saved workspace GUIDs. Dock IDs are deterministic SHA256 strings of executable paths; these are running-app summaries, not persistent pinned dock preferences. Display/rectangle values are physical pixels; request thumbnail bounds alone are CSS pixels. No native pointer is serialized as a JavaScript number.

## Native events and lifetime

`BRIDGE_READY` and `WINDOWS_CHANGED` carry `DesktopState`. `WINDOW_CREATED` carries `DesktopWindow`. `WINDOW_CLOSED`, `WINDOW_ACTIVATED` and `FOCUS_CHANGED` carry `{windowId: string or null}`. An unknown/FlowDesk-owned foreground window produces null. `DPI_CHANGED` carries `ViewportState`; despite its name, it also invalidates geometry on native resize, WebView offset changes or display/work-area changes.

The bridge reuses the existing WinEvent inventory source, coalesces inventory changes, and samples foreground state on a 250 ms dispatcher timer. It does not promise lossless intermediate shell transitions. If the inventory hook is unavailable, the timer refreshes inventory as a fallback. Readable inventory is capped at 128 windows and simultaneous thumbnails at 32.

All WebView access and bridge ownership belong to the UI thread. Enumeration and stage mutations run away from that thread; overlapping native requests receive `BUSY`. Browser process failure stops readiness and clears previews. Navigation/reload, hidden containers, source destruction, viewport changes and disposal release the relevant relationships. Native mutation guards must be supplied by the owning application, including its focus/fullscreen policy.

## DPI decisions and limitations

Use the **destination** WebView's `XamlRoot.RasterizationScale`, not a source window's scale or a desktop-wide multiplier. The host adds the WebView's physical client offset, rounds the top/left down and bottom/right up, and validates against the destination viewport. Negative monitor origins remain physical coordinates; they are not globally rescaled. `ClientScreenOrigin()` is available when a caller needs screen coordinates.

`GetDpiForWindow` reads the destination DPI; shcore `GetScaleFactorForMonitor` provides monitor-scale metadata. Per-monitor work areas use `GetMonitorInfo`, because `SPI_GETWORKAREA` alone describes the primary display. The existing manifest already declares Per-Monitor V2 awareness. HRESULT APIs are validated with `Marshal.ThrowExceptionForHR`; APIs documenting last-error failure use `Marshal.GetLastWin32Error()`. APIs such as `GetDpiForWindow` do not document a useful last-error value, so their documented zero return is checked directly.

Browser zoom is fixed at 100%; actual WebView devicePixelRatio was compared with native rasterization scale in the integration run. Physical mixed-DPI monitors and zoom changes are not qualified. DWM is a native rectangular relationship: it does not inherit CSS clipping, rounded corners, z-index, opacity or spring transforms. Registration/geometry tests do not establish visual compositor fidelity; Module 5 must coordinate visibility and animation settling, and complete visual review remains pending.

Official references: [WinUI WebView2](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/webview2), [WebView2 security](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security), [GetDpiForWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow), [GetScaleFactorForMonitor](https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getscalefactorformonitor).
