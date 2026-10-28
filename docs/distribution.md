# Distribution and local updates

[Documentation](README.md) · [Phase 8](phases/phase-8.md)

The v0.6.0 tooling creates a self-contained Windows x64 package and supports per-user installation, explicit local updates, rollback, and data-preserving removal. Trusted signing, clean-machine qualification, store publication, and a network update service remain separate release work.

## Build a package

Run the application checks first, then use PowerShell 7:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Output: `.cache/dist/FlowDesk-0.6.0-win-x64.zip` and a companion `.sha256` file. The archive includes the desktop app, native host, extension sources, and lifecycle scripts. Tools, caches, runtime data, and pairing tokens are excluded. Existing artifacts of the same version are not overwritten.

No certificate is generated or trusted automatically. To sign, supply an existing current-user code-signing certificate with `-CertificateThumbprint`; optional `-TimestampServer` selects a timestamp service. Without that credential the result is an unsigned development package. Keep private keys outside the repository.

## Install or update

Run the checked-in installer with a local ZIP and its independently obtained expected SHA256:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\install-flowdesk.ps1 -PackagePath .\.cache\dist\FlowDesk-0.6.0-win-x64.zip -Sha256 '<expected 64-character SHA256>' -InstallRoot D:\Apps\FlowDesk
```

The hash verifies integrity against the supplied value; it does not authenticate the publisher. Add `-RequireSignature` when using a trusted signed package. Unsigned development artifacts fail that option.

Close the installed app and native browser connections before updating. Invoke the same installer with a newer package/version. It validates archive paths, file hashes, and ownership, keeps versioned payloads, and points the app to stable local data. Updates are explicit local operations; packages are not silently downloaded.

Default root: `Apps\FlowDesk` on the script's drive. Use a dedicated folder. Unrelated nonempty directories and reparse-point paths are refused.

| Option | Behavior |
| --- | --- |
| `-NoShortcut` | Skip creation of a Start menu shortcut |
| `-Startup` | Explicitly enable sign-in launch; off by default |
| `-RegisterFlowWeb` | Register owned Chrome/Edge native hosts; extension loading remains manual |
| `-ImportDataPath` | Import saved data into a new data folder; exclude bridge tokens and active recovery journals |
| `-RequireSignature` | Require trusted signatures on FlowDesk assemblies/entry points and lifecycle scripts |

Saved data stays under `<InstallRoot>\data`, separate from application versions. End or recover sessions in the original development build before migration, and back up the source data. Imported transient journals are intentionally excluded.

## Roll back or remove

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\rollback-flowdesk.ps1 -InstallRoot D:\Apps\FlowDesk
pwsh -ExecutionPolicy Bypass -File .\scripts\uninstall-flowdesk.ps1 -InstallRoot D:\Apps\FlowDesk
```

Rollback verifies the previous payload and switches owned integrations while preserving data. Future incompatible data-schema changes would require an explicit migration policy.

Removal checks installation ownership and path boundaries, removes owned version files and matching integrations, and retains saved data and the ownership marker. Remove browser extensions manually. Before removal, each owned app version verifies its files and invokes its native notification registration cleanup. The script removes the protocol only if it still points inside this installation; unrelated identities/history are not erased. The `-SkipAppIntegrationCleanup` switch is reserved for fixture tests or explicit recovery when an app cannot run.

## Repeatable checks

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-distribution.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-real-package.ps1
```

Fixture-package checks cover installation, data import, update, rollback, wrong checksums, unsigned rejection, traversal and undeclared files, unrelated folders, and data-preserving uninstall. They run under ignored cache directories without enabling startup or changing personal browser profiles. Actual published-package installation is recorded separately in the versioned verification report.

## Release boundaries

A self-contained ZIP and a tested local lifecycle are not a clean-machine compatibility guarantee. Trusted signing, clean-machine prerequisites, broad monitor/app/browser coverage, store identities, a network update service, and a signed installer UI still need qualification or credentials.
