# Verification — v0.2.0

[Documentation](README.md) · [All verification reports](verification.md) · [Current roadmap](roadmap.md)

> Historical verification snapshot. Results and validation gaps below apply to this version; use the current roadmap for project-wide status.

Date: 2026-10-27. Windows 11 x64 build 26200. Project-local .NET SDK 10.0.403, WinUI 3 / Windows App SDK 1.8.260921001.

## Passed

- Solution build: zero warnings and errors.
- 34 assertions passed: earlier layout/restore checks, named-library replacement and persistence, case-insensitive duplicate name rejection, blank-name/schema rejection, immutable removal, and native stage behavior.
- Temporary windows in separate processes verified first-group presentation, inactive-group minimization, unchanged unrelated windows, no disruption for an unavailable group, shared members remaining visible, original WINDOWPLACEMENT restoration, and initially minimized/maximized state restoration.
- Native activation restored a minimized test window. Windows accepted the foreground request in this run; acceptance is policy dependent.
- Native split, undo and preview/apply restore rectangles were read back from Windows; a closed window was skipped when applying a stale preview.
- v0.2.0 launched. FlowControl, workspace and overview tabs rendered; tab navigation was observed. Final inventory excluded helper/tool overlays. The updated final FlowControl shell was inspected after the last build.

## Not established

Search filtering, dock cycling, saving/removing groups, stage switching and close-time restoration have not all been exercised end to end through the UI. The controller/native paths are covered as described above. UI automation did not establish a filtered search result; the final UI session was left to the user when concurrent input was detected. No existing user windows were moved by the integration checks.

Tray double-click, library corruption recovery workflow, multi-monitor/mixed-DPI/topology behavior, restricted/elevated apps, hung applications, packaging, signing, crash recovery and performance under large inventories remain unverified. Operations are synchronous and may partially succeed. Stage snapshots and activity history are memory-only.
