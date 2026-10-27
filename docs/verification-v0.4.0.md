# Verification — v0.4.0 / Phase 7

[Documentation](README.md) · [All verification reports](verification.md) · [Current roadmap](roadmap.md)

> Historical verification snapshot. Results and validation gaps below apply to this version; use the current roadmap for project-wide status.

Date: 2026-10-27. Windows 11 x64 build 26200; .NET SDK 10.0.403. Playwright 1.64.1; isolated Chrome for Testing 156.0.8078.4 (Chromium engine).

## Evidence

- Solution build: zero warnings and errors.
- 93 controller/native assertions passed, including previous functionality and FlowWeb framing, version/origin/command/URL validation, workspace persistence and size limits, pairing-token rejection, authenticated snapshot/command/result roundtrip, Chrome/Edge session isolation with overlapping tab IDs, disconnect cleanup and token rotation.
- Eight extension/native-host tests passed. These include manifest permission scope, URL/command limits, real host rejection of foreign origins, unsupported versions, oversized frames and unexpected switches.
- Eight real-browser checks and eight WinUI FlowWeb workflow checks passed. An isolated profile loaded the pinned extension, showed offline-host feedback, connected through actual browser native messaging to FlowDesk, shared two synthetic HTTP tabs, applied desktop activate/mute/pin commands, saved selected URLs, confirmed restore into a separate browser window and cleared metadata on disconnect. Test processes stopped cleanly.
- All 23 earlier notification/fullscreen/focus desktop workflows passed again on v0.4.0, including the real timer, process-restart recovery and close restoration. Cold/warm notification activation, single-instance redirection and rejection of injected flags also passed.
- The temporary Chromium-only native host registration was removed after verification. Chrome/Edge registrations use the normal app configuration. Personal browser profiles were not modified.
- Unsupported notification/recovery data schemas found during failure-path review now preserve their files and report failure instead of escaping startup. Regression assertions cover both cases.

## Boundaries

The end-to-end browser run uses isolated Chromium, not personal Chrome or Edge profiles. The extension uses their shared Manifest V3/native messaging APIs and both native host registry locations are configured, but personal-profile loading, actual Edge UI validation, store publication and managed browser policy scenarios remain manual/future work.

The browser workspace feature restores HTTP/HTTPS URLs in a new window. It does not restore login state, browser history, form data or page scroll position. Incognito is disabled. Live tab metadata is memory-only; explicitly saved URLs are plaintext local data ignored by Git. Page-level scripting/media control and arbitrary OS commands are not supported.

See [FlowWeb](flowweb.md) for setup/removal, permission scope, local data locations and repeatable tests. Existing notification/fullscreen/focus behavior retains its previously documented OS and hardware limits.
