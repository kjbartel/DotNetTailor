# WU-604 depsjson-asset-pruning

| Field | Value |
|---|---|
| ID | WU-604 |
| Title | depsjson-asset-pruning |
| Milestone | M6 Execution Engine (v0.2.0-preview) |
| Status | Not started |
| Depends on | WU-202, WU-601 |
| Parallel with | WU-505, WU-506, WU-602, WU-702 |
| Target project(s)/paths | `src/Tailor.Transforms/Configuration/DepsJson/Pruning/`, `tests/Tailor.Transforms.Tests/Configuration/DepsJson/` |
| Size | M |
| Branch / PR | `wu/604-depsjson-asset-pruning` / `WU-604: depsjson-asset-pruning` |

## Goal

Keep `*.deps.json` consistent with filtering so that v0.2.0 outputs launch: when a plan removes satellite resource assemblies, RID-specific assets or whole libraries, emit a minimal `ModifyConfig` action that removes exactly those asset entries (and empty libraries) using Microsoft.Extensions.DependencyModel. Nothing else in deps.json changes. WU-802 builds its FD⇄SC transformer on this code.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §20](../../Requirements/Transformation_Specification.md#20-removal-rules), [TS §15](../../Requirements/Transformation_Specification.md#15-resource-and-localisation-policy) | Semantic removal of resources and RID assets keeps the app consistent |
| [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) item 8 | No unrequested changes (no version changes) |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Packaging changes; safe, deterministic output |
| Architecture [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline) (phase 8 ConfigGeneration), [§9](../../Architecture/Tailor.architecture.md#9-transformation-handlers), [§15](../../Architecture/Tailor.architecture.md#15-determinism) | Config generation after filtering; determinism |
| Plan M6 criterion 1 (outputs launch after pruning) | AC-6 |

## Scope

**In**
- `DepsJsonAssetPruner.Prune(DepsJsonFacts, IReadOnlySet<RelativePath> removedAppRelativePaths) → Result<DependencyContext>`: removes `runtime`, `native`, `resources` and `runtimeTargets` entries whose resolved app-relative path is removed; removes a `package`/`project` library from `targets` and `libraries` only when **all** its assets are removed and no remaining library depends on it.
- `DepsJsonPruningHandler : ITransformationHandler` (`Category = "configGeneration.depsJsonPruning"`, `Phase = ConfigGeneration`): for each retained `*.deps.json` in the projected state, computes the removed-path set from phase-6 `Remove` actions and emits one `ModifyConfig` action (`Parameters.modifier = "depsJson.prune"`) when the set touches it.
- `DepsJsonPruneModifier : IConfigModifier` (WU-601 hook) that applies the same pruner at execution.
- Writer shared with WU-802 (`DepsJsonWriter`, DependencyModel `DependencyContextWriter`, deterministic ordering).

**Out**
- FD⇄SC changes, `runtimepack.*` libraries, `runtimeTarget` changes (WU-802). Library version changes (WU-902). runtimeconfig (WU-801).
- A dependency-safety error when removing a library that others depend on: that is WU-503 `TLR5302`; this WU keeps the entry and reports an info diagnostic.

## Deliverables

- `DepsJsonAssetPruner`, `DepsJsonPruningHandler`, `DepsJsonPruneModifier`, `DepsJsonWriter`; DI registration (handler by category, modifier by name).
- Diagnostics (proposed `TLR82xx` sub-range `TLR8290`–`TLR8299`, shared Transforms deps.json range with WU-802): `TLR8290` deps.json unreadable (error), `TLR8291` library kept because a retained library depends on it (info), `TLR8292` removed file not listed in deps.json (info, no change).

## Design Notes

- Path mapping: asset paths in deps.json are package-relative; resolve them to app-relative paths the same way the host does for the app's `runtimeTarget` (app-local layout: `runtimes/<rid>/…` kept as-is for portable apps, culture folders for resources). Use WU-202 `DepsJsonFacts` per-asset paths.
- Minimal change: every untouched library, target, `compilationOptions` and unknown member is preserved; round-trip is semantic (documented DependencyModel losses handled via JSON passthrough, same approach as WU-802).
- Order in the plan: phase 8, after phase 6 removals; the action `dependsOn` the `Remove` actions it reflects.
- The projected content (`IProjectedContentProvider`, WU-505) comes from the same pruner, so projection and execution produce identical bytes.
- Deterministic output ordering (SDK conventions where known, else ordinal).

## Acceptance Criteria

- [ ] AC-1 Removing the `de/` and `fr/` satellites of ConsoleApp removes exactly those `resources` entries; all other deps.json content is semantically unchanged (library-by-library comparison).
- [ ] AC-2 Removing non-`win-x64` `runtimes/<rid>/…` assets from `ConsoleApp/<tfm>-fdportable-il` removes exactly those `runtimeTargets` entries.
- [ ] AC-3 Removing every asset of a package library that no retained library depends on removes the library from `targets` and `libraries`; if another library depends on it, it is kept with `TLR8291`.
- [ ] AC-4 A plan without removals touching deps.json emits no `ModifyConfig` action.
- [ ] AC-5 Projected deps.json bytes (WU-505) equal the executed bytes (WU-601) for the same plan.
- [ ] AC-6 `apply` of the `resources-en` and `other-rid` scenarios on ConsoleApp (net8/net10, FD) produces outputs that launch via the smoke contract (harness).
- [ ] AC-7 No library version, sha512 or dependency range changes (test compares all remaining entries).
- [ ] AC-8 Identical inputs produce byte-identical deps.json output; malformed input yields `TLR8290` without exceptions.

## Test Requirements

- Unit: `tests/Tailor.Transforms.Tests/Configuration/DepsJson/` with checked-in deps.json fixtures. Trait `WU=604`.
- Matrix comparisons read `artifacts/testapps`: traits `Category=Matrix`, `WU=604`. Launch checks (AC-6): traits `Category=Integration`, `Category=Matrix`, `Category=Launch`, `WU=604` in `tests/Tailor.IntegrationTests/Execution/DepsJsonPruning/`.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=604"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=604"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; `TLR829x` codes listed for WU-1001; changes limited to target paths (plus the plan status row).

## Agent Notes

- WU-802 extends `DepsJsonWriter` and the pruner; keep them in `Configuration/DepsJson/` with public, reusable APIs.
- Transforms must not reference Execution (architecture §3.1); `IConfigModifier` is in Planning (architecture §3.2).

## Open Questions

- **Resolved** — `IConfigModifier` lives in `Tailor.Planning.Actions` (architecture §3.2); Transforms implement it, Execution (WU-601) consumes it.
