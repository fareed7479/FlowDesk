# Module 2 verification

[Module guide](module-2.md) · [All verification](../verification.md)

Date: **2026-10-28**. Actual Windows 11 x64 build 26200, project-local .NET SDK 10.0.403 and Windows App SDK 1.8.260921001. WebView2 Runtime was available; the module was exercised through an actual WinUI WebView2 control, not a simulated transport.

| Check | Result |
| --- | --- |
| Solution/final application compilation | Passed, zero warnings and errors |
| Native/controller suite | 125 assertions passed, including four new DPI geometry cases |
| Actual WebView2 Module 2 workflows | 23 passed |
| Existing advanced WinUI workflows | 34 passed |

The Module 2 run serves synthetic local HTML/JS through the real virtual-host mapping. It verifies readiness, origin checks, native created/closed/foreground/focus events, opaque window identities, browser/native scale agreement, actual DWM registration and release, offset-aware bounds, unknown-field rejection, mutation policy, actual minimize/activate effects, duplicate mutation IDs, journaled scene activation/restoration, resize epochs, stale/offscreen bounds rejection, external navigation cancellation and close-time scene restoration.

The complete `HybridWorkspaceWindow` is the initialization exercised by these checks. Testing exposed that WinUI programmatic Close can bypass AppWindow.Closing; the host now applies its recovery guard explicitly to programmatic close as well as caption close, with a Closed fallback. Tests also surfaced a sender casing mismatch and an assumption that native inventory would never be busy; the sender now uses the canonical camelCase serializer and handles documented BUSY replies with new IDs.

DPI checks cover fractional rounding, a physical destination offset, synthetic negative origins, nonfinite scale rejection and clipped rectangles. They do not establish real mixed-monitor behavior. The app remains .NET 10; .NET 8 retargeting, Windows 10 qualification, browser-crash injection, long-duration/rate-limit saturation, keyboard/pinch zoom changes and visual DWM/WebView compositor fidelity were not established by this run. React/TypeScript components and Apple-style UI are later modules, not claimed here.

To repeat from the repository in PowerShell 7:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\build.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-module2.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-advanced.ps1
```

Use an unlocked Windows desktop and avoid concurrent interaction. Checks move only their temporary fixture windows. Browser profiles, messages containing local inventory, runtime logs and journals remain under ignored D:-based `.cache` folders; they are not committed. The new test entry point is isolated from normal application startup. The normal WinUI UI is retained.
