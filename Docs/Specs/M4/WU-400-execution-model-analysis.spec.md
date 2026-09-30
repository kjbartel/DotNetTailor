# WU-400: execution-model-analysis

| Field | Value |
|---|---|
| ID | WU-400 |
| Title | execution-model-analysis |
| Milestone | M4 Analysis & Validation |
| Status | Not started |
| Depends on | WU-202, WU-305 |
| Parallel with | WU-403, WU-500 |
| Target project(s)/paths | `src/Tailor.Analysis/Execution/`, `src/Tailor.Analysis/Bootstrap/`, `tests/Tailor.Analysis.Tests/Execution/`, `tests/Tailor.IntegrationTests/Analysis/` |
| Size | M |
| Branch / PR | `wu/400-execution-model-analysis` / `WU-400: execution-model-analysis` |

## Goal

Discover the application's execution model from an unknown tree — entry points, deployment model, frameworks and versions, TFMs, platform/RID — with a confidence annotation per fact, and refuse single-file bundles.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §6.2](../../Requirements/Application_Specification.md#62-information), [AS §7](../../Requirements/Application_Specification.md#7-application-execution-model) | Identity, entry points, multiple entry points |
| [AS §8](../../Requirements/Application_Specification.md#8-platform-and-architecture-model), [AS §9](../../Requirements/Application_Specification.md#9-target-framework-and-framework-model) | Platform/RID, TFM, frameworks, framework contexts, confidence |
| [RQ §4.1](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Non-mutating analysis |
| Architecture [§1.2](../../Architecture/Tailor.architecture.md#12-non-goals-v1) (single-file refusal), [§6.2](../../Architecture/Tailor.architecture.md#62-appspec-shape-illustrative-the-wu-101-schema-is-normative), [§7.4](../../Architecture/Tailor.architecture.md#7-effective-application-model-semantics) | Target AppSpec members and facts |
| Plan M4 criterion 2 (confidence annotations) | AC-7 |

## Scope

**In**: bootstrap model over an unknown tree, entry-point discovery, FD/SC detection, framework contexts, TFMs, RID/architecture, confidence, bundle refusal.

**Out**: folder roles and rule compaction (WU-401), capability report (WU-402), spec-vs-tree validation (WU-403).

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Analysis.Bootstrap.BootstrapAppSpec` | Built-in AppSpec (root `recurse: true`, built-in groups, standard associations, `references: [root]`, `duplicates: first`) used to build a provisional EAM via `EffectiveModelBuilder` |
| `Tailor.Analysis.Execution.ExecutionModelAnalyzer.Analyze(IAppTree, EffectiveApplicationModel bootstrap)` → `ExecutionModelResult` | `EntryPoints`, `DeploymentModel`, `FrameworkContexts`, `Platform`, `Diagnostics` |
| `Confident<T>` | `Value`, `Confidence` (`Explicit`, `Derived`, `Inferred`, `Unknown`), `Source` (relative path + member) |
| `EntryPoint` | `Host?` (apphost exe), `Assembly`, `Subsystem` (`console`/`gui`), `RuntimeConfig?`, `DepsJson?` |
| Diagnostic codes (proposed, `RPK40xx`) | `RPK4001` no entry point found (warning), `RPK4002` conflicting deployment-model evidence (warning), `RPK4003` inconsistent framework contexts (warning); bundle refusal reuses the WU-200 bundle code at severity Error |

## Design Notes

| Fact | Rule | Confidence |
|---|---|---|
| Apphost entry point | Native exe whose apphost binding (WU-202) names an in-tree dll | Explicit |
| DLL-only entry point | Managed dll with sibling `{name}.runtimeconfig.json` and no apphost binding it | Derived |
| Subsystem | PE optional header of the apphost (WU-200); DLL-only → `console` | Derived |
| SC | `includedFrameworks` in runtimeconfig | Explicit |
| SC (fallback) | `hostfxr.dll` + `hostpolicy.dll` + `coreclr.dll` beside entry | Derived |
| FD | `framework`/`frameworks` in runtimeconfig | Explicit |
| Framework versions | runtimeconfig; SC fallback: `System.Private.CoreLib` file version | Explicit / Derived |
| TFM | runtimeconfig `tfm`; else `TargetFrameworkAttribute` | Explicit / Derived |
| RID | deps.json `runtimeTarget` suffix; else apphost machine → `win-x64`; AnyCPU-only → `Unknown` | Explicit / Inferred / Unknown |

- Deterministic facts (FD/SC, frameworks, TFM, RID source values) come from the Model `RuntimeFactsDetector` (WU-305, architecture §3.2). This WU adds confidence, conflict handling, entry-point discovery and heuristics on top; it does not re-implement detection.
- Entry points sharing an identical framework set form one framework context (`AS §9.3`); distinct sets → separate contexts.
- Conflicting evidence → pick the Explicit source, downgrade to `Inferred`, emit `RPK4002`.
- Bundle marker on any apphost → return early with the Error diagnostic; no further analysis.
- Analysis is read-only; the bootstrap EAM is in-memory only.

## Acceptance Criteria

- [ ] AC-1 Console, WinForms, WPF matrix apps each report one entry point with the correct host, assembly and subsystem (`gui` for WinForms/WPF).
- [ ] AC-2 A synthetic tree with two apphosts and one DLL-only tool reports three entry points in path order.
- [ ] AC-3 FD vs SC is correct for every matrix entry with `Explicit` confidence.
- [ ] AC-4 Framework names and versions equal the runtimeconfig values for every matrix entry (WindowsDesktop for WinForms/WPF).
- [ ] AC-5 TFM equals `net8.0*`/`net10.0*` per matrix entry.
- [ ] AC-6 RID is `win-x64` (`Explicit`) for SC entries and `win-x64` (`Inferred`) for FD entries with a x64 apphost.
- [ ] AC-7 Every fact in `ExecutionModelResult` carries a confidence and a source (golden file).
- [ ] AC-8 Conflicting runtimeconfig vs host files yields `RPK4002` and `Inferred` confidence (synthetic).
- [ ] AC-9 A single-file bundle fixture yields the bundle Error diagnostic and no entry points.
- [ ] AC-10 Tree fingerprint before and after analysis is unchanged (physical test).

## Test Requirements

- Unit: `tests/Tailor.Analysis.Tests/Execution/` with synthetic trees and fake facts; trait `WU=400`.
- Integration: `tests/Tailor.IntegrationTests/Analysis/ExecutionModelTests` over the full matrix; golden file per entry (framework patch versions scrubbed only if the WU-003 manifest marks them volatile); trait `Category=Integration`, `Category=Matrix`, `WU=400`.
- Run: `dotnet test --project tests/Tailor.Analysis.Tests --filter-trait "WU=400"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=400"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; changes limited to target paths (plus the plan status row).

## Agent Notes

- Consume the apphost binding reader from Inspection (WU-202); do not reference `Platform.Windows`.
- `ExecutionModelResult` is consumed by WU-401 (AppSpec `application`/`execution`/`platform`/`frameworkContexts`) and WU-402; keep it a plain immutable record.

## Open Questions

- **Resolved** — apphost binding read ownership: Inspection (WU-202) is the single owner; `IApphostService` reuses it (architecture §3.2).
- **Resolved** — shared FD/SC/TFM fact extraction: Model `Model.Execution` primitives (WU-305), used by this WU and WU-403.
