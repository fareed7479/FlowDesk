# Phase 4 · FlowDock

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Advanced scope implemented in v0.5.0.

## Delivered scope

An in-app dock and a separate floating native window show running apps and persistent app pins. Repeated clicks cycle running windows; a closed pinned executable launches only after an explicit click. Optional auto-hide collapses the floating dock to an edge strip.

## v0.6.0 integration

The floating dock follows the saved appearance and uses Desktop Acrylic with a single tint layer. Pinning, cycling and idle auto-hide behavior are retained. See the [glass redesign guide](../glass-redesign.md).

## Use the feature

Pin an app from a window card. Use Show floating dock in Overview & FlowDock, optionally enabling auto-hide. Hover over the edge strip to reveal it; Hide floating dock closes it. Unpin from the app's window card or the saved pin-management list, including closed apps. Choose a display before opening the dock to place it there; the selected device preference persists.

## Verification evidence

Controller checks cover pin validation/deduplication/storage. Actual UI checks cover pinning, the owned floating window, automatic idle collapse, and clean close. Topology-aware positioning exists but physical multi-monitor changes need testing. See the [current verification report](../verification-v0.5.0.md).

## Remaining work and boundaries

Pins are limited to 24 absolute executable paths captured from running apps. The app does not replace the taskbar. Closed-pin launch, cross-monitor dock movement, keyboard focus and long-duration behavior need broader manual coverage. Native collapse/reveal was visually checked on the installed build.

## Implementation map

`DesktopOverviewView.cs`, `FloatingDock.cs`, `DockPreferences.cs`.

---

[← Phase 3](phase-3.md) · [All phases](../roadmap.md) · [Phase 5 →](phase-5.md)
