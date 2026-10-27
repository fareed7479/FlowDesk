# Phase 0 · Environment and toolchain

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Complete for the audited development machine.

## Delivered scope

Audited Windows x64, available tools, and drive storage; established the project-local .NET SDK and caches on D: where practical. Toolchain binaries and caches are excluded from source control.

## Reproduce the setup

Use the [getting-started guide](../getting-started.md) to prepare a fresh clone. The current scripts expect the SDK in `.tools\\dotnet`, rather than selecting an arbitrary SDK on PATH.

## Verification evidence

The initial audit is preserved in the [audit record](../phase-0-audit.md). [v0.1.0 verification](../verification-v0.1.0.md) records the first successful build and ten native/controller assertions.

## Remaining work and boundaries

Fresh-machine provisioning, a supported-machine matrix, and packaged prerequisite handling remain release work.

## Implementation map

`global.json`, `Directory.Build.props`, and `scripts/build.ps1`.

---

[All phases](../roadmap.md) · [Phase 1 →](phase-1.md)
