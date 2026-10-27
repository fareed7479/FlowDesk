# Phase 3 · Mission Control

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Basic in-app searchable overview, introduced in v0.2.0.

## Delivered scope

Display the current eligible window inventory, filter by window title or app name, and request activation of a selected window. The panel shares a tab with FlowDock.

## Use the feature

Open Overview & FlowDock, choose Refresh overview & dock, enter a search term, and select a result. Windows can decline foreground activation; use the taskbar if the request is rejected.

## Verification evidence

[v0.2.0 verification](../verification-v0.2.0.md) records rendered overview/tab navigation and native activation of a minimized fixture. It does not establish a successful filtered-result UI workflow. Later builds retain the panel without claiming broader visual validation.

## Remaining work and boundaries

Live DWM thumbnails, monitor organization, virtual-desktop management, and comprehensive UI/accessibility verification remain pending. Refresh is explicit; this is a list-based overview rather than the full visual Mission Control experience.

## Implementation map

`DesktopOverviewView.cs` and native window activation services.

---

[← Phase 2](phase-2.md) · [All phases](../roadmap.md) · [Phase 4 →](phase-4.md)
