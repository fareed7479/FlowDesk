# Verification — v0.1.0

[Documentation](README.md) · [All verification reports](verification.md) · [Current roadmap](roadmap.md)

> Historical verification snapshot. Results and validation gaps below apply to this version; use the current roadmap for project-wide status.

2026-10-27, Windows 11 x64 build 26200, .NET SDK 10.0.403.

Solution build: passed, zero warnings and errors. Ten assertions passed, including two temporary WinForms windows detected by the Win32 service, split positions read back from the OS, and original rectangles restored and read back. Existing user windows were not moved by these checks.

FlowControl launched and its window inventory and controls were inspected earlier. Scrolling has since been added and built; this revision has not been visually re-inspected. Tray double-click, mixed DPI, multiple monitors, elevated applications and packaging remain unverified.

Local SDK/runtime and NuGet caches reside under the project on D:. CLI first-run reported installing an ASP.NET development certificate; it was not explicitly trusted. No other system-wide developer settings were intentionally changed.
