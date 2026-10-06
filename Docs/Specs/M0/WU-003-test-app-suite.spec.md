# WU-003 test-app-suite

| Field | Value |
|---|---|
| ID | WU-003 |
| Title | test-app-suite |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) |
| Status | Ready |
| Depends on | WU-000 |
| Parallel with | WU-001, WU-002, WU-006, WU-007, WU-100 |
| Target paths | `tests/TestApps/**`, `build/Build-TestApps.ps1` |
| Size | L |
| Branch | `wu/003-test-app-suite` |

## Goal

Provide source for the sample apps and a deterministic, idempotent script that publishes the `{net8.0, net10.0} × {FD, SC} × {R2R off, on}` matrix for `win-x64` into `artifacts/testapps/`, with a `manifest.json` and retained SDK crossgen2 `.rsp` files for WU-004. The matrix is the fixture corpus for M2–M9.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/Tailor.architecture.md#16-testing-strategy) | §16 Testing Strategy | App list, matrix, cache key |
| [Architecture](../../Architecture/Tailor.architecture.md#15-determinism) | §15 Determinism | Fingerprint format `(relativePath, size, sha256)` |
| [Architecture](../../Architecture/Tailor.architecture.md#71-folder-matching) | §7.1, [§7.6](../../Architecture/Tailor.architecture.md#76-graphs-as-16-rd-6) | Plugin nesting, cycle fixture |
| [AS](../../Requirements/Application_Specification.md) | §10 folders, §11 classification, §12 associations, §16 graphs | Layout features to exercise |
| [TS](../../Requirements/Transformation_Specification.md) | §15 resources, §33 example | `en`/`en-*`, other-RID removal, WPF plugin app |
| [RD](../../Requirements/R2R_tool_Design.md) | §6 Plugin Architecture | One-way chain, cycle invariant |
| [CK](../../Requirements/Read_to_run_Cake.md) | §2.2 app types, §7.1 native binaries, §7.3 resources | Coverage |
| [Plan](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) | M0 criteria 2–3; M2/M3/M8/M9 criteria; [risk R7](../../Plans/Tailor.plan.md#risks-register) | Consumers |

## Scope

**In**: test-app sources, isolation props, the build script, manifest, `.rsp` retention, smoke-run switch.

**Out**: xUnit tests consuming the matrix (M2+), regression harness (WU-405), single-file bundle and malformed-PE fixtures (WU-200), signed-input fixture (WU-800), BinaryFormatter fixture (WU-901), CI YAML (WU-002 owns it; only fix the cache-key call if needed).

## Deliverables

### Sources (`tests/TestApps/`)

| Path | Kind | TFMs | Features exercised |
|---|---|---|---|
| `Directory.Build.props`, `Directory.Packages.props` | isolation | — | Do **not** import repo root props; own CPM; `Deterministic=true`, `ContinuousIntegrationBuild=true`, `PathMap` to strip source roots, `GenerateDocumentationFile=true` for libraries, `DebugType=portable` |
| `TestApps.slnx` | convenience solution | — | Not referenced from `DotNetTailor.slnx` |
| `ConsoleApp/` + `ConsoleApp.Library/` | console exe + classlib | `net8.0;net10.0` | Satellite resources `en`, `de`, `fr` (neutral culture invariant); library with PDB + XML doc; native DLL via a package with `runtimes/<rid>/native` assets; `--smoke` exits 0 |
| `WinFormsApp/` | WinExe | `net8.0-windows;net10.0-windows` | GUI subsystem, `ApplicationIcon` (`app.ico`), version resources, `--smoke` exits 0 before showing UI |
| `WpfApp/` | WinExe | `net8.0-windows;net10.0-windows` | As WinForms, WPF profile |
| `PluginHost/`, `PluginHost.Contracts/`, `PluginHost.Plugins/PluginA/`, `…/PluginB/`, `…/PluginC/` | WPF host + classlibs | host `-windows` TFMs, plugins `net8.0;net10.0` | Layout `Plugins/PluginA/`, `Plugins/PluginB/`, nested `Plugins/PluginA/Plugins/PluginC/`; one-way chain PluginA → PluginB (reference with `Private=false`, resolved by host across plugin folders); plugins load via `AssemblyLoadContext` + `AssemblyDependencyResolver`; `--smoke` loads all plugins and exits 0 |
| `PluginHostCyclic/` + `PluginX/`, `PluginY/` | console host + classlibs | `net8.0;net10.0` | PluginX ⇄ PluginY assembly reference cycle (staged build, see Design notes). Not launched |
| `MixedMode/README.md` | documentation only | — | Optional C++/CLI fixture: requirements (MSVC C++/CLI toolset), intended layout; excluded from the matrix unless `-IncludeMixedMode` and the toolset is present |

### Script (`build/Build-TestApps.ps1`)

| Parameter | Behaviour |
|---|---|
| (none) | Build the full matrix if the source hash changed; otherwise no-op |
| `-Force` | Rebuild everything |
| `-App <name[]>`, `-Tfm <tfm[]>` | Subset (manifest still lists only what exists) |
| `-PrintCacheKey` | Print `testapps-win-<sdkVersion>-<sourceHash>` and exit (contract for WU-002) |
| `-SmokeTest` | After publishing, launch each runnable variant with `--smoke`, fail on non-zero exit or 30 s timeout |
| `-IncludeMixedMode` | Optional mixed-mode fixture |

Output layout:

```text
artifacts/testapps/
  manifest.json
  .source-hash
  <App>/<tfm>-<mode>-<r2r>/        tfm ∈ net8.0|net10.0, mode ∈ fd|sc, r2r ∈ il|r2r
  ConsoleApp/<tfm>-fdportable-il/   RID-less FD publish (multi-RID runtimes/ folder)
  _r2r-rsp/<App>/<tfm>-<mode>/*.rsp SDK crossgen2 response files (R2R variants), not in manifest
```

Publish command per variant: `dotnet publish <proj> -c Release -f <fullTfm> -r win-x64 --self-contained <true|false> -p:PublishReadyToRun=<true|false> --artifacts-path <temp>/<variant> -o <outDir>`. Plugins are published (FD, same `r2r` setting, no RID for `il`, `-r win-x64` for `r2r`) and copied into the host's `Plugins/` layout.

`manifest.json` (canonical: UTF-8 no BOM, LF, 2-space, sorted ordinal-ignore-case, no timestamps, no absolute paths):

```jsonc
{
  "schemaVersion": "1.0",
  "sdkVersion": "10.0.xxx",
  "sourceHash": "<sha256>",
  "nonDeterministic": [ { "pattern": "<glob>", "reason": "<why>" } ],
  "variants": [ { "app": "WpfApp", "variant": "net8.0-fd-r2r", "tfm": "net8.0-windows",
    "mode": "fd", "r2r": true, "rid": "win-x64", "runtimeVersion": "8.0.x",
    "expectedInvalid": null,
    "files": [ { "path": "WpfApp.dll", "size": 0, "sha256": "<hex or null if nonDeterministic>" } ] } ]
}
```

`expectedInvalid` is `null` for valid fixtures, or `{ "codes": ["TLR3401"], "reason": "<why>" }` for deliberately invalid ones. Every `PluginHostCyclic` variant sets it. Matrix-wide "zero errors" checks skip these variants and assert exactly the listed codes instead ([architecture §16](../../Architecture/Tailor.architecture.md#16-testing-strategy)).

## Design Notes

- **Isolation.** `tests/TestApps/` sits under `tests/`, whose props import the root (TreatWarningsAsErrors, net10.0, xUnit). The nearer `tests/TestApps/Directory.Build.props` and `Directory.Packages.props` must stop that chain.
- **Folder `<tfm>` uses the short form** (`net8.0`, `net10.0`) for every app. The full TFM (`net8.0-windows`) is recorded in the manifest.
- **Multi-RID `runtimes/` folder.** A `-r win-x64` publish flattens RID-specific assets, so it never produces `runtimes/<rid>/`. Only the extra `fdportable-il` variant (no `-r`) keeps it. This deviates from the pure matrix. See Open questions.
- **Native DLL.** Default: a package with native assets for several RIDs (e.g. `SQLitePCLRaw.lib.e_sqlite3`, MIT). This avoids a C++ toolchain and also feeds the multi-RID variant. Confirm the licence in the PR.
- **Cyclic plugins.** C# project references cannot form a cycle. Build in stages: (1) compile a `PluginY` stub with the final identity; (2) compile `PluginX` against the stub; (3) compile the real `PluginY` with a `Reference` to `PluginX.dll`. Keep the assembly version identical so references bind.
- **Determinism.** Isolated `--artifacts-path` per variant (no shared `obj`). Sequential builds. `PathMap`/`ContinuousIntegrationBuild` remove machine paths from PDBs. Hash every file; list files that still differ between two clean runs in `nonDeterministic` with a reason (expected candidates: none; verify R2R outputs and PDBs explicitly). The `_r2r-rsp` files contain absolute paths and are excluded from the manifest.
- **`.rsp` retention.** Copy the crossgen2 `*.rsp` files that the SDK ReadyToRun targets write under the variant's intermediate directory (locate by search under `<temp>/<variant>/obj/**`; record the exact relative path in the script comment).
- **Idempotence.** `sourceHash` = SHA-256 over sorted `(path, sha256)` of git-tracked `tests/TestApps/**`, `build/Build-TestApps.ps1`, `global.json`. Skip when `.source-hash` matches and all variant folders exist. Write output via a temp folder and move into place so an interrupted run leaves no partial variant.
- **Launching** is a test-harness function, not a tool feature ([architecture §19 item 21](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies)). net8 FD variants need the .NET 8 runtime installed.
- PowerShell 7 (`pwsh`), `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`. External processes are started with argument arrays, never string concatenation.

## Acceptance Criteria

- [ ] AC-1 `pwsh build/Build-TestApps.ps1 -Force` exits 0 and creates `artifacts/testapps/<App>/<tfm>-<mode>-<r2r>/` for every app in {ConsoleApp, WinFormsApp, WpfApp, PluginHost, PluginHostCyclic} × {net8.0, net10.0} × {fd, sc} × {il, r2r} (40 folders), plus `ConsoleApp/net8.0-fdportable-il` and `ConsoleApp/net10.0-fdportable-il`.
- [ ] AC-2 Running `-Force` a second time produces a byte-identical `manifest.json` (`Get-FileHash` equal) (M0 criterion 2).
- [ ] AC-3 Running without `-Force` after AC-1 performs no publish (log states "up to date") and leaves `manifest.json` unchanged.
- [ ] AC-4 `manifest.json` is canonical: no `\r`, no BOM, no absolute path (no drive-letter, UNC or user-profile path; `Select-String -Pattern '[A-Za-z]:[\\/]'` returns nothing), files sorted, every non-listed file has a non-null `sha256`, every `nonDeterministic` entry has a reason.
- [ ] AC-5 `-PrintCacheKey` prints one line matching `^testapps-win-10\.0\.\d+-[0-9a-f]{64}$`; the value changes when any file under `tests/TestApps/` changes and not when `src/` changes.
- [ ] AC-6 `-SmokeTest` exits 0: every console/WinForms/WPF/PluginHost variant exits 0 with `--smoke`; PluginHost smoke output lists PluginA, PluginB, PluginC.
- [ ] AC-7 Layout checks: `ConsoleApp/*/de/ConsoleApp.resources.dll`, `fr/…`, `en/…` exist; `ConsoleApp/*-fdportable-il/runtimes/` contains ≥ 2 RID folders; `PluginHost/*/Plugins/PluginA/Plugins/PluginC/PluginC.dll` exists; `PluginA` folder does not contain `PluginB.dll`.
- [ ] AC-8 `PluginHostCyclic/*/Plugins/PluginX/PluginX.dll` references `PluginY` and vice versa (verified with a short `System.Reflection.Metadata` or `ildasm`-free PowerShell check recorded in the PR). Every `PluginHostCyclic` variant has `expectedInvalid.codes = ["TLR3401"]` in `manifest.json`; every other variant has `expectedInvalid: null`.
- [ ] AC-9 SC variants contain `hostfxr.dll` and `coreclr.dll`; FD variants do not; `*.runtimeconfig.json` of SC variants contains `includedFrameworks`.
- [ ] AC-10 Every R2R variant has `artifacts/testapps/_r2r-rsp/<App>/<tfm>-<mode>/` with ≥ 1 `*.rsp`.
- [ ] AC-11 `dotnet build DotNetTailor.slnx -c Release -warnaserror` and `dotnet format DotNetTailor.slnx --verify-no-changes` still pass (TestApps are isolated).
- [ ] AC-12 With WU-002 merged, a CI re-run with unchanged TestApps sources restores `artifacts/testapps` from cache and skips the script (M0 criterion 3).

## Test Requirements

- Script-level verification is the AC commands above. Optional: `build/Build-TestApps.Tests.ps1` (Pester 5) for the source-hash and manifest-canonical functions; run with `Invoke-Pester build/`.
- No xUnit tests in this WU. M2+ tests read `manifest.json` to locate fixtures.
- Record Test Evidence for AC-1, AC-2, AC-6 (environment: OS, SDK version, installed runtimes).

## Definition of Done

- All AC ticked by the Verifier; solution builds with zero warnings; tests green; format clean.
- `test-apps` skill (WU-001) updated if it already exists, otherwise note the parameters in the PR for WU-001.
- Plan status updated.

## Agent Notes

- Load: [architecture §7.1, §7.6, §15, §16](../../Architecture/Tailor.architecture.md), this spec, [WU-002 spec](WU-002-ci-pipeline.spec.md) (cache-key contract).
- Suggested prompt: `implement-work-unit` with `wu=WU-003`; skill `test-apps` if present.
- Do not add TestApps to `DotNetTailor.slnx`. Do not commit anything under `artifacts/`.

## Open Questions

- Accept the extra `fdportable-il` variant for multi-RID coverage, or build a synthetic multi-RID layout instead?
- Add an `en-GB` satellite to exercise `en-*` selectors ([TS §33](../../Requirements/Transformation_Specification.md))?
- Native DLL: external package (default) vs. in-repo native source requiring MSVC.
- Should R2R plugin variants be R2R-compiled by the plugin publish, or remain IL so WU-704 can R2R them?
