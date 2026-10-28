# Verification — v0.6.0

[Documentation](README.md) · [All reports](verification.md) · [Redesign](glass-redesign.md) · [Compatibility](compatibility.md)

Date: **2026-10-28**. Actual Windows 11 x64 build 26200; local .NET SDK 10.0.403, Windows App SDK 1.8.260921001. Browser checks use Playwright 1.64.1 with isolated Chromium. These are local observations, not hosted CI or universal certification.

## Recorded results

| Suite | Result |
| --- | --- |
| Solution and final app build | Passed; zero warnings and errors |
| Controller/native assertions | 121 passed |
| Advanced WinUI workflows | 34 passed |
| Desktop workflows | 23 passed |
| Extension/native-host tests | 8 passed |
| Isolated-browser integration | 8 passed |
| FlowWeb WinUI workflows | 8 passed |
| Distribution fixture checks | 14 passed |
| Notification activation | Cold/warm launch, single instance, injected flags rejected; probes cleaned up |
| Actual Release ZIP | Install, WinUI resources, window launch, stable data, clean close, integration cleanup and data-preserving removal passed |
| Existing local installation | Actual v0.5.0 → v0.6.0 upgrade and launch passed; stable data and prior payload retained |

## New staging and motion evidence

Native tests add negative-origin/reserved-area layout geometry, unchanged source rectangles, empty geometry, session matching after title changes, and switching back to marked members. Existing shared/unrelated member, original show-state, durable-journal and interrupted-process checks remain included.

The actual WinUI suite invokes controls through automation peers. It checks all eight loaded pages, live overview previews, display movement/undo, dock pin management and idle auto-hide. New checks create two workspace groups, open the separate strip, register a real DWM thumbnail in that strip's destination HWND, verify reserved layout space, reject overlapping requests, switch via a strip tile, minimize inactive members, and restore both original layouts and visibility on leaving.

Appearance checks cover light/dark/system persistence, reduced-motion preference persistence and bypass, and completion without a Composition error. The native dock test explicitly returns focus to the main app before waiting for idle collapse; focus retention correctly keeps a dock open.

## Regression findings

An early launch exposed an invalid Composition Translation operation before translation was enabled. The initialization order was corrected; rebuilt launches and motion checks passed. The browser run exposed a partial read of its test coordination report. That report now writes through a temporary file and atomic replacement; the rerun passed. Production bridge configuration already used atomic replacement.

The desktop suite reran the real one-minute focus timer, actual Windows notification submission/history/removal, unread workflows, native fullscreen and interrupted focus recovery. Activation reran cold/warm protocol routing and injected-flag rejection. A physical banner click was not automated.

The browser suite used actual isolated Chromium, the extension, native host and desktop controls. It covered offline feedback, opt-in sharing, mute/pin/activate, confirmed new-window URL restore and disconnect cleanup. Temporary Chromium host registration was removed. Personal Chrome/Edge profiles were not used.

## Visual evidence and limits

A new private Figma file contains editable foundations, eight light and eight dark page frames, plus two linked staging states. The foundations screenshot/material context was retrieved. Further full-screen context and screenshots were blocked by the Figma Starter tool quota; full implementation parity is not established.

Windows Computer Use could enumerate the rebuilt app but rejected its returned window with an inconsistent ownership message: “window id … no longer belongs to …; current owner is …”, reporting the same canonical app on both sides. Fresh selection and an ID-only retry initially failed, so input stopped. After installing v0.6.0, fresh discovery returned the new installation and capture succeeded. The installed dark FlowControl and empty Workspaces pages were inspected. The helper then detected user input, and automation stopped. No unsupported capture/input fallback was used. These two views do not establish a complete all-page visual review.

Physical high-contrast/reduced-transparency changes, assistive technologies, multiple mixed-DPI monitors, large libraries, prolonged use, branded browsers and clean machines remain unqualified. Other applications use native positioning; FlowDesk surface animation does not provide macOS compositor-level window animation.

## Distribution boundaries

Fixture checks exercise install/import/update/rollback/removal and validation failures. The actual Release ZIP was installed into an isolated owned folder, launched, closed cleanly, cleaned up its native registration, and removed while preserving data. A separate actual v0.5.0 → v0.6.0 upgrade updated the existing owned installation and launched with stable data; actual product rollback was not exercised. The package remains unsigned; authorized signing credentials and trusted-release/store qualification are outstanding. Runtime logs, tokens, profiles and caches remain ignored and are not published.

Release archive SHA256: `B4AA666F1280919E5986093BE3A822F99AD8A5401DFAAC672F2CD5048D71C255`. Size: 142,012,872 bytes. This checksum verifies integrity against the recorded value, not publisher identity.
