# FlowDesk v0.2.0

A native C# / WinUI 3 desktop experience prototype for Windows 11 x64. Core logic, Windows interop, UI and executable checks are separate projects. This is an early application, not a replacement Windows shell.

## Run locally

Run `powershell -ExecutionPolicy Bypass -File scripts/build.ps1` to build and execute checks, then `powershell -ExecutionPolicy Bypass -File scripts/run.ps1` to launch. Close FlowDesk before rebuilding. The selected development environment uses a project-local .NET SDK 10.0.403 in `.tools/dotnet`, with NuGet and CLI state in `.cache` on D:. These tools and build outputs are excluded from Git.

A fresh clone needs the .NET SDK installed into `.tools/dotnet` or the script paths adjusted to an existing SDK. Follow Microsoft's setup documentation linked below. First restore needs network access. The verified development environment builds without a full Visual Studio installation; other environments may need WinUI build prerequisites.

## Available prototype features

- **FlowControl:** refresh accessible desktop windows, split 1–4 selections into columns, save a single workspace, preview/apply restoration, and undo the last successful layout operation.
- **Named workspaces:** create, replace and remove named groups; preview and restore a selected group. Names must be unique. A legacy single workspace is imported when the library is first created.
- **Stage Manager:** activate saved groups. Previously staged windows outside the requested group are minimized; unrelated windows are left alone. Shared members stay visible. Leave Stage restores original placement, including minimized/maximized state. Normal app close attempts restoration and is cancelled if restoration errors leave tracked windows pending.
- **Mission Control overview:** a searchable list of current window titles and app names. Click a window to request activation. Refresh updates the inventory.
- **FlowDock panel:** running apps grouped by executable path; repeated clicks cycle through their current windows. This panel lives inside FlowControl.
- **Activity:** an in-memory history of workspace, stage and overview messages, capped at 100 entries. This is not a Windows notification listener.
- **Tray:** double-click requests opening FlowControl. Closing the app exits it; it does not keep running in the tray.

## Trying the new features

1. In Workspaces & Stage, enter a name, select current windows, then Save new. Restore minimized windows before saving a group layout.
2. Select a saved group and Preview; Apply restore sends the planned positioning requests.
3. Select Stage group to enter/switch groups. Only windows from groups activated during this session participate in minimization. Leave Stage restores their original placement.
4. In Overview & FlowDock, search by title/app name or use the running-app buttons. Refresh after opening or closing apps.

## Verification and limits

The solution builds with zero warnings/errors and passes 34 assertions on Windows 11 x64 build 26200. Native tests use separate temporary windows and read placement back from Windows. See [v0.2.0 verification](docs/verification-v0.2.0.md) for the exact tested scope.

- Window matching uses exact executable path and title. Missing, changed or ambiguous identities are skipped. Windows are not launched automatically. Protected/elevated apps may be inaccessible; there is no elevation bypass.
- Tool windows, no-activate overlays, owned windows, untitled windows, shell windows, cloaked windows and FlowDesk's own windows are excluded from inventory.
- Some apps enforce their own sizes or ignore requests. Operations can partially succeed. Status counts indicate successful API requests. Windows may decline foreground activation.
- Stage switching uses minimize/restore, not hidden windows, virtual desktops or shell replacement. State is held in memory; crash/forced-termination recovery is not implemented. Changed titles can prevent restoration; minimized windows remain accessible from the taskbar.
- Stage exit restores show state and placement. Ordinary workspace restore and layout undo still restore rectangles only. Undo is one level deep and lasts for the current session.
- Workspace JSON next to the executable in `data/` contains titles and executable paths. Back it up before cleaning build output. It is ignored by Git. An unreadable library is not silently overwritten.
- Live thumbnails, virtual desktops, pinned-app launching, floating/auto-hide dock, global shortcuts, automatic inventory updates, notification integration, fullscreen/focus modes, FlowWeb and startup registration are not implemented.
- Mixed DPI, multiple monitors/topology changes, unresponsive apps, protected apps, tray interactions and packaging/signing need broader validation. Native operations currently run on the UI thread, so slow applications can delay the interface.

## Phase status

Phase 0 environment audit and Phase 1 layout/workspace prototype are complete within their documented test scope. This increment adds basic Stage Manager and named groups plus the first Mission Control/FlowDock panels. These are initial implementations, not completed macOS-style feature parity. See [roadmap](docs/roadmap.md) for remaining phases.

GitHub: https://github.com/fareed7479/FlowDesk — repository publishing authorized by the owner.

## Official references

- [WinUI development setup](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here)
- [GetWindowPlacement](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowplacement)
- [SetWindowPlacement](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowplacement)
- [Extended window styles](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles)
- [SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
