<div align="center">

# FlowDesk

**A native workspace companion for Windows 11**

Arrange windows · Switch workspaces · Stay focused · Connect your browser

**v0.4.0 · Development prototype · C# / .NET 10 · WinUI 3 · Windows x64**

[Get started](docs/getting-started.md) · [Documentation](docs/README.md) · [Phase roadmap](docs/roadmap.md) · [Verification](docs/verification.md)

</div>

---

## Your desktop, organized

FlowDesk brings window layouts, named workspaces, focus sessions, notifications, and optional browser controls into one native desktop app. Its FlowControl window is the starting point; additional tabs expose each feature.

> **Project status:** Phases 0–7 have implementations, with basic scope in phases 2–4. The full product vision is unfinished. Advanced visual experiences and Phase 8 distribution work remain pending; this is not a production-qualified release.

## What you can use today

| Experience | Available behavior |
| --- | --- |
| **FlowControl** | Detect windows, arrange 1–4 columns, preview saved layouts, restore matching windows, and undo one layout operation. |
| **Workspaces & Stage** | Save named window groups, restore their layouts, switch basic stages, and minimize inactive group members. |
| **Overview & FlowDock** | Search window titles or app names, activate a window, and cycle through a running app's windows. |
| **Focus & fullscreen** | Run timed focus sessions, minimize selected distractions, use borderless fullscreen, and restore managed windows. |
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
| [2](docs/phases/phase-2.md) | Workspaces and Stage Manager | Basic implementation |
| [3](docs/phases/phase-3.md) | Mission Control | Basic searchable overview |
| [4](docs/phases/phase-4.md) | FlowDock | Basic running-app panel |
| [5](docs/phases/phase-5.md) | FlowDesk notification center | Implemented |
| [6](docs/phases/phase-6.md) | Fullscreen and focus | Implemented |
| [7](docs/phases/phase-7.md) | Optional FlowWeb integration | Implemented; branded-browser validation pending |
| [8](docs/phases/phase-8.md) | Packaging and distribution | Not implemented |

Live thumbnails, richer stage visuals, a floating/pinned dock, broader monitor compatibility, signing, an installer, and updates remain future work. See the [roadmap](docs/roadmap.md) for release gaps.

## Tested, with clear boundaries

The last application verification was on Windows 11 x64 build 26200:

- Build completed with **zero warnings and errors**.
- **93** controller/native assertions and **8** extension/native-host tests passed.
- **8** isolated-browser checks and **8** FlowWeb UI workflows passed.
- **23** earlier desktop workflows and notification activation checks passed again.

Browser end-to-end testing used isolated Chromium. Personal Chrome/Edge profiles, actual Edge UI behavior, mixed-DPI and broader hardware compatibility still need validation. These are recorded results from v0.4.0, not a CI badge or a guarantee for every machine. Read the [verification guide](docs/verification.md) and [known limitations](docs/limitations.md).

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

Saved layouts, window metadata, notification history, recovery journals, and explicitly saved browser URLs live locally beside the application in `data\`. Saved browser URLs are plaintext. Live browser snapshots stay in memory. Runtime data, pairing tokens, caches, and browser test profiles are excluded from Git.

The app registers its own per-user notification identity/protocol. The optional FlowWeb installer registers its own per-user browser native host. Read [architecture and data](docs/architecture.md) before moving build output or removing an installation.

---

[Documentation hub](docs/README.md) · [Release history](CHANGELOG.md) · [Phase-by-phase roadmap](docs/roadmap.md)
