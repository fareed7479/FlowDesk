# Phase matrix — v0.4.0

Phase numbering follows the feature phases described in the project chat. Completion always refers to the explicitly documented development scope, not release qualification or parity with another operating system.

| Phase | Scope | Status |
| --- | --- | --- |
| 0 | Windows environment audit and local SDK/cache setup | Complete |
| 1 | Native inventory/positioning, splits, workspace save/restore, preview, undo | Prototype complete |
| 2 | Named workspaces and Stage Manager | Basic implementation; advanced visual grouping and durable stage recovery remain |
| 3 | Mission Control | Title-based searchable overview; live thumbnails and monitor organization remain |
| 4 | FlowDock | Running-app panel and cycling; floating/auto-hide dock and user-selected pins remain |
| 5 | FlowDesk notifications | Implemented: persistent center, read/unread/filter/dismiss/clear, optional native banners/sound, delivery states, validated notification links and single-instance activation |
| 6 | Fullscreen/focus behavior | Implemented: borderless target fullscreen, FlowDesk presenter, timed selected-distraction focus, app exclusions, quiet FlowDesk notifications, end/close/target-exit restoration, global recovery shortcut and startup journal recovery |
| 7 | Optional Chrome/Edge FlowWeb | Implemented: opt-in Manifest V3 extension, native messaging bridge, tab controls, browser workspaces, setup/removal scripts and isolated-browser validation |
| 8 | Distribution | Signing, installer, startup opt-in, update strategy and release qualification pending |

Phase 5 covers the application's own notifications. Cross-app notification access would be separate work with explicit Windows permission boundaries. Phase 6 manages selected desktop windows and FlowDesk banner delivery; it does not change system-wide Do Not Disturb or security/privacy settings.

## Further work

Finish earlier visual experiences with documented DWM thumbnails, monitor organization and an optional floating FlowDock. Add event-driven inventory updates and move potentially blocking native calls off the UI thread. Extend mixed-DPI, multi-monitor, protected-app and unresponsive-app compatibility coverage. Keep optional browser integration permission-scoped and validate native messaging/origins. Package and sign only after broader release tests.

Personal Chrome/Edge profile loading and browser-specific manual validation remain separate setup steps; Chrome/Edge store listing is part of distribution.

See verification-v0.4.0.md for actual test evidence and remaining validation boundaries.
