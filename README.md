# FlowDesk v0.3.0

Native C# / WinUI 3 desktop experience for Windows 11 x64. This development release adds FlowDesk notifications and recoverable fullscreen/focus sessions to the existing layouts, workspaces, Stage Manager and desktop overview.

## Run

Build and run controller/native checks with `powershell -ExecutionPolicy Bypass -File scripts/build.ps1`. Launch with `powershell -ExecutionPolicy Bypass -File scripts/run.ps1`.

The selected development environment keeps .NET SDK 10.0.403 in `.tools/dotnet` and caches in `.cache`, on D:. These are excluded from Git. A fresh clone needs that SDK installed locally or the build script adjusted to an existing SDK. Follow the official WinUI setup reference below. The app output now includes its .NET runtime so Windows can reopen it from a notification without inheriting a development terminal's environment. Distribute the complete output folder, not the EXE alone. Signing and installer packaging remain separate work.

## Notifications — Phase 5

Open Notifications to review FlowDesk events, show unread only, mark individual/all cards read, dismiss a card, or clear the center. History and preferences persist locally, with a maximum of 100 cards. The tab shows the unread count.

Windows banners are opt-in; sound defaults to silent. Test notification exercises delivery. Cards record whether delivery stayed in-app, was quieted, was submitted to Windows, or failed. Dismissing/clearing removes the corresponding FlowDesk notifications from Windows history as well. A delivery failure preserves the local card.

Notification links use `flowdesk-control://notification/<id>` to open the center and mark the matching card read. Cold/warm launches share one app instance. Protocol inputs accept only a notification GUID; extra parameters and injected command flags are rejected. The app registers its own per-user protocol and Windows notification identity. It refuses to take over a protocol owned by another application.

This center contains FlowDesk's own events. Reading or replacing other applications' notifications is outside this phase. Windows controls banner visibility and may suppress popups under its notification policies.

## Fullscreen and focus — Phase 6

In Focus & fullscreen:

1. Refresh windows and choose a target.
2. Select the distractions to minimize. Exclude selected apps to protect their windows; exclusions persist by executable path.
3. Enter 1–240 whole minutes and Start focus. Optionally use borderless fullscreen for the target.
4. Start fullscreen creates a standalone borderless session for the target.
5. End session or **Ctrl+Alt+F10** restores the original frame, normal placement, and minimized/maximized state. Toggle FlowDesk fullscreen uses the native WinUI fullscreen presenter for FlowDesk itself.

Only selected distractions participate. Focus and fullscreen quiet FlowDesk banners while retaining local notification cards. They do not change Windows Do Not Disturb or global notification/privacy settings. A focus timer or a closed target ends the session and restores surviving windows. Stage switching and layout mutations are blocked while a mode is active; leave Stage before entering a mode.

Before changing windows, FlowDesk writes a recovery journal. If the app exits unexpectedly, the next startup attempts restoration automatically. A unique property on each managed HWND, plus process identity and start time, prevents recovery from acting on a recycled handle. Document-title changes are supported. Missing/closed windows are skipped; failed restores remain available for retry. Normal close and programmatic close both attempt restoration. If the recovery shortcut is already occupied, new external sessions are blocked with a visible explanation.

## Earlier features

- FlowControl: window inventory, 1–4 column splits, single-workspace save, restore preview and one-step layout undo.
- Workspaces & Stage: named groups, group preview/restore, basic stage switching and inactive-group minimization. Shared members remain visible; unrelated windows stay untouched.
- Overview & FlowDock: searchable window-title overview and running-app buttons that cycle through windows. These are in-app panels; thumbnails and a floating/pinned dock remain future work.
- Activity history and a basic tray entry point.

## Verification

See [v0.3.0 verification](docs/verification-v0.3.0.md) and the [phase matrix](docs/roadmap.md). The implementation is tested on Windows 11 x64 build 26200 with temporary fixture windows; no existing user windows are moved by the test suites.

- `scripts/build.ps1`: controller/native assertions.
- `scripts/verify-desktop.ps1`: actual WinUI controls, Windows notification history, one-minute timer, process restart recovery and app-close restoration. Requires an unlocked interactive desktop and a closed FlowDesk instance.
- `scripts/verify-activation.ps1`: sends generic test notifications, checks Windows protocol cold/warm activation, rejects injected flags, verifies one-instance behavior, then removes the probes and restores previous preferences.
- The physical Ctrl+Alt+F10 shortcut was also tested from a fullscreen fixture. The opt-in `--hotkey-check` workflow supports repeating this interactive check.

## Development limits

The new phases are complete for the scope above. A physical click on a Windows popup was not automated: verification covers the notification payload stored by Windows and its registered protocol activation path. Banner appearance remains subject to Windows settings. Cross-app notification aggregation and system-wide Focus/Do Not Disturb control are not included.

Borderless fullscreen is not exclusive game fullscreen. Custom/elevated/protected or unresponsive apps may reject operations; there is no elevation bypass. Native calls are synchronous and can delay the UI when an app is unresponsive. Mixed DPI, monitor topology changes, elevated applications, packaging/signing and broader hardware/app compatibility still require validation.

Workspace JSON, notification preferences/history and the mode journal live in `data/` beside the executable on D:. Back up this directory before removing build output. It contains window/executable metadata and is ignored by Git. Unreadable data is preserved; an invalid recovery journal blocks new mode changes until repaired or backed up manually. Stage Manager snapshots are still session-only; durable recovery added here applies to fullscreen/focus sessions.

## Official references

- [WinUI setup](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here)
- [Windows App SDK app notifications](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-dotnet)
- [App notification content schema](https://learn.microsoft.com/en-us/uwp/schemas/tiles/toastschema/schema-root)
- [Handle URI activation](https://learn.microsoft.com/en-us/windows/apps/develop/launch/handle-uri-activation)
- [SetWindowPlacement](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowplacement)
- [SetWindowLongPtr](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowlongptrw)
- [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)
