# Compatibility evidence

[Documentation](README.md) · [Verification](verification.md) · [Limitations](limitations.md)

Recorded on Windows 11 x64 build 26200. “Passed” below describes controlled checks on this machine, not universal application support.

| Scenario | Evidence in v0.6.0 |
| --- | --- |
| Standard WinForms windows | Native layout, undo, focus/fullscreen and restoration checks passed |
| Borderless app window | Inventory, movement and restoration fixture checks passed |
| Hidden/tool windows | Controlled fixtures excluded from inventory |
| Unresponsive app | Windows detected a deliberately blocked fixture; mutation was rejected promptly |
| Changed document title | Managed stage and focus recovery use live identity; recovery checks passed |
| Closed/recycled windows | Existing identity/marker checks skip unavailable targets |
| Minimized/maximized state | Native stage/focus/fullscreen restoration checks retained |
| Display metadata and DPI | Actual monitor inventory, primary/work-area metadata and per-window DPI queried |
| Negative coordinates / offscreen layout | Synthetic geometry assertions; not a substitute for physical mixed-monitor tests |
| Live preview | Real DWM relationships registered by visible WinUI cards |
| Stage strip | Separate native window, real DWM registration, reserved space, serialized switching and restoration checked |
| Reduced motion | Persisted preference and no-animation path checked; physical OS setting coverage remains pending |
| Floating dock | Real owned window, persistent pins, auto-hide and clean close checked |
| Chrome/Edge protocol separation | Authenticated simulated peers with overlapping tab IDs checked |
| Browser end-to-end | Isolated Chromium extension → native host → WinUI controls checked |
| Local distribution | Fixture install/update/rollback/removal plus actual published-package smoke test; see versioned report |

## Environments still requiring validation

- Real multiple monitors with different scale factors, orientation, taskbar placement, and hot-plug.
- Elevated/protected applications, custom rendering/game windows, other Windows builds and hardware.
- Large inventories, prolonged use, assistive technologies, reduced-motion/high-contrast behavior.
- Personal Chrome/Edge profiles, enterprise browser policies, extension store identities.
- Fresh machines without the audited development machine's installed runtimes/prerequisites.

Known-hung checks reduce one source of blocking. Native operations still include synchronous APIs and races are possible if an app hangs after validation. Window requests can partially succeed; Windows controls foreground activation. No unsupported private virtual-desktop API or privilege bypass is used.

## Official API references

- [DWM thumbnail overview](https://learn.microsoft.com/en-us/windows/win32/dwm/thumbnail-ovw)
- [SetWinEventHook](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook)
- [SetWindowPlacement](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowplacement)
- [App notification registration cleanup](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.appnotifications.appnotificationmanager.unregisterall?view=windows-app-sdk-1.8)
