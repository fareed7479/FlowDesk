# Release history

[Project home](README.md) · [Roadmap](docs/roadmap.md) · [Verification](docs/verification.md)

These entries describe committed development versions. They are not claims of signed releases, installers, or store publication.

## v0.5.0 — Advanced desktop and local distribution

- Redesigned all pages around adaptive navigation, theme-aware cards, Mica, transitions, appearance preferences and a focus countdown.
- Added live DWM overview/stage previews, display organization/move undo and event-driven background inventory refresh.
- Added durable stage recovery with renamed-window support and a floating auto-hide dock with persistent, removable pins.
- Added controlled borderless/tool/hidden/unresponsive application compatibility checks.
- Added self-contained package creation, SHA256 integrity checks, an optional signing hook, versioned per-user install/update/rollback, stable data and owned removal.
- [Verification](docs/verification-v0.5.0.md) · [Distribution](docs/distribution.md)

## v0.4.0 — FlowWeb

- Added an optional Manifest V3 extension, authenticated native bridge, and FlowWeb desktop tab.
- Added activate/mute/pin controls and explicitly saved browser URL workspaces.
- Added native-host setup/removal scripts and isolated browser/UI tests.
- Added failure-path handling for unsupported notification and recovery schemas.
- [Verification](docs/verification-v0.4.0.md) · [Setup](docs/flowweb.md)

## v0.3.0 — Notifications and focus

- Added persistent FlowDesk events, opt-in Windows delivery, and validated single-instance notification activation.
- Added timed focus, selected distractions/exclusions, borderless fullscreen, and restoration.
- Added a recovery journal and global recovery shortcut.
- [Verification](docs/verification-v0.3.0.md)

## v0.2.0 — Workspaces and basic desktop panels

- Added named window groups and basic Stage Manager switching/restoration.
- Added searchable overview and running-app window cycling panels.
- [Verification](docs/verification-v0.2.0.md)

## v0.1.1 — Restore preview and undo

- Added restore planning, identity rechecks, and one-step layout undo.
- [Verification](docs/verification-v0.1.1.md)

## v0.1.0 — Initial native prototype

- Added modular C#/.NET and WinUI projects, native inventory/positioning, column splits, saved layout restoration, and a basic tray entry point.
- [Verification](docs/verification-v0.1.0.md)
