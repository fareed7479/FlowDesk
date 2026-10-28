# Phase 2 · Workspaces and Stage Manager

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Native stage strip and session switching implemented in v0.6.0; full visual and hardware qualification pending.

## Delivered scope

Named groups support save/replace/remove, preview/restore, and stage switching. Live preview cards show matched group windows. Inactive managed members minimize; shared members stay visible and unrelated windows are untouched. Original managed states are written to a durable journal before mutation.

## Use the feature

Open Workspaces and choose New workspace. Select current non-minimized windows and save a unique group. Select the group and choose Start stage. The separate strip offers live previews, active/unavailable states, and one-click switching; the managed layout reserves space beside it when possible. Hide strip keeps the stage active; Leave Stage restores original states. Preview / Apply restore offers an independent restore after leaving Stage. Leave Stage before entering focus/fullscreen. See the [glass workflow guide](../glass-redesign.md).

## Verification evidence

Native checks cover inactive/shared/unrelated windows, unavailable groups, original show states, and separate-process interrupted-stage recovery after a title change. Actual WinUI checks cover two saved groups, the separate strip, its own DWM destination, reserved space, serialized switching, inactive minimization and restoration of both original layouts. See the [current verification report](../verification-v0.6.0.md).

## Remaining work and boundaries

Saved-layout matching still uses executable path/title and cannot reopen applications. Recovery uses a marker plus process identity/start time, skips missing windows, and preserves corrupt journals for manual attention. Real monitor/app compatibility needs broader qualification.

## Implementation map

`WorkspaceView.cs`, `StageStrip.cs`, `StageManager.cs`, `StageLayouts.cs`, `ManagedWindows.cs`.

---

[← Phase 1](phase-1.md) · [All phases](../roadmap.md) · [Phase 3 →](phase-3.md)
