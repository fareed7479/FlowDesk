# Phase roadmap

[Documentation](README.md) · [Verification](verification.md) · [Compatibility](compatibility.md)

Current version: **v0.6.0**. The preview includes native staging, shared glass surfaces, live previews, durable recovery, and a floating dock. Editable designs cover all eight pages; full implementation parity and visual qualification are still pending. Distribution tooling is available; the full product is still awaiting release qualification.

| Phase | Delivered scope | Status | Guide |
| :---: | --- | --- | --- |
| 0 | Audited Windows machine and local SDK/cache workflow | Established | [Environment](phases/phase-0.md) |
| 1 | Window engine, splits, preview/restore, undo, event-driven background inventory | Implemented prototype | [FlowControl](phases/phase-1.md) |
| 2 | Named groups, separate live stage strip, reserved canvas and durable recovery | Implemented; broader visual/hardware qualification pending | [Workspaces & Stage](phases/phase-2.md) |
| 3 | Live DWM cards, search, display filtering/grouping and display moves/undo | Advanced scope implemented | [Mission Control](phases/phase-3.md) |
| 4 | In-app and floating docks, persistent pins, explicit app launch and auto-hide | Advanced scope implemented | [FlowDock](phases/phase-4.md) |
| 5 | FlowDesk event center, opt-in native delivery and validated activation | Implemented and redesigned | [Notifications](phases/phase-5.md) |
| 6 | Timed focus/countdown, borderless fullscreen, exclusions and recovery | Implemented and redesigned | [Focus](phases/phase-6.md) |
| 7 | Optional extension, authenticated local bridge, tab controls and URL workspaces | Implemented and redesigned; branded-browser validation pending | [FlowWeb](phases/phase-7.md) |
| 8 | Release ZIP, checksums, optional signing hook, per-user install/update/rollback/removal | Local lifecycle implemented; trusted release pending | [Distribution](phases/phase-8.md) |

## Release work still outstanding

- Supply an authorized trusted code-signing certificate and validate signed artifacts/timestamping.
- Qualify prerequisites and install/upgrade/removal on clean Windows machines.
- Test real mixed-DPI/multi-monitor hardware, display hot-plug, protected/elevated apps, accessibility, and large inventories.
- Validate actual Chrome/Edge profiles and managed-browser policies; publish optional extension store listings with reviewed identities.
- Choose any future online update service and signed graphical installer/store distribution.
- Expand visual behaviors only through supported APIs; OS virtual-desktop management and a comprehensive system-settings Control Center are not implemented.

The implementation uses no elevation bypass and does not change Windows Do Not Disturb or global privacy settings. Browser sharing and startup remain explicit opt-ins. See [compatibility evidence](compatibility.md) for the difference between actual tests and unverified environments.
