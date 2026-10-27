# Phase 8 · Distribution and release qualification

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Not implemented. This page documents planned work, not a completed phase.

## Current baseline

Existing development output is self-contained, but the project has no signed installer, automatic updater, startup opt-in workflow, or store release. FlowWeb has development-loading and native-host scripts only.

## Development use today

For now, use the [development build instructions](../getting-started.md). Keep the complete output folder together and back up local data before output cleanup. Do not treat a copied executable as a complete installation.

## Verification evidence

There is no Phase 8 verification report or clean-machine release qualification. Previous build and feature tests do not establish installer/update readiness.

## Remaining work and boundaries

Before marking this phase complete:

- Choose and test packaging, prerequisite delivery, and a complete install/uninstall lifecycle.
- Sign and verify application artifacts and installer packages with authorized credentials.
- Add explicit startup opt-in, an update strategy, version migration, and rollback handling.
- Define stable per-user data storage and preserve existing development data during migration.
- Validate clean-machine install, upgrade, repair, uninstall, and registration cleanup.
- Complete real-app, monitor/DPI, accessibility, performance, and branded-browser release tests.
- Prepare optional extension store identities/listings and validate pinned native origins.

Release acceptance requires published evidence for these workflows, not only a successful build.

## Implementation map

Planned packaging/release tooling; no implementation claimed.

---

[← Phase 7](phase-7.md) · [All phases](../roadmap.md)
