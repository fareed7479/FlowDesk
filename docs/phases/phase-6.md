# Phase 6 · Fullscreen and focus

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Implemented within selected-window session scope, introduced in v0.3.0.

## Delivered scope

Run focus timers of 1–240 whole minutes with a live countdown/progress card; optionally apply borderless fullscreen; minimize explicitly selected distractions; persist executable-path exclusions; quiet FlowDesk banners. Support standalone target fullscreen and FlowDesk's own native fullscreen presenter. Journal managed changes for restart recovery.

## Use the feature

1. Leave Stage, then open Focus & fullscreen and refresh windows.
2. Choose a target, distractions, exclusions, and focus duration.
3. Start focus, optionally with borderless fullscreen, or start standalone fullscreen.
4. End session or press **Ctrl+Alt+F10** to restore managed windows.

A timer expiry or closed target ends the session. Closing FlowDesk attempts restoration. A shortcut conflict blocks new external sessions.

## Verification evidence

[v0.3.0 verification](../verification-v0.3.0.md) records a real one-minute timer, actual WinUI controls, native placement/frame restoration, close/target-exit behavior, restart recovery, and a physical global-shortcut test. The 23 desktop workflows passed again on [v0.5.0](../verification-v0.5.0.md).

## Remaining work and boundaries

Borderless fullscreen does not provide exclusive game fullscreen. Focus does not suppress all other apps or alter Windows Do Not Disturb. App permissions, custom frames, DPI/topology changes, and unresponsive apps need broader coverage. Recovery skips identity-changed/closed windows; failed restores remain for retry and corrupt journals require attention.

## Implementation map

`DesktopModes.cs`, `ModesView.cs`, `ManagedWindows.cs`, and `RecoveryHotkey.cs`.

---

[← Phase 5](phase-5.md) · [All phases](../roadmap.md) · [Phase 7 →](phase-7.md)
