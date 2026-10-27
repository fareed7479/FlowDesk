# Phase 1 · Window engine and FlowControl

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Prototype implemented; introduced in v0.1.0 and extended in v0.1.1.

## Delivered scope

Detect eligible visible top-level windows; position selected windows in 1–4 columns; save a single layout; preview and apply restore; undo one successful layout operation. The redesigned WinUI shell includes adaptive navigation, appearance settings, and a basic tray icon. v0.5.0 adds event-driven background inventory refresh.

## Use the feature

1. Open FlowControl and choose Refresh.
2. Select one to four windows and choose Split.
3. Choose Save to capture selected windows.
4. Choose Preview restore, review the matched targets, then Apply restore.
5. Use Undo to reverse the last successful layout operation.

Saved layouts match already-running windows by executable path and title; missing or ambiguous matches are skipped.

## Verification evidence

[v0.1.0](../verification-v0.1.0.md) records ten assertions; [v0.1.1](../verification-v0.1.1.md) records 18, including native split/undo/restore readback and a closed-window stale preview. The latest [v0.5.0 suite](../verification-v0.5.0.md) retains controller regression coverage.

## Remaining work and boundaries

Mixed-DPI and broader monitor/app compatibility need validation. Layout undo is one level and memory-only. Position requests may partially succeed, and this phase does not reopen applications or implement comprehensive Windows setting controls. Tray double-click is not established by the recorded end-to-end suites.

## Implementation map

`FlowDesk.Core` layouts/restore plans, `FlowDesk.Windows` inventory/controller, and `FlowDesk.App/MainWindow.cs`.

---

[← Phase 0](phase-0.md) · [All phases](../roadmap.md) · [Phase 2 →](phase-2.md)
