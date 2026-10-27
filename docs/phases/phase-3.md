# Phase 3 · Mission Control

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Advanced scope implemented in v0.5.0.

## Delivered scope

Live DWM thumbnail cards, title/app search, automatic event-driven refresh, display grouping/filtering, window activation, and display move with undo. Preview relationships are released when cards/pages unload.

## Use the feature

Open Overview & FlowDock. Search for an app/title and filter a display. Select a window card, choose a target display, then Move selected to display; Undo display move restores its prior rectangle. Open activates a window. Search narrower if more than 36 results match.

## Verification evidence

The WinUI suite verifies no-result search, native create/destroy event updates, real DWM registrations, selection and display move/undo on controlled windows. Display metadata and DPI are checked natively. See the [current verification report](../verification-v0.5.0.md).

## Remaining work and boundaries

Actual multiple-monitor/mixed-DPI/hot-plug behavior remains unqualified. Minimized/protected previews can fall back. OS virtual-desktop management is not implemented. Windows can decline activation and app sizing can differ.

## Implementation map

`DesktopOverviewView.cs`, `LivePreview.cs`, `LiveThumbnail.cs`, `DesktopDisplay.cs`, `WindowEvents.cs`.

---

[← Phase 2](phase-2.md) · [All phases](../roadmap.md) · [Phase 4 →](phase-4.md)
