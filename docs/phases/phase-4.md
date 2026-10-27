# Phase 4 · FlowDock

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Basic in-app running-app panel, introduced in v0.2.0.

## Delivered scope

Group current windows by executable path and show one button per running app. Repeated clicks cycle through that app's eligible windows. Each click re-enumerates windows before requesting activation.

## Use the feature

Open Overview & FlowDock, refresh the panel, then click an app button. Click again to advance through its windows. Refresh after applications open or close to update the displayed app groups.

## Verification evidence

[v0.2.0 verification](../verification-v0.2.0.md) records shell/panel rendering and native activation behavior. Dock cycling itself has not been comprehensively exercised end to end through the UI.

## Remaining work and boundaries

A floating dock, auto-hide, user-selected pins, richer icons/previews, monitor placement, and full keyboard/accessibility validation remain unfinished. The panel does not launch closed or pinned applications, replace the Windows taskbar, or bypass Windows activation policies.

## Implementation map

`DesktopOverviewView.cs`.

---

[← Phase 3](phase-3.md) · [All phases](../roadmap.md) · [Phase 5 →](phase-5.md)
