# Architecture and local data

[Documentation](README.md) · [Phase roadmap](roadmap.md)

## Solution structure

| Project | Responsibility |
| --- | --- |
| `FlowDesk.Core` | Geometry, 1–4 column layouts, saved-window matching, restore plans, workspace libraries, and notification state |
| `FlowDesk.Windows` | Win32 inventory/placement/activation, durable stage state, DWM previews, display inventory, window events, focus/fullscreen sessions, recovery, and current-user browser pipe server |
| `FlowDesk.App` | WinUI shell and feature views, tray entry point, global recovery shortcut, native notifications, and single-instance activation |
| `FlowDesk.FlowWeb` | Versioned bounded message protocol, command/URL validation, and browser workspace persistence |
| `FlowDesk.FlowWebHost` | Native messaging process linking a browser connection to the desktop bridge |
| `extensions/flowweb` | Manifest V3 service worker and explicit connection popup |

Desktop views call controllers and native services. Saved desktop workspaces match already-running windows by executable path and title; restoration does not launch missing applications. Native operations can partially succeed and Windows may decline foreground activation.

FlowWeb traffic follows this path:

```text
Browser extension
    ↕ bounded native messaging frames
FlowDesk.FlowWebHost
    ↕ authenticated current-user named pipe
FlowDesk.Windows bridge
    ↕ commands and live snapshots
FlowDesk.App FlowWeb view
```

The host validates the pinned extension origin. The desktop bridge validates a local pairing token and isolates connections so overlapping browser tab IDs do not cross sessions. This is a local integration, not a cloud sync service.

## Persistence

Development data is under `data\` beside the desktop executable. Installed builds resolve their stable `<InstallRoot>\data` location from `flowdesk-install.json`.

| File | Contents / lifetime |
| --- | --- |
| `workspace.json` | Single saved desktop layout |
| `library.json` | Named desktop window groups |
| `notifications.json` | FlowDesk event history and preferences |
| `stage-recovery.json` | Original managed stage placements for interrupted-session restoration |
| `dock.json` | Saved app pins, auto-hide and selected display |
| `appearance.json` | Light/dark/system appearance |
| `mode-recovery.json` | Restoration journal for managed focus/fullscreen windows |
| `flowweb-workspaces.json` | Explicitly saved browser workspace URLs, in plaintext |
| `flowweb-bridge.json` | Local bridge configuration and pairing token |

Focus exclusions are stored in notification preferences in `notifications.json`. Stage and focus/fullscreen snapshots have durable journals. One-step layout undo, activity history, and live browser snapshots remain session memory.

Window metadata can include document titles and executable paths. Explicitly saved URLs can contain sensitive query strings even though embedded URL credentials are rejected. Data is not encrypted by this prototype. Runtime data and tokens are excluded from Git; avoid adding them to issues or commits.

Unreadable libraries and unsupported notification/recovery schemas are preserved instead of silently overwritten. A bad recovery journal blocks new mode mutations until addressed. Back up the entire data directory before manual repair or output cleanup.

## Local Windows integration

- The app registers its own per-user notification identity and `flowdesk-control` URI scheme. It refuses to take over an unrelated protocol owner.
- `Ctrl+Alt+F10` is the recovery shortcut. Conflicts block new external focus/fullscreen sessions.
- The optional installer registers `com.flowdesk.flowweb` for selected browsers under the current user's native host keys. It uses an ownership marker and refuses unrelated registrations.
- Generated host manifests live in ignored `.flowweb-native\`; host settings live beside the native host executable. Paths depend on the build location, so reinstall the host after moving the repository/output.
- The FlowWeb removal script removes matching host registrations and retains saved browser workspaces. The [distribution scripts](distribution.md) handle versioned application installation, rollback, owned notification cleanup and data-preserving removal.

## Development storage

`.tools\` holds the local SDK and optional browser tools; `.cache\` holds restored packages, test reports, and isolated browser profiles. These remain on the clone's drive where practical. No runtime files, private browser data, or generated tokens belong in source control.

See [limitations](limitations.md) for native compatibility and the [distribution plan](phases/phase-8.md) for the remaining lifecycle work.
