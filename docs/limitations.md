# Known limitations

[Documentation](README.md) · [Verification](verification.md) · [Roadmap](roadmap.md)

These limits apply to the v0.4.0 development prototype.

## Product completeness

- Stage Manager implements window groups and switching, without the full visual stage experience or durable stage crash recovery.
- Mission Control is a searchable title/app list, without live thumbnails, monitor organization, or virtual-desktop management.
- FlowDock is an in-app running-app panel, without a floating/auto-hide dock or user-selected pins.
- FlowControl opens Windows Settings but does not implement a comprehensive set of system-setting controls.
- Packaging, signing, startup opt-in, updating, store publication, and a complete install/uninstall lifecycle are pending.

## Windows and application behavior

Only Windows 11 x64 build 26200 is represented by the recorded end-to-end runs. Mixed DPI, multi-monitor changes, other hardware, large inventories, and protected/elevated or unresponsive apps need broader testing.

Inventory refresh is explicit in the basic panels. Apps may enforce minimum sizes, reject positioning, or deny foreground activation. Native calls are synchronous and can delay the UI. Operations may partially succeed; request counts do not guarantee identical visual results across applications.

Desktop workspace restore targets existing windows. Executable-path/title matching can skip changed document titles or ambiguous matches. It does not reopen missing applications, recover application documents, or recreate unsaved work. Basic layout restore is rectangle-oriented; focus/fullscreen recovery separately preserves managed native placement and frame state.

Borderless fullscreen is not exclusive game fullscreen. The app has no elevation bypass. Focus quiets FlowDesk banners and minimizes selected distractions; it does not set Windows Do Not Disturb, change global privacy settings, or suppress every other app.

## Notifications and activation

The notification center contains FlowDesk events only. Windows may suppress banners according to app/system policy. Recorded tests cover stored Windows notification payloads and cold/warm protocol activation; a physical click on a Windows popup was not automated.

Tray double-click has not been established by the recorded end-to-end suites. Closing the main window exits FlowDesk.

## FlowWeb

The full browser/UI test used isolated Chromium, not personal Chrome or Edge profiles. Actual branded-browser UI validation and enterprise browser policy scenarios remain pending. Personal extension loading is manual, and the extension is not store-published.

Sharing is opt-in, session-based, and limited to normal HTTP/HTTPS tabs. Incognito is disabled. The bridge accepts up to four connections, each sharing at most 50 tabs. Browser workspaces allow up to 25 URLs each and 20 saved groups.

Restoration opens URLs in a new browser window; it does not restore history, login state, form data, media state, or scroll position. Saved URLs are plaintext. The extension has no page-content scripts or arbitrary OS-command interface.

## Recovery and distribution

One-step undo, activity history, and stage snapshots are memory-only. Durable recovery covers fullscreen/focus windows; it skips missing or identity-changed windows and retains failed restoration work for retry. Invalid recovery data needs manual attention.

Data lives next to development output. Back it up before cleaning the build directory. Self-contained output is not proof of clean-machine portability; runtime dependencies, signing, installer behavior, upgrade migration, and rollback still need release qualification.
