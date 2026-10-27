# Phase 2 · Workspaces and Stage Manager

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Advanced scope implemented in v0.5.0.

## Delivered scope

Named groups support save/replace/remove, preview/restore, and stage switching. Live preview cards show matched group windows. Inactive managed members minimize; shared members stay visible and unrelated windows are untouched. Original managed states are written to a durable journal before mutation.

## Use the feature

Open Workspaces & Stage. Select current non-minimized windows and save a unique group. Select a group to see live previews, then choose Preview / Apply restore or Stage group. Switch to another group or Leave Stage. Leave Stage before entering focus/fullscreen.

## Verification evidence

Native checks cover inactive/shared/unrelated windows, unavailable groups, original show states, and separate-process interrupted-stage recovery after a title change. Actual WinUI checks cover saving a group, preview/staging, journal creation, and Leave Stage. See the [current verification report](../verification-v0.5.0.md).

## Remaining work and boundaries

Saved-layout matching still uses executable path/title and cannot reopen applications. Recovery uses a marker plus process identity/start time, skips missing windows, and preserves corrupt journals for manual attention. Real monitor/app compatibility needs broader qualification.

## Implementation map

`WorkspaceView.cs`, `StageManager.cs`, `ManagedWindows.cs`.

---

[← Phase 1](phase-1.md) · [All phases](../roadmap.md) · [Phase 3 →](phase-3.md)
