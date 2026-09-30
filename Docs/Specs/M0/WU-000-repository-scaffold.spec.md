# WU-000 repository-scaffold

| Field | Value |
|---|---|
| ID | WU-000 |
| Title | repository-scaffold |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap) |
| Status | Not started |
| Depends on | — |
| Parallel with | — (every other WU depends on this one) |
| Target paths | repo root files, `src/*`, `tests/DotNetRepack.*`, `DotNetRepack.slnx` |
| Size | M |
| Branch | none — first commits go to `main` (no repository exists yet) |

## Goal

Create the git repository and an empty, compiling, testable solution that matches the layout in [architecture §3](../../Architecture/DotNetRepack.architecture.md#3-solution-layout). Every later WU adds code to these projects without touching build infrastructure.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/DotNetRepack.architecture.md#1-summary) | §1 Summary | `net10.0`, xUnit v3 on MTP, Verify, working names |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#3-solution-layout) | §3 Solution Layout | Folder layout, build props, `.slnx`, CPM |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#31-project-responsibilities-and-allowed-dependencies) | §3.1 | Allowed project references |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#15-determinism) | §15 Determinism | `Deterministic=true`, LF artefacts |
| [RQ](../../Requirements/Repackage_tool_Requirements_v1.1.md) | §10 CLI and Distribution, §11 Platform and Runtime | .NET tool packaging, runtime independence |
| [Plan](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap) | M0, WU-000 bullet; M0 criterion 1 | Scope and milestone gate |

## Scope

**In**
- `git init` (default branch `main`), commit 1 = existing `Docs/` only, commit 2 = scaffold.
- Root config files, 13 empty `src` projects, 15 test projects, `.slnx`, README, CONTRIBUTING, LICENSE placeholder.
- Project references exactly as in [§3.1](../../Architecture/DotNetRepack.architecture.md#31-project-responsibilities-and-allowed-dependencies).
- One placeholder test per test project so `dotnet test` succeeds (see Design notes).

**Out**
- Any production types or logic (WU-100 onwards).
- CI workflows (WU-002), AI enablement files (WU-001), test apps and `build/` (WU-003), `schemas/` content (WU-101/102).
- Creating a GitHub remote or pushing (user action).
- Choosing the licence (open question).

## Deliverables

| Path | Content |
|---|---|
| `.gitignore` | `dotnet new gitignore` (VisualStudio template) plus `artifacts/`, `.repack/`, `*.staging-*/`, `TestResults/` |
| `.gitattributes` | `* text=auto eol=lf`; explicit `text eol=lf` for `*.cs`, `*.csproj`, `*.props`, `*.targets`, `*.slnx`, `*.json`, `*.md`, `*.yml`, `*.ps1`, `*.xml`, `*.resx`; `binary` for `*.dll`, `*.exe`, `*.pdb`, `*.nupkg`, `*.zip`, `*.ico`, `*.png`, `*.snk` |
| `.editorconfig` | `dotnet new editorconfig` baseline, `root = true`, LF, UTF-8, 4-space C#, 2-space JSON/YAML/XML/props, `csharp_style_namespace_declarations = file_scoped:warning`, `dotnet_style_qualification_for_* = false`, `var` preferences, `_camelCase` private fields, `IDE0005` (unused usings) as warning |
| `global.json` | `sdk.version` = current 10.0.1xx band, `rollForward: latestFeature`, `"test": { "runner": "Microsoft.Testing.Platform" }` |
| `Directory.Build.props` | `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`, `TreatWarningsAsErrors=true`, `Deterministic=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile=true` (needed for IDE0005 on build), `IsPackable=false`, `ContinuousIntegrationBuild=true` when `$(CI)`/`$(GITHUB_ACTIONS)` is `true`, `RootNamespace`/`AssemblyName` = project name |
| `Directory.Packages.props` | `ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`; versions for xUnit v3 (MTP-capable flavour), `Verify.XunitV3` only |
| `tests/Directory.Build.props` | Imports root props; sets `IsTestProject=true`, `OutputType=Exe`, xUnit v3 + Verify package references, suppresses `CS1591` for tests |
| `DotNetRepack.slnx` | All 28 projects below, solution folders `src` and `tests` |
| `src/DotNetRepack.<P>/DotNetRepack.<P>.csproj` | `P` ∈ `Core`, `Specifications`, `Inspection`, `Model`, `Analysis`, `Validation`, `Planning`, `Transforms`, `Acquisition`, `Execution`, `Platform.Abstractions`, `Platform.Windows`, `Cli` |
| `src/DotNetRepack.Cli/Program.cs` | Top-level `return 0;` only (an Exe needs an entry point). Csproj: `OutputType=Exe`, `IsPackable=true`, `PackAsTool=true`, `ToolCommandName=dotnet-repack`, `PackageId=DotNetRepack.Tool` |
| `tests/DotNetRepack.<P>.Tests/` | One per `src` project, referencing that project; `ScaffoldTests.cs` |
| `tests/DotNetRepack.IntegrationTests/`, `tests/DotNetRepack.RegressionTests/` | Reference `DotNetRepack.Cli`; `ScaffoldTests.cs` |
| `README.md` | Purpose (1 paragraph), status, prerequisites (.NET 10 SDK; .NET 8 runtime for test apps later), build/test/format commands, repo map, links to architecture, plan, requirements |
| `CONTRIBUTING.md` | WU workflow summary linking [plan §How Agents Use This Plan](../../Plans/DotNetRepack.plan.md#how-agents-use-this-plan), branch `wu/<id>-<slug>`, PR title `WU-<id>: <title>`, required local checks, ADR rule |
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

- **Zero-test exit code.** MTP returns exit code 8 when no test runs. Each test project gets `ScaffoldTests.Referenced_assembly_loads`, which calls `Assembly.Load("DotNetRepack.<P>")` and asserts non-null. Do not add placeholder public types to `src`. Do not use `--ignore-exit-code 8` or `--minimum-expected-tests 0` to hide the problem.
- **xUnit v3 on MTP.** Use the xUnit v3 package flavour that matches the MTP version the .NET 10 SDK's `dotnet test` MTP mode requires (e.g. `xunit.v3.mtp-v2` if needed). Record the chosen package ids in `Directory.Packages.props`. Confirm with `dotnet test` before committing.
- **Isolation of non-solution code.** Later WUs add `tests/TestApps/` (WU-003) and `spikes/<ID>/` (WU-004..007). They will stop the props chain with their own nearer `Directory.Build.props`/`Directory.Packages.props`. Do not add wildcard project discovery that would pick them up.
- `Platform.Windows` targets `net10.0` (not `net10.0-windows`) and will use `[SupportedOSPlatform("windows")]` later ([§12](../../Architecture/DotNetRepack.architecture.md#12-platform-abstraction)). This keeps the Cli portable.
- No `UseArtifactsOutput`: `artifacts/` is reserved for test-app output (WU-003).
- Build and test commands are always run against `DotNetRepack.slnx` from the repo root.
- Initial commit: stage only `Docs/`. Do not commit `bin/`, `obj/` or IDE folders.

## Acceptance Criteria

- [ ] AC-1 `git log --oneline` shows exactly two commits on `main`: the first contains only `Docs/**`, the second the scaffold.
- [ ] AC-2 `dotnet --version` from the repo root resolves an SDK allowed by `global.json`; `global.json` contains `"runner": "Microsoft.Testing.Platform"` under `test`.
- [ ] AC-3 `dotnet build DotNetRepack.slnx -c Release -warnaserror` exits 0 with 0 warnings.
- [ ] AC-4 `dotnet test --solution DotNetRepack.slnx -c Release` exits 0 and reports 15 passing tests (one per test project), running under MTP.
- [ ] AC-5 `dotnet format DotNetRepack.slnx --verify-no-changes` exits 0.
- [ ] AC-6 `DotNetRepack.slnx` lists exactly the 13 `src` and 15 `tests` projects named in Deliverables.
- [ ] AC-7 `dotnet list <each src csproj> reference` matches the project-reference table exactly.
- [ ] AC-8 No `PackageReference` has a `Version` attribute (`Select-String -Path **/*.csproj -Pattern 'PackageReference[^>]+Version='` returns nothing).
- [ ] AC-9 `dotnet pack src/DotNetRepack.Cli -c Release -o artifacts/pkg` produces `DotNetRepack.Tool.*.nupkg` containing `tools/net10.0/any/DotnetToolSettings.xml` with command `dotnet-repack`.
- [ ] AC-10 `git check-attr eol -- src/DotNetRepack.Core/DotNetRepack.Core.csproj Docs/Plans/DotNetRepack.plan.md` reports `lf`; `git check-attr binary -- x.dll` reports `set`.
- [ ] AC-11 `git status --ignored` after build shows `bin/`, `obj/` ignored; `.gitignore` contains `artifacts/` and `.repack/`.
- [ ] AC-12 README.md, CONTRIBUTING.md and LICENSE exist; README links resolve to existing files.

## Test Requirements

- `tests/DotNetRepack.<P>.Tests/ScaffoldTests.cs` — `Referenced_assembly_loads` (one per project).
- Run: `dotnet test --solution DotNetRepack.slnx -c Release`; single project: `dotnet test --project tests/DotNetRepack.Core.Tests --filter-class "*ScaffoldTests"`.
- Record a Test Evidence block (test-evidence protocol) in the PR description.

## Definition of Done

- All AC ticked by the Verifier; build with zero warnings; tests green; `dotnet format --verify-no-changes` clean.
- Plan status for WU-000 set to `In review`, then `Done` by the Verifier.
- No files outside the Deliverables list changed (except the plan status row).

## Agent Notes

- Context to load: [architecture §1, §3, §3.1, §15, §16](../../Architecture/DotNetRepack.architecture.md), [plan M0](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap).
- `.github/` prompts do not exist yet (WU-001). Follow the plan workflow manually.
- Use `dotnet new classlib|console|sln --format slnx` and `dotnet sln add`; then strip template `Class1.cs`.
- Do not touch `Docs/` content beyond the plan status row.

## Open Questions

- Licence (plan Open Questions). LICENSE stays a placeholder.
- Exact SDK feature band to pin (`10.0.100` vs latest installed `10.0.1xx`/`10.0.2xx`).
- Whether xUnit v3 needs the `mtp-v2` package flavour with the pinned SDK (resolve during implementation, record in PR).
