# WU-503 planner-core

| Field | Value |
|---|---|
| ID | WU-503 |
| Title | planner-core |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-502 |
| Parallel with | WU-404, WU-405 |
| Target project(s)/paths | `src/DotNetRepack.Planning/{Handlers,Actions,Pipeline,State,Checks,Acquisition}/`, `tests/DotNetRepack.Planning.Tests/{Pipeline,Checks}/` |
| Size | L |
| Branch / PR | `wu/503-planner-core` / `WU-503: planner-core` |

## Goal

Provide the side-effect-free planner. It defines the handler contract, runs the fixed phase sequence, lets handlers contribute intents and actions against an evolving projected state, and records planner-derived order and provenance. It also detects output collisions and unsafe removals, and resolves declared acquisitions through a seam. Later WUs add categories without changing the core.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §23](../../Requirements/Transformation_Specification.md#23-transformation-dependencies-and-ordering), [RQ §6](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Planner-derived ordering; fixed, explicit order reflected in outputs; impossible combinations fail |
| [TS §26.4](../../Requirements/Transformation_Specification.md#26-dry-run-and-planning-behaviour), [TS §31](../../Requirements/Transformation_Specification.md#31-relationship-to-transformation-plans-and-logs) | Per-file action kinds, plan content |
| [TS §18.5](../../Requirements/Transformation_Specification.md#18-file-and-folder-layout-rules), [TS §22.4](../../Requirements/Transformation_Specification.md#22-rule-precedence-and-conflict-resolution) | Output-path collisions are errors, never implicitly resolved |
| [TS §20.3](../../Requirements/Transformation_Specification.md#20-removal-rules) | Removal dependency safety |
| [TS §19.4](../../Requirements/Transformation_Specification.md#19-addition-rules), [TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials) | Provenance; pinned external identities |
| [TS §7.2](../../Requirements/Transformation_Specification.md#7-transformation-operations), [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) items 7, 12, 17 | Extensible categories, determinism, plan before execution |
| [RQ §7](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | No mutation in planning, auditable, testable |
| Architecture [§4](../../Architecture/DotNetRepack.architecture.md#4-processing-pipeline), [§8](../../Architecture/DotNetRepack.architecture.md#8-selectors-precedence-and-actions), [§3.1](../../Architecture/DotNetRepack.architecture.md#31-project-responsibilities-and-allowed-dependencies), [§19](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) item 9 | Phases, handler contract, action model, wiring by category |
| Plan M5 criteria 2, 3 | AC-5–AC-11 |

## Scope

**In**
- `ITransformationHandler` contract (architecture §8), `HandlerRegistry` (by category, DI), `HandlerContext`.
- `PlanPhase` (9 fixed phases), `Planner` pipeline, within-phase ordering.
- Action model with provenance. `PlanBuilder` for intents (WU-501) and actions.
- `ProjectedState`: an in-memory view of the output tree that is updated as each phase completes, and that implements WU-500 `ISelectableArtefactSet` so later phases select over the projected state.
- Phase-1 `TargetState` resolution (identity passthrough in M5: input TFM, RID, deployment model, framework versions).
- Phase-2 acquisition through the `IAcquisitionPlanner` seam.
- Phase-9 planner-owned checks: output collisions and removal dependency safety.
- Materialisation of resolved disposition/destination intents into actions at the end of phase 6.

**Out**
- Concrete category handlers (WU-504, WU-702, WU-803/804, WU-900–902). Projected AppSpec and output assertions (WU-505, which is called from phase 9). Plan serialisation, schema and CLI (WU-506). Execution (WU-601).
- Explicit-upgrade enforcement ([TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) item 8) beyond recording `VersionChange` on actions. The check is completed by WU-902.

## Deliverables

Namespace `DotNetRepack.Planning`.

| Type | API / responsibility |
|---|---|
| `ITransformationHandler` | `Category`, `Phase`, `IEnumerable<Diagnostic> Validate(HandlerContext)`, `ValueTask ContributeAsync(PlanBuilder, HandlerContext, CancellationToken)` (architecture §8, verbatim) |
| `IAcquisitionContributor` (optional handler interface) | `IEnumerable<PackageRequestIntent> DeclareAcquisitions(HandlerContext)`, called in phase 2 |
| `HandlerRegistry` | Built from DI `IEnumerable<ITransformationHandler>`. A duplicate category → startup error. `IsActive(category, ValidatedTransformSpec)` |
| `enum PlanPhase` | `ResolveTargetState=1`, `Acquisition`, `Retarget`, `Patch`, `DeploymentModel`, `FilteringLayout`, `Optimisation`, `ConfigGeneration`, `ProjectedValidation=9` |
| `HandlerContext` | `Spec` (`ValidatedTransformSpec`), `InputModel` (EAM), `InputAppSpec`, `State` (`ProjectedState`, read-only view), `Target` (`TargetState`), `Selectors` (`SelectorEngine`), `Policies` (`PolicySet`), `Acquisitions` (resolved packages) |
| `PlanBuilder` | `AddIntent(Intent)`, `AddAction(PlanActionDraft)`, `Report(Diagnostic)`. No IO |
| `enum ActionKind` | `Copy`, `Add`, `Remove`, `Replace`, `Move`, `ModifyConfig`, `Optimise`, `ExtractSymbols`, `Preserve` |
| `PlanAction` | `Id`, `Kind`, `Phase`, `Order`, `Source` (`ActionSource`), `Destination` (`OutputLocation { Root: Primary\|Symbols, Path }`, null for `Remove`), `DependsOn[]`, `Provenance`, `ExpectedHash?`, `Size?`, `VersionChange?`, `Parameters` (kind-specific, canonical-JSON serialisable) |
| `ActionSource` | `InputFile {Path, Hash}` \| `Package {Id, Version, Sha512, Path}` \| `Generated {GeneratorId, ContentHash?}` \| `UserFile {Document, Path, Hash}` |
| `Provenance` | `Handler` (category), `RuleId?`, `Document?`, `JsonPointer?`, `PrecedenceLevel?` |
| `IAcquisitionPlanner` (seam) | `ValueTask<Result<IReadOnlyList<ResolvedPackageRef>>> ResolveAsync(IReadOnlyList<PackageRequestIntent>, bool offline, CancellationToken)`. The default `NoAcquisitionPlanner` returns an error if any request exists. WU-700/702 provide the real adapter |
| `Planner.PlanAsync(PlanningInput, CancellationToken)` → `PlanResult` | `PlanningInput {ValidatedTransformSpec, AppSpecValidationResult, Options}`. `PlanResult {Actions (sorted), Acquisitions, TargetState, Variables, ProjectedState, IntentResolution, Diagnostics, PhaseLog}` |
| `Checks.OutputCollisionCheck`, `Checks.RemovalSafetyCheck` | Run in phase 9 |
| `PlanningDiagnostics` | `RPK5301`–`RPK5399` |

**Action semantics**

| Kind | Meaning |
|---|---|
| `Preserve` | Input file kept unchanged at the same path (default disposition, recorded explicitly) |
| `Copy` | Input file written to an additional destination; the source path is also retained |
| `Move` | Input file written to a different destination; the source path is absent from the output |
| `Remove` | Input artefact absent from the output (record only; `Destination` null) |
| `Add` | New file from package, user file or generator |
| `Replace` | Destination content replaced by another source (e.g. a package file) |
| `ModifyConfig` | Config file produced from a source + a named modifier (`Parameters.modifier`) |
| `Optimise` | Assembly transformed by a tool (e.g. R2R). `Parameters.unitId` |
| `ExtractSymbols` | File written to the `Symbols` output root instead of the primary root |

**Diagnostics**

| Code | Condition | Structural |
|---|---|---|
| `RPK5301` | Two actions produce the same `(Root, Path)` (policy comparer) and neither is a `Remove`/`Replace` chain on that path | Yes |
| `RPK5302` | Removal of an in-tree managed assembly referenced (resolved edge, WU-303/304) by a retained assembly, with no `Add`/`Replace` of the same simple name in a probed location | Yes |
| `RPK5303` | Removal of a **required** associated file while its primary is retained | Yes |
| `RPK5304` | `operations.extensions` category with no registered handler | Yes |
| `RPK5305` | Acquisition requested but unavailable (`NoAcquisitionPlanner`, or an error from the seam) | No (exit 4 via category) |
| `RPK5306` | Handler contributed an action outside its declared phase, or a `dependsOn` cycle | Yes (internal guard) |

## Design Notes

- **Phase loop.** For each phase in order: run `Validate` for active handlers of that phase, then `ContributeAsync` in ordinal category order. At the end of phase 6, resolve intents through WU-501 and materialise the winners (`preserve` → `Preserve`/`Move`, `exclude` → `Remove`, `separateSymbols` → `ExtractSymbols`). Then apply the phase's actions to `ProjectedState`. Handlers of the same phase see the state **before** that phase, which makes the result independent of handler order within a phase.
- **Order.** Within a phase, actions are topologically sorted by `dependsOn`, and ties are broken by `(Root, Destination ?? Source path, Kind)`. `Order` is a global sequence. `Id` = `"{phase:00}-{order:00000}"`. There are no GUIDs, random values or timestamps.
- Every in-scope input file ends up with exactly one of `Preserve`/`Move`/`Remove`/`ExtractSymbols`/`Replace`/`Optimise` (coverage invariant, [TS §9.5](../../Requirements/Transformation_Specification.md#9-include-and-exclude-rules)). With no handlers active, every file is `Preserve`.
- **Removal safety** uses the input dependency graph plus projected additions. It does not attempt native-dependency analysis (none exists in the EAM). Required associations come from the AppSpec association rules.
- **Zero side effects.** Planning reads only via `IAppTree` and the EAM. The only permitted external effect is `IAcquisitionPlanner.ResolveAsync` (writes to the NuGet package cache only, and never with `--offline`; architecture §4, §19 item 28).
- The phase-2 acquisition list is final. A handler that needs a package it did not declare in phase 2 reports `RPK5305`.
- Variables (`ResolvedTransformSpec.Variables`) and the resolved `TargetState` are part of `PlanResult` so that WU-506 can pin them ([TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials)).

## Acceptance Criteria

- [ ] AC-1 `PlanPhase` has exactly the 9 phases of architecture §4 in that order. Handlers run strictly by phase (a test with fake handlers records the call sequence).
- [ ] AC-2 A fake category registered via DI and referenced in `operations.extensions["test.noop"]` participates without any change to Planning code. An unregistered extension category yields `RPK5304`.
- [ ] AC-3 With no active handlers, every in-scope file of a synthetic tree gets exactly one `Preserve` action (coverage test), and sidecars get none.
- [ ] AC-4 Two fake handlers in the same phase, registered in either order, produce byte-identical `PlanResult` (golden file).
- [ ] AC-5 Every action carries `Phase`, `Order`, `Provenance.Handler`, and, when rule-derived, `RuleId`, `Document`, `JsonPointer` and `PrecedenceLevel`.
- [ ] AC-6 Two actions targeting `Lib/A.dll` and `lib/a.DLL` in the primary root yield `RPK5301` naming both sources (structural, not downgradable).
- [ ] AC-7 Removing `ConsoleApp.Library.dll` while `ConsoleApp.dll` is retained yields `RPK5302`. Adding a replacement with the same simple name at the same path clears it.
- [ ] AC-8 Removing a required associated file (e.g. `App.runtimeconfig.json`) while `App.dll` is retained yields `RPK5303`.
- [ ] AC-9 A handler that declares an acquisition with the default `NoAcquisitionPlanner` yields `RPK5305`. A fake planner's resolved packages appear in `PlanResult.Acquisitions` sorted by id and version.
- [ ] AC-10 Planning over a physical temp copy of a matrix app leaves the temp root's full fingerprint (including sidecars and directory listing) unchanged, and creates no files anywhere under the temp root.
- [ ] AC-11 Two `PlanAsync` runs on the same inputs produce equal `PlanResult` canonical JSON (byte comparison).
- [ ] AC-12 A `dependsOn` cycle contributed by a fake handler yields `RPK5306`.
- [ ] AC-13 A phase-7 fake handler selecting via `HandlerContext.Selectors` over `State` does not see files removed in phase 6.

## Test Requirements

- Unit: `tests/DotNetRepack.Planning.Tests/Pipeline/` and `…/Checks/`, with fake handlers, synthetic EAMs from `InMemoryAppTree`, and a fake `IAcquisitionPlanner`. Trait `WU=503`.
- Integration: `tests/DotNetRepack.IntegrationTests/Planning/PlannerCoreTests` over a matrix copy (AC-10, AC-11). Traits `Category=Integration`, `Category=Matrix`, `WU=503`.
- Run: `dotnet test --project tests/DotNetRepack.Planning.Tests --filter-trait "WU=503"`, `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=503"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `RPK53xx` codes listed for WU-1001.

## Agent Notes

- Planning must not reference Acquisition, Transforms or Execution (architecture §3.1). The `IAcquisitionPlanner` seam lives in Planning and is implemented elsewhere.
- Keep `Parameters` as a sealed record hierarchy per kind, or as a canonical `JsonObject`. Either way it must serialise deterministically for WU-506.
- WU-702, WU-803, WU-804 and WU-900–902 build on these types. Treat names in this spec as the contract.

## Open Questions

- TS §26.4 lists `Retarget` as a per-file action; architecture §8 does not. It is proposed that retargeting is expressed as `ModifyConfig`/`Replace` with `Provenance.Handler = retarget`.
- `Copy` vs `Move` vs `Preserve` semantics above are proposals (the architecture only lists names).
- Should planning fail fast after phase 1–2 errors, or continue to collect later-phase diagnostics? Proposed: stop after a phase with structural errors.
- **Resolved** — real `IAcquisitionPlanner`: the Cli registers `NoAcquisitionPlanner` for v0.2.0; the Transforms adapter over Acquisition (WU-702) is wired by the Cli from WU-704 (architecture §3.2).

## Test Evidence

_To be completed by the implementer._
