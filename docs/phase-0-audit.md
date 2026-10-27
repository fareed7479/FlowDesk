# Phase 0 audit record

[Phase 0 guide](phases/phase-0.md) · [Current setup](getting-started.md)

Audit date: **2026-10-27**. This is a historical environment record, not the current project status.

## Initial observations

- The selected project directory on D: was empty.
- Windows x64 reported build 26200, release 25H2. The registry product label was inconsistent with the Windows 11 build.
- Git 2.50.0 was available.
- .NET, MSBuild, winapp, and WinGet were not found on PATH; standard Visual Studio/SDK locations were not found.
- D: had sufficient space for project-local tools and caches.
- The audit used runtime/registry observations where system inventory queries were unavailable.
- At this point no app had been built or runtime-tested.

## Subsequent setup and outcome

A project-local .NET SDK 10.0.403 and NuGet cache were established under ignored `.tools\` and `.cache\` directories. The first WinUI/native prototype was then built and tested; see [v0.1.0 verification](verification-v0.1.0.md).

The SDK CLI's initial run reported creating an ASP.NET development certificate; it was not explicitly trusted. No other machine-wide developer settings were intentionally changed by that local setup.

Later authorized repository commits and application phases are recorded in the [release history](../CHANGELOG.md). Initial audit observations do not imply those features or repository permissions remain absent.

## Reproduction boundary

This audit describes one development machine. A new clone does not contain the SDK or caches, and clean-machine provisioning remains unqualified. Follow [getting started](getting-started.md) and Microsoft's [WinUI setup](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here) for preparation.
