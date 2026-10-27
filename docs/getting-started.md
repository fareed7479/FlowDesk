# Getting started

[Documentation](README.md) · [Limitations](limitations.md) · [FlowWeb setup](flowweb.md)

## Prerequisites

- Windows 11 x64 with an interactive desktop. The recorded test machine uses build 26200; a broader Windows compatibility matrix is not established.
- Git and PowerShell 7 (`pwsh`).
- Windows x64 .NET SDK 10.0.403, matching `global.json`. The repository allows later patches in that feature band.
- Network access for initial NuGet restoration.
- Node.js only if you want the optional FlowWeb test suite; it is not required to run the desktop app.

The checked-in WinUI project references Windows App SDK 1.8.260921001 and Windows SDK BuildTools 10.0.26100.4948 through NuGet. The existing project has been built with its local SDK workflow. For a fresh Windows development machine, consult Microsoft's [WinUI setup](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here) and [.NET installation guidance](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Fresh-machine provisioning has not been independently qualified.

## Clone and prepare the local SDK

```powershell
git clone https://github.com/fareed7479/FlowDesk.git D:\PROJECTS\FlowDesk
Set-Location D:\PROJECTS\FlowDesk
```

D: is a recommended development location, not a hardcoded runtime requirement.

Download the official Windows x64 .NET SDK 10.0.403 binary archive from Microsoft's [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), check its published checksum, and extract it into `.tools\dotnet` within the clone. The result must include `.tools\dotnet\dotnet.exe`. An SDK installed only on PATH does not satisfy the current scripts: they explicitly select this local executable.

```powershell
.\.tools\dotnet\dotnet.exe --version
```

SDKs, restored packages, and browser testing tools are ignored by Git. Scripts keep the .NET CLI home and NuGet packages under `.cache` in the clone.

## Build and launch

Use PowerShell 7 from the repository root:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\build.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

The build script builds the solution and runs the controller/native checks. Its movement checks create and arrange temporary test windows. The run script starts the Debug Windows x64 build.

Start in FlowControl: refresh the inventory, select one to four windows, and choose Split. Undo reverses one successful layout operation. Open the other tabs for named workspaces, overview/dock, focus, notifications, and optional FlowWeb. Closing the main window exits the app; the tray icon is a basic activation entry point.

## Output and saved data

The desktop output is:

```text
src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/
```

The desktop app includes its .NET and Windows App SDK runtime output. Retain the complete output folder; copying just the executable is insufficient. This is a development build, not a signed installer or a tested portable distribution.

Runtime data lives in `data\` beside the executable. Back it up before cleaning, relocating, or replacing output. Read [architecture and data](architecture.md) for file details and local registrations.

## Troubleshooting

| Symptom | Next step |
| --- | --- |
| Local dotnet executable is missing | Extract the x64 SDK under `.tools\dotnet`; a fresh clone does not contain tools. |
| SDK selection fails | Check `global.json` and the local SDK version. |
| Package restoration fails | Check access to NuGet and available space on the clone's drive. |
| App output is missing | Run the build script successfully before the run script. |
| A window is missing or refuses to move | Refresh the list; review app identity, access level, and [compatibility limits](limitations.md). |
| A recovery journal cannot be read | Preserve and back up the file before manual repair; do not discard recovery information casually. |
| Browser popup cannot connect | Follow [FlowWeb troubleshooting](flowweb.md#troubleshooting). |

Full UI suites need FlowDesk closed and an unlocked desktop. See [verification](verification.md) before running them.
