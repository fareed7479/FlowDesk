<div align="center">

# FlowDesk

**A native workspace companion for Windows 11**

Live previews · Recoverable workspaces · Floating dock · Focus · Browser controls

**v0.6.0 · Advanced desktop preview · C# / .NET 10 · WinUI 3 · Windows x64**

[Get started](docs/getting-started.md) · [Documentation](docs/README.md) · [Phase roadmap](docs/roadmap.md) · [Verification](docs/verification.md)

</div>

---

## Your desktop, organized

FlowDesk brings window layouts, named workspaces, focus sessions, notifications, and optional browser controls into one native desktop app. Its FlowControl window is the starting point; the sidebar opens each feature.

> **Project status:** This preview includes a shared glass design system, a native stage strip, live previews, recoverable stages, and a floating dock. The new Figma designs extend beyond the implementation; full design parity and visual qualification remain pending. Local install/update/rollback tooling is available. Trusted signing, store publication, clean-machine and broader hardware qualification are still pending.

## What you can use today

| Experience | Available behavior |
| --- | --- |
| **FlowControl** | Automatically refresh windows, arrange 1–4 columns, preview saved layouts, restore matching windows, and undo one layout operation. |
| **Workspaces & Stage** | Save named groups, use a separate live stage strip, reserve stage space, switch groups, and recover original managed placements after interruption. |
| **Overview & FlowDock** | Browse live window cards by display, search/activate/move windows, and use a floating auto-hide dock with persistent pins. |
| **Focus & fullscreen** | Run timed sessions with a countdown, minimize selected distractions, use borderless fullscreen, and restore managed windows. |
| **Notifications** | Review persistent FlowDesk events, manage unread cards, and opt into Windows banners. |
| **FlowWeb** | Opt into live browser tab sharing; activate, mute, pin, and save or reopen tab workspaces. |

## Start on Windows

Clone to a development folder on D: where practical:

```powershell
git clone https://github.com/fareed7479/FlowDesk.git D:\PROJECTS\FlowDesk
Set-Location D:\PROJECTS\FlowDesk
```

Place the Windows x64 .NET SDK **10.0.403** in `.tools\dotnet` before running the scripts. The SDK and build caches are local, ignored files; they are not included in a fresh clone. See [prerequisites and setup](docs/getting-started.md) for the full instructions.

From PowerShell 7:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\build.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

The build script also runs controller/native checks using temporary fixture windows. FlowWeb is optional and has its own [browser setup guide](docs/flowweb.md).

## Phase progress

“Implemented” describes the listed development scope, not complete compatibility or release readiness.

| Phase | Deliverable | Status |
| :---: | --- | --- |
| [0](docs/phases/phase-0.md) | Environment audit and local toolchain | Complete for audited machine |
| [1](docs/phases/phase-1.md) | Window engine and FlowControl | Prototype implemented |
| [2](docs/phases/phase-2.md) | Workspaces and Stage Manager | Native stage strip, live previews and durable recovery |
| [3](docs/phases/phase-3.md) | Mission Control | Live previews and display organization |
| [4](docs/phases/phase-4.md) | FlowDock | Floating dock, saved pins and auto-hide |
| [5](docs/phases/phase-5.md) | FlowDesk notification center | Implemented |
| [6](docs/phases/phase-6.md) | Fullscreen and focus | Implemented |
| [7](docs/phases/phase-7.md) | Optional FlowWeb integration | Implemented; branded-browser validation pending |
| [8](docs/phases/phase-8.md) | Packaging and distribution | Local lifecycle tooling; trusted release pending |

The native interface includes an adaptive sidebar, shared Acrylic glass surfaces, Mica, native Composition entrances, light/dark/system appearance, reduced motion, and high-contrast resources. Read the [glass redesign guide](docs/glass-redesign.md) for the implementation and remaining design work. See [distribution](docs/distribution.md) for the self-contained package, local updates and rollback, and the [roadmap](docs/roadmap.md) for remaining release gates.

## Tested, with clear boundaries

The last application verification was on Windows 11 x64 build 26200:

- Build completed with **zero warnings and errors**.
- **121** controller/native assertions and **8** extension/native-host tests passed.
- **8** isolated-browser checks and **8** FlowWeb UI workflows passed.
- **34** advanced UI workflows and **14** fixture distribution checks passed.
- **23** earlier desktop workflows and notification activation checks passed again.

Browser end-to-end testing used isolated Chromium. Personal Chrome/Edge profiles, actual Edge UI behavior, mixed-DPI and broader hardware compatibility still need validation. These are local results, not a CI badge or a guarantee for every machine. Full Figma comparison is quota-blocked; native visual review covers only the installed dark FlowControl and empty Workspaces views. See the [compatibility matrix](docs/compatibility.md). Read the [verification guide](docs/verification.md) and [known limitations](docs/limitations.md).

## Explore the repository

| Location | Purpose |
| --- | --- |
| `src/FlowDesk.Core` | Layouts, saved-workspace models, and notification state |
| `src/FlowDesk.Windows` | Native window operations, stages, focus/recovery, and browser bridge |
| `src/FlowDesk.App` | WinUI shell, controls, tray, and Windows integrations |
| `src/FlowDesk.FlowWeb` | Shared browser protocol and workspace library |
| `src/FlowDesk.FlowWebHost` | Browser native messaging executable |
| `extensions/flowweb` | Optional Manifest V3 extension |
| `tests` · `scripts` · `docs` | Verification, developer workflows, and project guides |

## Data and local integration

Saved layouts, window metadata, notification history, recovery journals, and explicitly saved browser URLs stay local. Development builds use `data\` beside the application; installed builds use `<InstallRoot>\data`. Saved browser URLs are plaintext. Live browser snapshots stay in memory. Runtime data, pairing tokens, caches, and browser test profiles are excluded from Git.

The app registers its own per-user notification identity/protocol. The optional FlowWeb installer registers its own per-user browser native host. Read [architecture and data](docs/architecture.md) before moving build output or removing an installation.

---

[Documentation hub](docs/README.md) · [Release history](CHANGELOG.md) · [Install & update](docs/distribution.md) · [Phase-by-phase roadmap](docs/roadmap.md)
