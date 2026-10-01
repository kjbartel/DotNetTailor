# WU-000 repository-scaffold

| Field | Value |
|---|---|
| ID | WU-000 |
| Title | repository-scaffold |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) |
| Status | Done |
| Depends on | — |
| Parallel with | — (every other WU depends on this one) |
| Target paths | repo root files, `src/*`, `tests/Tailor.*` (incl. `tests/Tailor.Testing`), `Tailor.slnx` |
| Size | M |
| Branch | none — first commits go to `main` (no repository exists yet) |

## Goal

Create the git repository and an empty, compiling, testable solution that matches the layout in [architecture §3](../../Architecture/Tailor.architecture.md#3-solution-layout). Every later WU adds code to these projects without touching build infrastructure.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/Tailor.architecture.md#1-summary) | §1 Summary | `net10.0`, xUnit v3 on MTP, golden-file helper project, working names |
| [Architecture](../../Architecture/Tailor.architecture.md#3-solution-layout) | §3 Solution Layout | Folder layout, build props, `.slnx`, CPM |
| [Architecture](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies) | §3.1 | Allowed project references |
| [Architecture](../../Architecture/Tailor.architecture.md#15-determinism) | §15 Determinism | `Deterministic=true`, CRLF working tree for sources, LF for `*.sh`, golden files and committed generated files |
| [RQ](../../Requirements/Repackage_tool_Requirements_v1.1.md) | §10 CLI and Distribution, §11 Platform and Runtime | .NET tool packaging, runtime independence |
| [Plan](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) | M0, WU-000 bullet; M0 criterion 1 | Scope and milestone gate |

## Scope

**In**
- `git init` (default branch `main`), commit 1 = existing `Docs/` only, commit 2 = scaffold.
- Root config files, 13 empty `src` projects, 15 test projects, `.slnx`, README, CONTRIBUTING, LICENSE placeholder.
- Project references exactly as in [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies).
- One placeholder test per test project so `dotnet test` succeeds (see Design notes).

**Out**
- Any production types or logic (WU-100 onwards).
- CI workflows (WU-002), AI enablement files (WU-001), test apps and `build/` (WU-003), `schemas/` content (WU-101/102).
- Creating a GitHub remote or pushing (user action).
- Choosing the licence (open question).

## Deliverables

| Path | Content |
|---|---|
| `.gitignore` | `dotnet new gitignore` (VisualStudio template) plus `artifacts/`, `.tailor/`, `*.staging-*/`, `TestResults/`, `*.received.*` |
| `.gitattributes` | `* text=auto eol=crlf`; explicit `text eol=crlf` for `*.cs`, `*.csproj`, `*.props`, `*.targets`, `*.slnx`, `*.sln`, `*.json`, `*.md`, `*.yml`, `*.yaml`, `*.xml`, `*.resx`, `*.ps1`, `*.cmd`, `*.bat`, `.editorconfig`, `.gitattributes`; then the LF exceptions `*.sh text eol=lf`, `tests/**/Golden/** text eol=lf`, `schemas/** text eol=lf` and `Docs/Guides/diagnostics.md text eol=lf` (after the CRLF lines so they win); `binary` for `*.dll`, `*.exe`, `*.pdb`, `*.nupkg`, `*.zip`, `*.ico`, `*.png`, `*.snk` |
| `.editorconfig` | `dotnet new editorconfig` baseline, `root = true`, `[*]` `end_of_line = crlf`, `[*.sh]`, `[tests/**/Golden/**]`, `[schemas/**]` and `[Docs/Guides/diagnostics.md]` `end_of_line = lf`, UTF-8, 4-space C#, 2-space JSON/YAML/XML/props, `csharp_style_namespace_declarations = file_scoped:warning`, `dotnet_style_qualification_for_* = false`, `var` preferences, `_camelCase` private fields, `IDE0005` (unused usings) as warning |
| `global.json` | `sdk.version` = current 10.0.1xx band, `rollForward: latestFeature`, `"test": { "runner": "Microsoft.Testing.Platform" }` |
| `Directory.Build.props` | `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`, `TreatWarningsAsErrors=true`, `Deterministic=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile=true` (needed for IDE0005 on build), `IsPackable=false`, `ContinuousIntegrationBuild=true` when `$(CI)`/`$(GITHUB_ACTIONS)` is `true`, `RootNamespace`/`AssemblyName` = project name |
| `Directory.Packages.props` | `ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`; versions for xUnit v3 (MTP-capable flavour) only |
| `tests/Directory.Build.props` | Imports root props; for test projects (every project except `Tailor.Testing`) sets `IsTestProject=true`, `OutputType=Exe`, xUnit v3 package reference, `Using Xunit` and a `ProjectReference` to `tests/Tailor.Testing`; suppresses `CS1591` only (CA1707 is **not** suppressed) |
| `Tailor.slnx` | All 29 projects below, solution folders `src` and `tests` |
| `src/Tailor.<P>/Tailor.<P>.csproj` | `P` ∈ `Core`, `Specifications`, `Inspection`, `Model`, `Analysis`, `Validation`, `Planning`, `Transforms`, `Acquisition`, `Execution`, `Platform.Abstractions`, `Platform.Windows`, `Cli` |
| `src/Tailor.Cli/Program.cs` | CLI entry point. Csproj: `OutputType=Exe`, `IsPackable=true`, `PackAsTool=true`, `ToolCommandName=dotnet-tailor`, `PackageId=dotnet-tailor` |
| `tests/Tailor.<P>.Tests/` | One per `src` project, referencing that project; `ScaffoldTests.cs` |
| `tests/Tailor.IntegrationTests/`, `tests/Tailor.RegressionTests/` | Reference `Tailor.Cli`; `ScaffoldTests.cs` |
| `tests/Tailor.Testing/Tailor.Testing.csproj` | Test-support class library, empty (the `Golden` helper arrives in WU-100): `IsTestProject=false`, `OutputType=Library`, no package or project references, no `ScaffoldTests` |
| `README.md` | Purpose (1 paragraph), status, prerequisites (.NET 10 SDK; .NET 8 runtime for test apps later), build/test/format commands, repo map, links to architecture, plan, requirements |
| `CONTRIBUTING.md` | WU workflow summary linking [plan §How Agents Use This Plan](../../Plans/Tailor.plan.md#how-agents-use-this-plan), branch `wu/<id>-<slug>`, PR title `WU-<id>: <title>`, required local checks, ADR rule |
| `LICENSE` | Placeholder: "Licence not yet chosen. All rights reserved until decided." |

Project references (must match §3.1; no others):

| Project | References |
|---|---|
| Core | — |
| Specifications, Inspection, Platform.Abstractions | Core |
| Model | Specifications, Inspection |
| Analysis, Validation | Model |
| Planning | Validation |
| Acquisition | Inspection |
| Transforms | Planning, Acquisition, Platform.Abstractions |
| Execution | Planning, Validation, Platform.Abstractions |
| Platform.Windows | Platform.Abstractions, Inspection |
| Cli | Analysis, Validation, Planning, Execution, Transforms, Platform.Windows |

## Design Notes

- **Zero-test exit code.** MTP returns exit code 8 when no test runs. Each test project gets `ScaffoldTests.ReferencedAssemblyLoads`, which calls `Assembly.Load("Tailor.<P>")` and asserts non-null. Do not add placeholder public types to `src`. Do not use `--ignore-exit-code 8` or `--minimum-expected-tests 0` to hide the problem.
- **xUnit v3 on MTP.** Use the xUnit v3 package flavour that matches the MTP version the .NET 10 SDK's `dotnet test` MTP mode requires (e.g. `xunit.v3.mtp-v2` if needed). Record the chosen package ids in `Directory.Packages.props`. Confirm with `dotnet test` before committing.
- **Isolation of non-solution code.** Later WUs add `tests/TestApps/` (WU-003) and `spikes/<ID>/` (WU-004..007). They will stop the props chain with their own nearer `Directory.Build.props`/`Directory.Packages.props`. Do not add wildcard project discovery that would pick them up.
- `Platform.Windows` targets `net10.0` (not `net10.0-windows`) and will use `[SupportedOSPlatform("windows")]` later ([§12](../../Architecture/Tailor.architecture.md#12-platform-abstraction)). This keeps the Cli portable.
- No `UseArtifactsOutput`: `artifacts/` is reserved for test-app output (WU-003).
- Build and test commands are always run against `Tailor.slnx` from the repo root.
- Initial commit: stage only `Docs/`. Do not commit `bin/`, `obj/` or IDE folders.

### Post-review changes

Deltas to the already-implemented scaffold (architecture [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§16](../../Architecture/Tailor.architecture.md#16-testing-strategy), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) items 37–38). Implement as one follow-up commit; verified by AC-13–AC-18.

| # | Change | Files |
|---|---|---|
| D1 | Drop Verify: no `Verify.XunitV3` version or reference; remove the "Verify.XunitV3 is deferred" comment from `tests/Directory.Build.props` | `Directory.Packages.props`, `tests/Directory.Build.props` |
| D2 | Add the empty `tests/Tailor.Testing` class library; add it to `Tailor.slnx` (`tests` folder); exclude it from the test-project settings in `tests/Directory.Build.props` and reference it from every test project there | `tests/Tailor.Testing/Tailor.Testing.csproj`, `tests/Directory.Build.props`, `Tailor.slnx` |
| D3 | Standard .NET test naming: rename `ScaffoldTests.Referenced_assembly_loads` → `ScaffoldTests.ReferencedAssemblyLoads` in all 15 test projects; remove `CA1707` and the "Snake_case" comment from `NoWarn` | `tests/*/ScaffoldTests.cs`, `tests/Directory.Build.props` |
| D4 | Line endings: `.gitattributes` and `.editorconfig` as in Deliverables (CRLF working tree; LF for `*.sh`, `tests/**/Golden/**`, `schemas/**` and `Docs/Guides/diagnostics.md`); run `git add --renormalize .` and re-checkout so the working tree matches | `.gitattributes`, `.editorconfig` |
| D5 | Ignore golden-mismatch output: `*.received.*` | `.gitignore` |

AC-10's `lf` expectation for `*.csproj`/`*.md` is superseded by AC-14; AC-6's project list (13 `src` + 15 `tests`) is extended by AC-16 with `Tailor.Testing`.

## Acceptance Criteria

- [x] AC-1 `git log --oneline` shows exactly two commits on `main`: the first contains only `Docs/**`, the second the scaffold. _(Verified at scaffold time. `main` now has 4 commits: + `889adc1` docs-only post-review spec edits, + `ceef2b6` post-review deltas D1–D5.)_
- [x] AC-2 `dotnet --version` from the repo root resolves an SDK allowed by `global.json`; `global.json` contains `"runner": "Microsoft.Testing.Platform"` under `test`.
- [x] AC-3 `dotnet build Tailor.slnx -c Release -warnaserror` exits 0 with 0 warnings.
- [x] AC-4 `dotnet test --solution Tailor.slnx -c Release` exits 0 and reports 15 passing tests (one per test project), running under MTP.
- [x] AC-5 `dotnet format Tailor.slnx --verify-no-changes` exits 0.
- [x] AC-6 `Tailor.slnx` lists exactly the 13 `src` and 15 `tests` projects named in Deliverables.
- [x] AC-7 `dotnet list <each src csproj> reference` matches the project-reference table exactly.
- [x] AC-8 No `PackageReference` has a `Version` attribute (`Select-String -Path **/*.csproj -Pattern 'PackageReference[^>]+Version='` returns nothing).
- [x] AC-9 `dotnet pack src/Tailor.Cli -c Release -o artifacts/pkg` produces `dotnet-tailor.*.nupkg` containing `tools/net10.0/any/DotnetToolSettings.xml` with command `dotnet-tailor`.
- [x] AC-10 ~~`git check-attr eol -- src/Tailor.Core/Tailor.Core.csproj Docs/Plans/Tailor.plan.md` reports `lf`~~ (superseded by AC-14; both now report `crlf`); `git check-attr binary -- x.dll` reports `set` (re-checked after D4: still `set`).
- [x] AC-11 `git status --ignored` after build shows `bin/`, `obj/` ignored; `.gitignore` contains `artifacts/` and `.tailor/`.
- [x] AC-12 README.md, CONTRIBUTING.md and LICENSE exist; README links resolve to existing files.

Post-review deltas (see [Post-review changes](#post-review-changes)):

- [x] AC-13 No Verify package anywhere: `Select-String -Path Directory.Packages.props, tests/Directory.Build.props, **/*.csproj -Pattern 'Verify'` returns nothing. _(Checked all `*.props`/`*.csproj`/`*.targets` outside `bin/obj`: no match.)_
- [x] AC-14 After re-checkout, `git ls-files --eol` shows `w/crlf` for source files and `w/lf` for `*.sh` / Golden files; `git check-attr eol -- src/Tailor.Core/Tailor.Core.csproj x.sh tests/X.Tests/Golden/C/n.golden.json` reports `crlf`, `lf`, `lf`; `dotnet format Tailor.slnx --verify-no-changes` passes. _(All 128 tracked files `i/lf w/crlf`. No `*.sh`/Golden/`schemas` files exist yet, so the `w/lf` part is verified only via `check-attr` (also `lf` for `schemas/a.json`, `Docs/Guides/diagnostics.md`). Format covered by Implementer evidence at matching state.)_
- [x] AC-15 `.editorconfig` has `end_of_line = crlf` under `[*]` and `end_of_line = lf` under `[*.sh]` and `[tests/**/Golden/**]`. _(Also `lf` under `[schemas/**]` and `[Docs/Guides/diagnostics.md]`; no later section overrides `end_of_line`.)_
- [x] AC-16 `tests/Tailor.Testing` exists as a non-test class library; `Tailor.slnx` lists 13 `src` projects, 15 test projects and `Tailor.Testing` (29 total); every test project references `Tailor.Testing`; `dotnet test --solution Tailor.slnx -c Release` still reports exactly 15 passing tests. _(Evaluated via `dotnet msbuild -getItem/-getProperty`: Testing is `IsTestProject=false`, `OutputType=Library`, no ProjectReference/PackageReference; all 15 test projects `IsTestProject=true`, `Exe`, reference Testing + their target project.)_
- [x] AC-17 Every `ScaffoldTests` method is named `ReferencedAssemblyLoads`; no `NoWarn` in the repo contains `CA1707`; `dotnet build Tailor.slnx -c Release -warnaserror` exits 0. _(15/15 methods `ReferencedAssemblyLoads`; no `CA1707`/`Referenced_assembly_loads` remain.)_
- [x] AC-18 `git check-ignore tests/X.Tests/Golden/C/n.received.json` reports the path as ignored. _(`.gitignore:489:*.received.*`.)_

## Test Requirements

- `tests/Tailor.<P>.Tests/ScaffoldTests.cs` — `ReferencedAssemblyLoads` (one per project).
- Run: `dotnet test --solution Tailor.slnx -c Release`; single project: `dotnet test --project tests/Tailor.Core.Tests --filter-class "*ScaffoldTests"`.
- Record a Test Evidence block (test-evidence protocol) in the PR description.

## Definition of Done

- All AC ticked by the Verifier; build with zero warnings; tests green; `dotnet format --verify-no-changes` clean.
- Plan status for WU-000 set to `In review`, then `Done` by the Verifier.
- No files outside the Deliverables list changed (except the plan status row).

## Agent Notes

- Context to load: [architecture §1, §3, §3.1, §15, §16](../../Architecture/Tailor.architecture.md), [plan M0](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap).
- `.github/` prompts do not exist yet (WU-001). Follow the plan workflow manually.
- Use `dotnet new classlib|console|sln --format slnx` and `dotnet sln add`; then strip template `Class1.cs`.
- Do not touch `Docs/` content beyond the plan status row.

## Open Questions

- Licence (plan Open Questions). LICENSE stays a placeholder.
- Exact SDK feature band to pin (`10.0.100` vs latest installed `10.0.1xx`/`10.0.2xx`).
- Whether xUnit v3 needs the `mtp-v2` package flavour with the pinned SDK (resolve during implementation, record in PR).
- **Resolved** — D4 includes LF exceptions for committed generated files: `schemas/** text eol=lf` and `Docs/Guides/diagnostics.md text eol=lf` in `.gitattributes`, mirrored in `.editorconfig` (architecture [§20](../../Architecture/Tailor.architecture.md#20-open-questions)).
- **Resolved** — architecture §15, the plan's WU-000 bullet and the Deliverables `.editorconfig` row now list all four LF exceptions.
- **Note (Verifier 2026-09-30)** — Test Evidence is recorded in this spec rather than a PR description: no remote/PR exists (commits go straight to `main`). Commit `889adc1` changed many `Docs/Specs/**` files beyond the plan status row; it is a docs-only planning commit, not part of the implemented deltas (`ceef2b6` touched only Deliverables files).

## Test Evidence
- **State**: `62c37e1b6257bc5e38d2dbe5934194322e16e4cc690900913bbcf6d920777bb9` (`52` files; docs included: no; exclusions: none)
- **Environment**: `Windows; .NET SDK 10.0.401 (global.json 10.0.100 + latestFeature); NuGet packages xunit.v3 4.0.1 (xunit.v3.mtp-v2), restored from nuget.org; no behaviour-affecting env vars (CI/GITHUB_ACTIONS unset)`
- **Impact**: `new repository: all 13 src projects, 15 test projects, root build props/CPM/global.json/editorconfig; no indirect consumers exist`
- **Selected checks**: `full solution build with -warnaserror, full solution test (15 ScaffoldTests), dotnet format verify — WU-000 AC-3/4/5 require the full solution`
- **Excluded checks**: `none — no other suites exist`
- **Command**: `cd E:\Tailor; dotnet format Tailor.slnx --verify-no-changes; dotnet build Tailor.slnx -c Release -warnaserror; dotnet test --solution Tailor.slnx -c Release`
- **Result**: `pass — format exit 0; build exit 0, 0 warnings, 0 errors; test exit 0, total 15, succeeded 15, failed 0, skipped 0 (MTP)`
- **Evidence source**: `run by Implementer`
- **Rerun reason**: `none`

## Test Evidence
- **State**: `62c37e1b6257bc5e38d2dbe5934194322e16e4cc690900913bbcf6d920777bb9` (`52` files; docs included: no; exclusions: none)
- **Environment**: `Windows; .NET SDK 10.0.401; no CI env vars`
- **Impact**: `Tailor.Cli packaging (PackAsTool)`
- **Selected checks**: `AC-9 pack + DotnetToolSettings.xml inspection`
- **Excluded checks**: `none`
- **Command**: `cd E:\Tailor; dotnet pack src/Tailor.Cli -c Release -o artifacts/pkg`
- **Result**: `pass — exit 0; dotnet-tailor.1.0.0.nupkg; tools/net10.0/any/DotnetToolSettings.xml contains <Command Name="dotnet-tailor" EntryPoint="Tailor.Cli.dll" Runner="dotnet" />`
- **Evidence source**: `run by Implementer`
- **Rerun reason**: `none`

## Test Evidence
- **State**: `62c37e1b6257bc5e38d2dbe5934194322e16e4cc690900913bbcf6d920777bb9` (`52` files; docs included: no; exclusions: none) — recomputed by Verifier, matches both inherited records
- **Environment**: `Windows; .NET SDK 10.0.401 (dotnet --version from repo root); no CI env vars`
- **Impact**: `AC-1/2/6/7/8/9/10/11/12 static and git checks; no code executed`
- **Selected checks**: `git history/commit contents, global.json, slnx project list, ProjectReference sets of all 28 csproj, AC-8 pattern, nupkg entry read, check-attr, git status --ignored, README/CONTRIBUTING relative link resolution`
- **Excluded checks**: `build/test/format/pack — covered by matching inherited records`
- **Command**: `cd E:\Tailor; Get-FunctionalState.ps1; git log --oneline; git show --name-only ee00d6c/311b2c3; dotnet --version; git check-attr eol -- src/Tailor.Core/Tailor.Core.csproj Docs/Plans/Tailor.plan.md; git check-attr binary -- x.dll; git status --ignored --short; Select-String -Path (all *.csproj) -Pattern 'PackageReference[^>]+Version='; ProjectReference XML parse per csproj; ZipFile read of artifacts/pkg/dotnet-tailor.1.0.0.nupkg; Test-Path on README/CONTRIBUTING link targets`
- **Result**: `pass — 2 commits (311b2c3: 72 files all Docs/**; ee00d6c: 55 files, none under Docs/); SDK 10.0.401; runner MTP; slnx 13 src + 15 tests; refs match spec table exactly; AC-8 no matches; eol lf/lf, binary set; bin/ obj/ artifacts/ ignored; 12/12 links resolve; DotnetToolSettings.xml Command Name="dotnet-tailor"`
- **Evidence source**: `run by Verifier`
- **Rerun reason**: `none`

## Test Evidence
- **State**: `e845bcecd945c85c18619d581ec064d704258aff9ad1e439bdab7135c8d2a51b` (`53` files; docs included: no; exclusions: none)
- **Environment**: `Windows; .NET SDK 10.0.401 (global.json 10.0.100 + latestFeature); NuGet xunit.v3 4.0.1 (xunit.v3.mtp-v2) restored from nuget.org; CI/GITHUB_ACTIONS unset; git core.autocrlf=true, core.safecrlf=true (global)`
- **Impact**: `root build/config files (Directory.Packages.props, .gitattributes, .editorconfig, .gitignore, Tailor.slnx), tests/Directory.Build.props (affects all 15 test projects), new tests/Tailor.Testing, all 15 ScaffoldTests.cs; no src changes`
- **Selected checks**: `full solution format verify, -warnaserror build, full solution test — AC-14/16/17 require the full solution`
- **Excluded checks**: `none — no other suites exist`
- **Command**: `cd E:\Tailor; dotnet format Tailor.slnx --verify-no-changes; dotnet build Tailor.slnx -c Release -warnaserror; dotnet test --solution Tailor.slnx -c Release`
- **Result**: `pass — format exit 0; build exit 0, 0 warnings, 0 errors; test exit 0, total 15, succeeded 15, failed 0, skipped 0 (Tailor.Testing not run as a test project); fingerprint unchanged after run`
- **Evidence source**: `run by Implementer`
- **Rerun reason**: `none`

## Test Evidence
- **State**: `e845bcecd945c85c18619d581ec064d704258aff9ad1e439bdab7135c8d2a51b` (`53` files; docs included: no; exclusions: none) — recomputed by Verifier, matches inherited record
- **Environment**: `Windows; .NET SDK 10.0.401 (dotnet --version from repo root); CI/GITHUB_ACTIONS unset; git core.autocrlf=true, core.safecrlf=true`
- **Impact**: `AC-13..AC-18 static, MSBuild-evaluation and git checks; no code executed`
- **Selected checks**: `Verify/CA1707/Referenced_assembly_loads search over *.props/*.csproj/*.targets/.editorconfig/ScaffoldTests.cs (excl. bin/obj); git ls-files --eol; check-attr eol/binary; check-ignore; .editorconfig section/end_of_line scan; .gitattributes/.gitignore/Directory.Packages.props/tests/Directory.Build.props/Testing csproj read; slnx XML parse; dotnet msbuild -getItem:ProjectReference/PackageReference -getProperty:IsTestProject/OutputType per tests/*.csproj; git show --stat 889adc1/ceef2b6`
- **Excluded checks**: `format/build/test — covered by matching inherited Implementer record`
- **Command**: `cd E:\Tailor; Get-FunctionalState.ps1; git log --oneline; git ls-files --eol; git check-attr eol -- src/Tailor.Core/Tailor.Core.csproj x.sh tests/X.Tests/Golden/C/n.golden.json schemas/a.json Docs/Guides/diagnostics.md Docs/Plans/Tailor.plan.md; git check-attr binary -- x.dll; git check-ignore -v tests/X.Tests/Golden/C/n.received.json; dotnet msbuild <each tests csproj> -getItem:ProjectReference -getProperty:IsTestProject -getProperty:OutputType; dotnet msbuild tests/Tailor.Testing/Tailor.Testing.csproj -getItem:PackageReference`
- **Result**: `pass — no Verify/CA1707 matches; 128/128 tracked files i/lf w/crlf; eol crlf,lf,lf,lf,lf,crlf; binary set; received.json ignored by .gitignore:489 *.received.*; .editorconfig [*] crlf, lf for [*.sh] [tests/**/Golden/**] [schemas/**] [Docs/Guides/diagnostics.md]; slnx 29 projects (13 /src/, 16 /tests/ incl. Testing); Testing IsTestProject=false Library, 0 project/package refs, only its csproj; 15 test projects IsTestProject=true Exe, each refs Testing + target; 15/15 ReferencedAssemblyLoads; ceef2b6 touched only Deliverables files`
- **Evidence source**: `run by Verifier`
- **Rerun reason**: `none`
