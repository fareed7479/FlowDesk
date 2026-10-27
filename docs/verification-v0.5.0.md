# Verification — v0.5.0

[Documentation](README.md) · [All reports](verification.md) · [Compatibility](compatibility.md) · [Distribution](distribution.md)

Date: **2026-10-27**. Windows 11 x64 build 26200; project-local .NET SDK 10.0.403 and Windows App SDK 1.8.260921001. Browser tests use Playwright 1.64.1 with isolated Chromium.

## Recorded results

| Suite | Result |
| --- | --- |
| Solution build | Passed; zero warnings and errors |
| Controller/native assertions | 115 passed |
| Advanced WinUI workflows | 25 passed |
| Earlier desktop workflows | 23 passed |
| Extension/native-host tests | 8 passed |
| Actual isolated-browser checks | 8 passed |
| FlowWeb WinUI workflows | 8 passed |
| Distribution fixture checks | 14 passed |
| Notification activation | Cold/warm, single instance, injected-flag rejection and cleanup passed |
| Actual published package | Install, WinUI resources, window launch, stable data, clean close, native registration cleanup and data-preserving removal passed |

These are local versioned results, not hosted CI or broad release certification. Later preview-only fixes reran the affected build/advanced UI suites and published-package check; the earlier notification/focus/browser evidence remains applicable to their unchanged paths.

## Advanced desktop evidence

Native stage tests launch a separate process, leave its journal intact, rename a managed fixture's title, and recover through a fresh manager. Checks cover original show state/normal bounds, journal clearing, unsupported-schema preservation, and refusal to mutate when the journal cannot be written.

The UI suite drives actual WinUI controls through automation peers. It checks all eight pages, empty search, automatic create/destroy inventory updates, real DWM thumbnail registration, display selection/move undo, persistent pins and independent pin removal, a real floating dock with idle auto-hide, named workspace creation/staging/Leave Stage, and light/dark/system appearance.

Compatibility fixtures add borderless movement, hidden/tool-window exclusion, and a deliberately blocked message loop. Windows detects the unresponsive fixture, and the move request is rejected promptly. Only controlled fixture windows are moved by these checks.

Manual desktop inspection caught a compact-sidebar branding problem and native preview drift during smooth scrolling. The sidebar adapts its branding; previews now hide during scroll/entrance motion and resume after settling, with clipped cards hidden. The corrected installed build was visually checked while scrolling. The native dock was observed collapsing from its full-height window to an edge strip and revealing again; the original auto-hide preference was restored. Actual multi-monitor/DPI hardware remains outside this run.

## Distribution evidence

Fixture packages check initial install, explicit data migration, exclusion of bridge tokens/transient journals, stable data markers, update, rollback, wrong checksums, unsigned rejection, traversal/undeclared files, unrelated directories and data-preserving uninstall.

A separate suite installs the real self-contained Release ZIP, launches its actual window, checks the stable installed-data marker, closes it cleanly, invokes owned app notification cleanup and removes only owned version payloads. Saved data remains.

This smoke test found that unpackaged publishing omitted generated `FlowDesk.App.pri` and `App.xbf`. The project now explicitly publishes them and the package scripts/checks require them. A successful Debug build alone did not detect the issue.

The package is **unsigned**. An existing authorized certificate is required to exercise the signing branch and trusted-signature acceptance. Actual logon startup, shortcut/update failure injection, clean-machine prerequisites, online updates and store publication are not qualified here. Fixture updates/rollback are not described as an actual two-version product upgrade.

## Earlier functionality retained

The 23 desktop workflows include actual Windows notification submission/history/removal, local unread state, a real one-minute focus timer, exclusions, borderless fullscreen, native FlowDesk presenter entry/exit, target/close restoration and separate-process restart recovery.

Notification activation checks verify cold/warm launch, one-instance routing, strict URI inputs and rejected injected flags. A physical banner click was not automated. The physical recovery shortcut evidence comes from v0.3.0.

Browser checks traverse the real extension, native host and desktop UI using synthetic loopback pages. They cover offline feedback, opt-in sharing, activate/mute/pin, confirmed new-window URL-workspace restore and disconnect cleanup. Personal Chrome/Edge profiles are unchanged; actual Edge UI and managed-policy validation remain pending. Temporary Chromium host registration is removed after tests.

## Repeat the checks

See [verification commands](verification.md). Logs, fixture data, browser profiles and runtime tokens stay in ignored `.cache/`; they are not committed. See [limitations](limitations.md) before assuming compatibility with a particular app or machine.
