# Known limitations

[Documentation](README.md) · [Verification](verification.md) · [Compatibility](compatibility.md)

These boundaries apply to v0.5.0. Advanced desktop features are implemented; production qualification is incomplete.

## Desktop behavior

The app uses a native navigation shell, theme-aware surfaces, live window previews, and optional floating dock. Live DWM content can be unavailable for minimized/protected windows; cards provide a fallback. The overview renders at most 36 matching preview cards, so narrow search for larger inventories.

Display grouping/filtering and move/undo are implemented. Real mixed-DPI/multi-monitor/hot-plug compatibility remains unqualified. Windows virtual-desktop management and broad Windows system-setting controls are outside the implementation.

Native mutations can partially succeed. Known unresponsive windows are rejected, and inventory refresh runs off the UI thread in FlowControl/overview. Some native calls remain synchronous; an app can become unresponsive after the check. Windows may decline foreground activation.

Saved desktop layouts restore already-running windows matched by executable path/title. They do not relaunch missing apps or restore unsaved documents. Ambiguous or changed saved-title matches are skipped. Managed stage/focus recovery separately supports title changes through process identity and per-window markers.

## Persistence and recovery

Stage and focus/fullscreen original states are journaled before mutation. Startup attempts restoration, skips unavailable/identity-changed windows, and retains failed work for retry. Unsupported/corrupt journals are preserved and block new corresponding sessions until addressed. Recovery does not restore application documents or lost application processes.

Layout undo is one level and memory-only. Activity history is session-only. Saved URLs, titles, paths and preferences are local plaintext. Development builds use adjacent data; installed builds use stable installation data. Back up before cleanup, migration or manual repair.

## Focus and notifications

Borderless fullscreen is not exclusive game fullscreen. Focus minimizes selected distractions and quiets FlowDesk banners. It does not control global Windows Do Not Disturb or other apps' notifications.

The center contains FlowDesk events only. Windows controls native banner visibility. Stored payload and cold/warm activation are tested separately; a physical banner click was not automated. Tray double-click and broad assistive-technology coverage remain unestablished.

## FlowWeb

Sharing is explicit and session-based, restricted to normal HTTP/HTTPS tabs. Incognito is disabled. Limits: four connections, 50 shared tabs per connection, 20 saved groups, and 25 URLs per workspace. Disconnection clears live metadata.

Browser workspaces reopen URLs in a new window; history, login state, forms, media state and scroll position are not restored. There are no page content scripts or arbitrary OS-command capabilities.

End-to-end testing used isolated Chromium. Actual Edge UI/personal-profile and enterprise-policy validation remain pending. The extension is not store-published.

## Distribution

Self-contained package creation, checksum validation, local install/update, rollback and owned removal are implemented. Sign-in launch and native browser registration are opt-in. Signing requires an existing authorized certificate; no trusted certificate was available for this build.

Clean-machine prerequisites, trusted signing/timestamping, an online update service, signed graphical installer, extension store publication and broader release qualification remain pending. A local SHA256 is an integrity check, not publisher authentication. See [distribution](distribution.md).
