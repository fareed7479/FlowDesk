# Phase 7 · Optional FlowWeb integration

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Implemented in v0.4.0 and visually refreshed in v0.5.0; actual personal Chrome/Edge validation remains pending.

## Delivered scope

Add an opt-in Manifest V3 extension, authenticated local native bridge, desktop tab list, activate/mute/pin commands, and explicitly saved URL workspaces. Include native-host installation/removal scripts and isolated-browser verification.

## Use the feature

Follow the [FlowWeb setup and usage guide](../flowweb.md). Build and register the native host, manually load the extension in your browser, enable the desktop bridge, and choose Connect & share tabs in its popup. Sharing starts only after those explicit actions. Saved workspace restore requires confirmation and opens a new browser window.

## Verification evidence

[v0.5.0 verification](../verification-v0.5.0.md) records 115 controller/native assertions, eight extension/host tests, eight actual isolated-browser checks, and eight WinUI FlowWeb workflows. Browser commands traversed the real extension, host process, and desktop UI.

## Remaining work and boundaries

The end-to-end run used isolated Chromium. Personal Chrome/Edge profiles, actual Edge UI behavior, managed policies, and store publication remain unverified/pending. No incognito, page scripting, arbitrary OS commands, or page-state recovery. Saved URLs are plaintext; see the setup guide for exact limits and permissions.

## Implementation map

`FlowDesk.FlowWeb`, `FlowDesk.FlowWebHost`, `FlowWebBridge.cs`, `FlowWebView.cs`, and `extensions/flowweb`.

---

[← Phase 6](phase-6.md) · [All phases](../roadmap.md) · [Phase 8 →](phase-8.md)
