# Roadmap

## Established foundation

- Phase 0: environment audit; local SDK/runtime and caches on D:.
- Phase 1: WinUI shell/tray, Win32 inventory and positioning, split layouts, workspace save/restore, preview and one-step undo.

## This increment: v0.2.0

- Named workspace library with validation and local persistence.
- Basic Stage Manager group switching, inactive-group minimization, shared members, and session exit restoring original window placement.
- Mission Control title-based searchable window overview.
- FlowDock running-app panel with window cycling.
- In-app activity history and native placement/activation APIs.

## Next increments

1. Finish UI workflow validation and improve responsiveness; add event-driven inventory updates, accessibility checks, multi-monitor/DPI coverage and crash recovery for staged windows.
2. Expand Mission Control with documented DWM thumbnails, visual grouping and monitor organization. Do not promise undocumented virtual-desktop APIs.
3. Expand FlowDock into an optional floating, auto-hide surface with user-selected pins and reliable focus behavior.
4. Add FlowControl system setting integrations through supported APIs; add an app notification service and explicit permission boundaries for any Windows notification access. Implement focus/fullscreen behavior with reliable restoration and per-app exclusions.
5. Build the optional Chrome/Edge FlowWeb extension with explicit permissions, versioned messages, validated native messaging and origin restrictions.
6. Packaging, signing, installer/update strategy, startup opt-in, release testing and wider hardware coverage.

Each increment must document actual build, test and manual results. No Windows security/privacy settings or unsupported shell hooks are required for the current prototype.
