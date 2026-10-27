# Phase roadmap

[Documentation](README.md) · [Verification](verification.md) · [Limitations](limitations.md)

Current application version: **v0.4.0**. Phases 0–7 have implementations, but the overall product is not finished. Phases 2–4 deliver basic experiences, and Phase 8 has not been implemented.

## Implementation matrix

| Phase | Delivered development scope | Status | Guide |
| :---: | --- | --- | --- |
| 0 | Windows audit, local SDK and caches | Complete on audited machine | [Environment](phases/phase-0.md) |
| 1 | Window detection/positioning, splits, save/restore, preview and undo | Prototype implemented | [FlowControl](phases/phase-1.md) |
| 2 | Named groups, restore preview, stage switching and inactive-group minimization | Basic implementation | [Workspaces & Stage](phases/phase-2.md) |
| 3 | Search windows by title/app and request activation | Basic implementation | [Mission Control](phases/phase-3.md) |
| 4 | Group running windows by app and cycle through them | Basic implementation | [FlowDock](phases/phase-4.md) |
| 5 | Persistent FlowDesk cards, unread state, opt-in banners and validated activation | Implemented within documented scope | [Notifications](phases/phase-5.md) |
| 6 | Timed focus, borderless fullscreen, selected distractions, exclusions and recovery | Implemented within documented scope | [Focus & fullscreen](phases/phase-6.md) |
| 7 | Opt-in extension, native bridge, tab controls and browser workspaces | Implemented; personal Chrome/Edge validation pending | [FlowWeb](phases/phase-7.md) |
| 8 | Installer, signing, startup opt-in, updates and release qualification | Not implemented | [Distribution](phases/phase-8.md) |

## Remaining product work

1. Complete the visual stage experience and durable stage recovery.
2. Add supported live thumbnails and monitor organization to the overview.
3. Build the optional floating FlowDock, auto-hide behavior, and explicit app pins.
4. Improve inventory refresh and responsiveness around potentially blocking native calls.
5. Extend real-app, mixed-DPI, multi-monitor, topology-change, accessibility, and performance coverage.
6. Validate FlowWeb in actual Chrome/Edge profiles and managed-browser environments.
7. Complete the Phase 8 packaging and release lifecycle.

The original all-in-one Control Center vision also includes broader Windows system controls. Current FlowControl offers a Settings entry point; comprehensive system controls are not implemented.

## Scope boundaries

Notifications cover FlowDesk events. Cross-app notification aggregation would require separate design and permission review. Focus manages selected desktop windows and FlowDesk banner delivery; global Windows Do Not Disturb/privacy settings are outside the current scope.

No completion percentage is assigned: basic prototypes, tested feature scopes, and release qualification are different kinds of work. Use the phase guides and [versioned evidence](verification.md) to assess readiness.
