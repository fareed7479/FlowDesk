# Glass redesign · v0.6.0

[Documentation](README.md) · [Verification](verification-v0.6.0.md) · [Phase 2](phases/phase-2.md)

The redesign prioritizes the stage strip, previews and transitions. It introduces a shared native material and motion foundation across the eight pages. This is an implementation milestone, not a claim of complete Apple-style behavior or full Figma parity.

## Design source and tools

A new private FlowDesk Figma file contains an editable foundations board, semantic color variables, local button/stage/notification components, eight light and eight dark page frames, and two desktop staging states with linked Research/Writing transitions. Separate light/dark collections accommodate the account's one-mode-per-collection limit. The file link is delivered privately in the project chat rather than published with account details.

Figma generation, design-to-code, motion and WinUI development skills were installed and consulted. Existing project-local .NET/Windows App SDK tools build the native app. Microsoft Composition and DWM provide runtime animation and previews; no additional AI model, animation runtime or Apple asset package is required. Figma uses Inter; Windows uses native Segoe typography and controls.

Only the foundations material sample was retrieved through full design context. Further design-context reads and full-screen screenshots were blocked by the Figma Starter tool quota. The native implementation follows the user brief, existing app workflows and the retrieved material. Full-frame matching remains pending. Figma screenshots are diagnostic references, not application assets.

## Shared visual system

| Layer | Native implementation |
| --- | --- |
| Canvas | Theme-aware gradient over the native Mica shell |
| Surfaces | In-app Acrylic with semantic fallback colors, 24 px corners and a subtle stroke |
| Floating strip/dock | Desktop Acrylic plus a light tint; avoids stacking two blur materials |
| Controls | Native WinUI behavior, 12 px corners, consistent minimum heights and primary action styling |
| Typography | Clear page heading/subtitle hierarchy, readable semantic foregrounds |
| Notifications | One material card per event with title, delivery/read state, message and actions |
| Accessibility | Light/dark/system appearance, solid high-contrast resources, Windows animation preference and explicit reduced-motion setting |

The reference token file is `design/glass.tokens.json`; runtime resources live in `App.xaml`. Windows owns native notification banner appearance. Only FlowDesk's in-app notification center uses its glass cards.

## Staging workflow

1. Open **Workspaces**, create a named group from restored windows, and save it.
2. Select the group to inspect available window previews. **Start stage** moves matching members into the stage area and opens the strip.
3. Select another strip tile to switch. Inactive managed windows minimize; unrelated windows remain untouched. Unavailable groups cannot be selected.
4. **Hide strip** hides its UI while leaving the stage active. **Show stage strip** brings it back.
5. **Leave Stage** restores managed original bounds/show states and closes the strip. The recovery shortcut also leaves an active stage. Journal restoration is attempted after interruption.

The stage area reserves space for the strip when the display is wide enough. Saved windows retain their relative arrangement and are scaled down only when needed. Groups with unavailable or ambiguous saved matches are skipped. During an active session, marked window identity supports document-title changes; this session binding is cleared after leaving.

Switches run away from the UI thread and reject overlapping requests. Stage/focus operations are mutually guarded. Closing while a switch is in progress is refused until the operation finishes. Original states are journaled before mutation; failures are reported rather than described as success.

## Motion and live previews

Page entrances use 220 ms native Composition opacity/translation. Active stage surfaces use 320 ms; 140 ms is the feedback design token. These timings animate FlowDesk surfaces. Other applications' top-level windows are positioned through supported native operations; they are not animated as macOS window layers.

DWM thumbnails use the actual destination window, including the separate strip. They hide during ancestor motion and scrolling, then resume after settling. Protected/minimized windows may provide unavailable or stale content; no screenshot cache is persisted. Reduced motion bypasses FlowDesk entrances and also respects Windows animation/high-contrast settings.

## Qualification still needed

The new native strip and motion behaviors have actual Windows workflow evidence. Initial Windows inspection was blocked by an inconsistent window owner. Installing v0.6.0 restored capture; the dark FlowControl and empty Workspaces pages were observed before detected user input ended automation. The remaining native pages still need visual review, and Figma full-frame comparison remains quota-blocked. High-contrast and reduced-transparency resources exist, but physical setting changes, assistive technology, large libraries, prolonged use and mixed-DPI monitors require manual qualification. This version does not establish pixel-perfect parity, spring physics or compositor-level animation of other applications.

Official implementation guidance: [Microsoft Acrylic](https://learn.microsoft.com/en-us/windows/apps/design/style/acrylic), [XAML animation](https://learn.microsoft.com/en-us/windows/apps/develop/motion/xaml-animation).
