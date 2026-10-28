# FlowDesk documentation

[Repository home](../README.md) · [Roadmap](roadmap.md) · [Release history](../CHANGELOG.md)

FlowDesk v0.6.0 is an advanced desktop preview. This documentation separates implemented scope, observed test results, and unfinished product work.

## Start here

| Guide | What it covers |
| --- | --- |
| [Glass redesign](glass-redesign.md) | Design system, staging workflows, tools and verification boundaries |
| [Getting started](getting-started.md) | Windows prerequisites, local SDK, build, launch, and troubleshooting |
| [Architecture and data](architecture.md) | Project responsibilities, native integrations, and local persistence |
| [Verification](verification.md) | Commands, recorded results, and test boundaries |
| [Known limitations](limitations.md) | Compatibility gaps and behavior to understand before use |
| [FlowWeb setup](flowweb.md) | Optional extension installation, permissions, usage, and removal |
| [Roadmap](roadmap.md) | Current scope and unfinished work |
| [Distribution](distribution.md) | Package, install, update, roll back and remove |
| [Compatibility](compatibility.md) | Actual Windows evidence and environments still unqualified |

## Hybrid frontend modules

[Module 2 · WebView2 IPC and DPI bounds](modules/module-2.md) includes the complete native bridge, host initialization and [actual WebView2 verification](modules/module-2-verification.md). This separate module sequence does not replace the phase roadmap or claim that the React frontend is complete.

## Every phase

| Phase | Guide | Current scope |
| :---: | --- | --- |
| 0 | [Environment and toolchain](phases/phase-0.md) | Audited Windows development machine |
| 1 | [Window engine and FlowControl](phases/phase-1.md) | Native prototype with preview and undo |
| 2 | [Workspaces and Stage Manager](phases/phase-2.md) | Native stage strip, live previews, switching and durable recovery |
| 3 | [Mission Control](phases/phase-3.md) | Live cards, event-driven refresh and display organization |
| 4 | [FlowDock](phases/phase-4.md) | Persistent pins, app cycling and floating auto-hide dock |
| 5 | [Notifications](phases/phase-5.md) | FlowDesk event center and opt-in Windows delivery |
| 6 | [Fullscreen and focus](phases/phase-6.md) | Selected-window sessions with recovery |
| 7 | [FlowWeb](phases/phase-7.md) | Optional local browser integration |
| 8 | [Distribution](phases/phase-8.md) | Local lifecycle tooling; trusted release pending |

Each phase guide records what exists, how to use or inspect it, verification evidence, and remaining work. Historical reports are indexed in the [verification guide](verification.md); their statements apply to the version tested.
