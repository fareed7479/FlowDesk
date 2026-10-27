# Phase 2 · Workspaces and Stage Manager

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Basic implementation, introduced in v0.2.0.

## Delivered scope

Save, replace, and remove named groups of selected windows; preview/apply a group layout; stage a group and minimize inactive previously staged members. Shared windows remain visible, and unrelated windows are left untouched. Leave Stage restores original managed placement.

## Use the feature

1. Open Workspaces & Stage and refresh windows.
2. Select current, non-minimized windows, enter a unique name, and choose Save new.
3. Select a saved group to Preview / Apply restore or Stage group.
4. Stage another group to switch; use Leave Stage before entering focus/fullscreen.

Named groups persist in `data/library.json`. Removing a group does not close its applications.

## Verification evidence

[v0.2.0 verification](../verification-v0.2.0.md) records native checks for inactive/shared/unrelated windows, unavailable groups, and placement restoration, including minimized/maximized states. All UI workflows were not established end to end.

## Remaining work and boundaries

Richer visual grouping, animated stage previews, and durable stage crash recovery remain unfinished. Stage snapshots are session-only; saved desktop layouts still depend on existing window identity/title matches. Normal close attempts restoration and can cancel closing when work remains.

## Implementation map

`WorkspaceLibrary.cs`, `WorkspaceView.cs`, and `StageManager.cs`.

---

[← Phase 1](phase-1.md) · [All phases](../roadmap.md) · [Phase 3 →](phase-3.md)
