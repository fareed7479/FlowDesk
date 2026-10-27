# Verification — v0.1.1

[Documentation](README.md) · [All verification reports](verification.md) · [Current roadmap](roadmap.md)

> Historical verification snapshot. Results and validation gaps below apply to this version; use the current roadmap for project-wide status.

2026-10-27, Windows 11 x64 build 26200, project-local .NET SDK 10.0.403.

Build: passed with zero warnings/errors. All 18 assertion checks passed. Two separate temporary WinForms processes provided controlled test windows. Native rectangles were read back from the OS after split, undo and preview/apply restore. A closed window was skipped when applying a stale preview. No existing user windows were moved by this integration test.

UI: v0.1.1 launched and was visually inspected, including the window list, two rows of controls, restore preview area, and scrollable content. UI button workflows and tray activation are not covered end to end; automated verification exercises their underlying controller. The attempted empty-undo UI click did not produce an observable status change and is not counted as verified.

## Behavior and remaining limits

- Preview reports targets without moving windows; applying rechecks handle, PID, executable path and title. Duplicate saved entries cannot schedule the same window twice.
- Undo stores live rectangles immediately before successful move requests. Failed requests do not replace undo history; failed undo attempts remain available for retry. Closed or identity-changed windows are skipped. Undo history is memory-only and one level deep.
- Requests may partially succeed. Production applications can enforce sizes or ignore requests; counts indicate successful API requests, not universal visual correctness.
- Only rectangles are restored. Minimized/maximized state, changed document titles, cross-monitor topology, mixed DPI, protected apps, shell integration, packaging and browser tabs remain outside verified scope.
- Stage Manager grouping, Mission Control, FlowDock, notification integration, focus/fullscreen and FlowWeb remain future increments.
