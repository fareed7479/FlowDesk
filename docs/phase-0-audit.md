# Phase 0 environment audit

Target: D:\PROJECTS\FlowDesk
Date: 2026-10-27

- Selected project directory exists and is empty.
- Windows x64, OS build 26200, release 25H2. Registry ProductName reports a legacy Windows 10 label.
- Git 2.50.0 is available.
- dotnet, MSBuild, winapp and WinGet were not found on PATH.
- Standard Visual Studio, .NET and Windows SDK locations were not found.
- D: has approximately 551 GiB free.
- CIM OS query was denied; registry and runtime checks were used instead.
- No application has been built or runtime-tested.
- No GitHub repository access or publishing has been authorized.

## Initial scope

v0.1.0: modular C# core, Win32 window inventory and positioning, split layout, JSON workspace save/restore, WinUI FlowControl and tray entry point. Validate pure layout behavior and then native behavior on Windows. Later phases cover Stage Manager, Mission Control, FlowDock, notifications, focus/fullscreen and optional FlowWeb extension.

## Development setup

Use project-local .tools and .cache directories on D: where possible. Obtain the .NET 10 SDK from the official Microsoft installer. Additional WinUI build prerequisites must be confirmed before claiming build support. No machine-wide development settings will be changed by the local SDK setup.

Official setup reference: https://learn.microsoft.com/en-us/windows/apps/get-started/start-here?tabs=stable
