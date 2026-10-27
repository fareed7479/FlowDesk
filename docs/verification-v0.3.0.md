# Verification — v0.3.0

Date: 2026-10-27. Windows 11 x64 build 26200. Project-local .NET SDK 10.0.403; Windows App SDK 1.8.260921001.

## Build and automated checks

The solution/controller checks passed with zero warnings/errors. 66 assertions cover prior behavior plus notification persistence, opt-in/quiet/failed delivery, bounded history, read/dismiss/clear, strict protocol parsing, focus exclusions/expiry, overlapping-session rejection, uniquely marked-window recovery, changed document titles, borderless frame/monitor bounds, minimized/maximized placement restoration, target exit, preservation of application-owned dynamic style flags and corrupt-journal protection.

23 desktop workflow checks passed. The desktop workflow suite drives actual WinUI button/toggle controls through their automation peers and verifies native effects on separate fixture processes. Its results cover local card/read/filter/dismiss, actual Windows submission/history/removal, notification protocol payload, selected-distraction focus, quiet native delivery, exclusions, fullscreen entry/exit, native FlowDesk presenter entry/exit, a real one-minute timer, all-read/clear, and active-session restoration on window close. It also starts a separate process that exits with a recovery journal intact and checks a fresh UI process automatically restores both affected test windows.

## Additional interactive/system checks

- Ctrl+Alt+F10 was sent through the Windows computer-use interface to an actual fullscreen fixture. The registered global shortcut ended the combined focus/fullscreen session and restored both fixture placements; the test reported success.
- Windows ShellExecute of the notification protocol opened FlowDesk from a stopped state and marked the cold test card read. A second unread card activated the existing instance; a normal second launch redirected and exited. Injected extra command flags were rejected. The activation suite removes its generic probe notifications and restores the prior notification preferences.
- The v0.3.0 shell and notification center were inspected on the desktop. Windows reported native notification delivery enabled.

## Validation boundaries

A direct physical click on a Windows banner was not automated because the desktop tool did not expose a targetable banner. The stored native notification payload and the complete protocol activation path are tested separately. Windows can suppress popups according to system/app settings. The new notification center contains FlowDesk events; it does not read other apps' notifications.

Fullscreen/focus verification uses standard WinForms fixture windows on this supported Windows desktop. It does not establish universal compatibility with custom chrome, protected/elevated apps, exclusive games, mixed DPI or changing monitor topology. Native calls remain synchronous; unresponsive applications can delay the UI. Corrupt journals are preserved for manual attention. Release packaging/signing, broader hardware tests and system-wide notification/focus control remain outside this increment.

No existing user windows were moved by the test suites. Test reports and runtime data stay local and are excluded from Git. The application's own per-user notification identity/protocol registration is required for native integration; unrelated registrations are not replaced.
