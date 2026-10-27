# Phase 8 · Distribution and release qualification

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Local lifecycle tooling implemented in v0.5.0; trusted release qualification remains pending.

## Delivered scope

Self-contained Windows ZIP packaging, per-file hashes and archive checksum, optional Authenticode signing with an existing certificate, owned per-user installation, stable data storage, opt-in sign-in launch, explicit local updates, rollback, and data-preserving removal. Native notification cleanup is performed by the owned app before removal.

## Use the feature

Follow the [distribution guide](../distribution.md) to package, install, update, roll back or remove. Packages are local and updates explicit. Use an independently obtained expected SHA256; require trusted signatures when signed artifacts are available. Browser extension loading remains manual.

## Verification evidence

Fourteen fixture checks cover the lifecycle, migration and malformed/unsafe package rejection. Actual published-package launch/removal is tracked separately in the [v0.5.0 report](../verification-v0.5.0.md). No trusted signing certificate was available. See the [current verification report](../verification-v0.5.0.md).

## Remaining work and boundaries

Trusted signing/timestamp verification, clean-machine prerequisite testing, broad hardware/app/browser validation, store publication, a network update service, and a signed graphical installer still require work or credentials. This phase is not marked production-qualified.

## Implementation map

`scripts/package.ps1`, `install-flowdesk.ps1`, `rollback-flowdesk.ps1`, `uninstall-flowdesk.ps1`, `AppearanceView.cs`.

---

[← Phase 7](phase-7.md) · [All phases](../roadmap.md)
