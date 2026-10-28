# Verification guide

[Documentation](README.md) · [Known limitations](limitations.md)

## Latest recorded results

Application version **v0.6.0**, tested on **2026-10-28** on Windows 11 x64 build 26200 with .NET SDK 10.0.403.

| Check | Recorded outcome |
| --- | --- |
| Solution build | Passed; zero warnings and errors |
| Controller/native assertions | 121 passed |
| Advanced WinUI workflows | 34 passed |
| Fixture distribution workflows | 14 passed |
| Actual published package | Installation, launch, stable data, cleanup and removal passed |
| Extension/native-host tests | 8 passed |
| Isolated-browser integration | 8 passed |
| FlowWeb WinUI workflows | 8 passed |
| Earlier desktop workflows | 23 passed again |
| Notification activation | Cold/warm launch, single instance, and injected-flag rejection passed |

The physical recovery shortcut was tested in v0.3.0. These are local, versioned observations; this repository does not advertise an automated CI result.

## Repeat the suites

Run from the repository root in PowerShell 7 after [setting up the SDK](getting-started.md). UI suites require an unlocked interactive Windows desktop and FlowDesk closed. Allow the suites to finish without concurrent interaction.

```powershell
# Build and run controller/native checks, including temporary fixture windows.
pwsh -ExecutionPolicy Bypass -File .\scripts\build.ps1

# Actual WinUI controls, notifications, focus timer, and recovery.
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-advanced.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-desktop.ps1

# Windows notification protocol activation and single-instance behavior.
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-activation.ps1
```

The desktop suite includes a real one-minute focus timer. The activation suite temporarily sends generic Windows notifications, checks the app's registered protocol, removes probes, and restores previous notification preferences.

For optional browser testing, install Node.js first, then:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\setup-flowweb-tests.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-flowweb.ps1
```

This downloads pinned Playwright 1.64.1 and its isolated browser under the repository. Tests use synthetic loopback pages and an isolated browser profile, plus per-user native-host registration. See [FlowWeb testing](flowweb.md#repeatable-tests) for setup and cleanup boundaries.

Local reports and logs are in ignored `.cache\`. Generated browser profiles, tokens, user data, binaries, and full local logs are not published as test artifacts.

## Distribution checks

After packaging, use the fixture and actual-product suites:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-distribution.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-real-package.ps1
```

The latter checks the published ZIP rather than a development build. See [distribution](distribution.md) for signing boundaries and local update/removal.

## Historical evidence

Each report is a snapshot of that version; later features can supersede earlier “not implemented” notes without changing what was actually tested.

| Version | Evidence |
| --- | --- |
| v0.1.0 | [Initial build and native layout checks](verification-v0.1.0.md) |
| v0.1.1 | [Preview and undo](verification-v0.1.1.md) |
| v0.2.0 | [Named workspaces, stages, and basic panels](verification-v0.2.0.md) |
| v0.3.0 | [Notifications, focus/fullscreen, activation, and recovery](verification-v0.3.0.md) |
| v0.4.0 | [Browser bridge and full regression run](verification-v0.4.0.md) |
| v0.5.0 | [Advanced desktop, compatibility fixtures and actual package lifecycle](verification-v0.5.0.md) |
| v0.6.0 | [Glass, stage strip, motion and regression evidence](verification-v0.6.0.md) |

## What these results do not establish

The suites use temporary fixture windows instead of moving existing user windows. They do not establish universal behavior across applications, monitors, DPI settings, Windows builds, or enterprise policies. The browser end-to-end run uses isolated Chromium; actual Edge and personal-profile installation remain manual. Notification payload/protocol testing does not substitute for an observed physical banner click.

The new overview/dock and stage UI workflows are covered within the fixture scope described in the v0.6.0 report. Remaining visual/app/hardware coverage is limited. See individual phase pages and [limitations](limitations.md) before treating a component as release-ready.
