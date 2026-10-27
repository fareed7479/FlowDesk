# FlowDesk v0.1.1 prototype

Native C# / WinUI 3 FlowControl with a separate pure core and Win32 integration layer. Windows 11 x64 is the initial target. This is an early prototype, not a replacement Windows shell.

## Build and run

The project-local .NET SDK is in `.tools/dotnet`. Run `powershell -ExecutionPolicy Bypass -File scripts/build.ps1` to build and run the checks, or add `-Run` to launch FlowControl. Dependencies and CLI state stay under `.cache` on D:. First restore requires network access. WinUI XAML compilation may require additional Windows build tools; see docs/phase-0-audit.md for actual results.

## Prototype interaction

Refresh lists visible, titled, non-owned, non-cloaked windows accessible to this process. Select one to four and click Split to place them in columns on the first selected window's monitor. Save persists current selected window rectangles. Preview restore shows matched windows and target rectangles. Apply restore rechecks identities and skips closed, changed or ambiguous windows. Undo restores rectangles from the last successful split or restore; history lasts only for the current session. Targets are clamped to the current monitor for restore and undo. Windows are never launched automatically. Settings opens Windows Settings. Tray double-click opens FlowControl; closing the window exits and removes the tray icon.

## Limitations

- Restore is conservative: changed document titles cannot match. No window state (maximized/minimized), browser tabs, process launching or historical monitor topology restoration yet.
- Protected/elevated applications may be inaccessible. No elevation bypass. Apps can enforce minimum sizes or ignore positioning. Rectangle calculations use native window bounds, including non-client borders; no invisible-border correction yet.
- Split and restore can partially complete if a window disappears or refuses a move; no transactional rollback yet.
- Workspace JSON stores titles and executable paths locally in the app output data directory. Do not publish this file; build output and data are ignored by Git.
- No auto-start, global shortcuts, settings toggles, notification interception, fullscreen management or browser extension yet.
- No package signing, installer or release qualification yet. Native interaction needs manual testing across DPI, displays, taskbar placement and protected apps.

## Modules and next phases

Core: geometry, layouts, workspace schema and matching. Windows: documented Win32 APIs. App: WinUI FlowControl and a WinForms tray icon. Checks: executable assertion harness and read-only native smoke checks.

Implemented next increment: restore preview and one-step undo. Next: monitor/DPI and window-state persistence; Stage Manager grouping; Mission Control overview; FlowDock; supported notification and focus APIs; optional permission-scoped Chrome/Edge FlowWeb bridge. Each feature requires an independently verified increment.

GitHub: https://github.com/fareed7479/FlowDesk — publishing authorized by the repository owner on 2026-10-27. See docs/verification-v0.1.0.md for tested scope.

References: https://learn.microsoft.com/en-us/windows/apps/get-started/start-here • https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows • https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos • https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmgetwindowattribute

Launch an existing build with `powershell -ExecutionPolicy Bypass -File scripts/run.ps1`. Close FlowDesk before rebuilding. Verification for this increment: docs/verification-v0.1.1.md.
