# WU-602 post-execution-validation

| Field | Value |
|---|---|
| ID | WU-602 |
| Title | post-execution-validation |
| Milestone | M6 Execution Engine (v0.2.0-preview) |
| Status | Not started |
| Depends on | WU-601, WU-505 |
| Parallel with | WU-506, WU-604 |
| Target project(s)/paths | `src/Tailor.Execution/PostValidation/`, `src/Tailor.Execution/Pipeline/`, `src/Tailor.Execution/Reports/`, `tests/Tailor.Execution.Tests/{PostValidation,Pipeline}/`, `tests/Tailor.IntegrationTests/Execution/` |
| Size | M |
| Branch / PR | `wu/602-post-execution-validation` / `WU-602: post-execution-validation` |

## Goal

After execution and before commit, re-derive the Effective Application Model from the staged output using the projected AppSpec. Validate it, compare it with the projection, and evaluate the output assertions against the actual state. Write the output AppSpec and the reports. On any failure, roll back so that no output remains (exit 5).

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §25.4](../../Requirements/Transformation_Specification.md#25-output-state-requirements) | Assertions checked against the actual output before success |
| [TS §30.4](../../Requirements/Transformation_Specification.md#30-relationship-to-application-specification), [RQ §4.3](../../Requirements/Repackage_tool_Requirements_v1.1.md) | New output directory plus a new AppSpec describing the resulting state |
| [RQ §9](../../Requirements/Repackage_tool_Requirements_v1.1.md), [AS §23](../../Requirements/Application_Specification.md#23-location-and-portability) | AppSpec default and alternate locations; read-only inputs |
| [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md), [TS §31](../../Requirements/Transformation_Specification.md#31-relationship-to-transformation-plans-and-logs) | Reports separate from the AppSpec |
| [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Safe failure |
| Architecture [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline) (PV → C → W), [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§11](../../Architecture/Tailor.architecture.md#11-execution-and-safety), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) items 1, 2, [§20](../../Architecture/Tailor.architecture.md#20-open-questions) (artefacts location for `apply`) | Re-derivation, sidecar output AppSpec, report location |
| Plan M6 criteria 1, 3 | AC-4–AC-9 |

## Scope

**In**
- `PostExecutionValidator`: EAM over the staged primary root (`PhysicalAppTree`) with the projected AppSpec → `AppSpecValidator` (WU-403) → projection comparison → `StateAssertionEvaluator` (WU-502/505) with `AssertionSide.Output`, including the symbols root.
- `ExecutionPipeline`: staging session (WU-600) → executor (WU-601) → post-validation → write the output AppSpec into staging (or to `--spec-out`) → commit → write reports. Rollback on any failure or cancellation.
- Report placement rules for `apply`, including read-only inputs.

**Out**
- CLI argument handling and exit-code mapping (WU-603). Plan creation (WU-503–WU-506).

## Deliverables

| Type | API / responsibility |
|---|---|
| `PostValidation.PostExecutionValidator.Validate(StagedOutput, ProjectionResult, ValidatedTransformSpec)` → `PostExecutionResult` | `ActualModel`, `ValidationResult`, `ProjectionDifferences[]`, `AssertionOutcomes[]`, `Diagnostics`, `Passed` |
| `PostValidation.ProjectionComparer` | Compares actual vs projected: file set per root, sha256 where the projection knows it, primary classification, associations, and assembly facts that the projection overrides (e.g. R2R) |
| `Pipeline.ExecutionPipeline.RunAsync(ExecutionRequest, CancellationToken)` → `ExecutionOutcome` | `ExecutionRequest {InputRoot, OutputRoot, SymbolsOutput?, SpecOut?, ArtifactsDirectory, PlanResult, ValidatedTransformSpec}`. `ExecutionOutcome {Succeeded, Stage (Preflight\|Execute\|PostValidate\|Commit\|Reports), Diagnostics}` |
| `Reports.ArtefactLocationResolver` | Rule table below |
| Output AppSpec | Canonical projected AppSpec (WU-505) written as `<staging>/tailor.appspec.json` (sidecar, excluded from the model) or to `--spec-out` |
| Reports | `validation-report.json` (output tree), `execution-report.json` (WU-601), `post-execution-report.json` (differences and assertion outcomes) |
| `PostExecutionDiagnostics` | `TLR6201`–`TLR6299` |

**Artefacts location for `apply`**

| Condition | Location |
|---|---|
| `--artefacts <dir>` given | `<dir>` (checked by WU-600: not inside output, input or symbols output) |
| Input AppSpec directory is writable and outside the input tree, or the input tree is writable | `.tailor/` next to the input AppSpec (architecture §5; sidecar per item 2) |
| Otherwise (read-only input) | `<output>.tailor/` sibling of the output (architecture §20, provisional) |

**Diagnostics**

| Code | Condition |
|---|---|
| `TLR6201` | Staged tree fails AppSpec validation against the projected AppSpec (wraps WU-403 diagnostics) |
| `TLR6202` | Actual state differs from the projection (file set, hash, classification, association or fact), one per difference |
| `TLR6203` | Output assertion failed on the actual output (structural) |
| `TLR6204` | Output AppSpec could not be written (`--spec-out` not writable) — checked in pre-flight |
| `TLR6205` | Reports could not be written after a successful commit (warning; output kept, exit 0 with a warning, or 3 under `--strict`) |

## Design Notes

- Architecture item 1: validation state is a separate report. The output AppSpec never embeds it.
- Sidecar handling: `tailor.appspec.json` is written into staging **after** validation, and is added to the `SidecarSet` so that it does not affect the tree fingerprint.
- `--spec-out` is validated in pre-flight (writable parent, not inside input) before any execution, so a late failure cannot leave a committed output without its AppSpec.
- Any `Error` (after policy and `--strict`) from post-validation → `StagingSession.RollbackAsync()` → `ExecutionOutcome.Succeeded = false`, `Stage = PostValidate`. WU-603 maps this to exit 5 (plan M6 criterion 3). Reports already produced are still written to the artefacts location for diagnosis.
- Post-validation reuses WU-403 checks unchanged (WU-403 agent note), so input and output validation behave identically.
- For pure filter/layout plans the output AppSpec must be byte-identical to the projected AppSpec written by WU-506.

## Acceptance Criteria

- [ ] AC-1 For the WU-504 filtering scenario, the staged output validates `Validated`, has no `TLR6202`, and all output assertions pass. `tailor.appspec.json` in the committed output is byte-identical to the projected AppSpec from `plan`.
- [ ] AC-2 `--spec-out <file>` writes the output AppSpec there and not into the output. An unwritable `--spec-out` fails pre-flight with `TLR6204` and no staging is created.
- [ ] AC-3 A fake executor that silently skips one file yields `TLR6202` (missing file), rollback, and no output or staging directory.
- [ ] AC-4 A fake executor that writes different bytes for a preserved file yields `TLR6202` (hash).
- [ ] AC-5 An output assertion that passes on the projection but fails on the actual output (injected via a fake executor that adds a `de/` satellite) yields `TLR6203`, rollback, and no output (M6 criterion 3).
- [ ] AC-6 Artefacts location follows the rule table: `--artefacts`; `.tailor/` next to a writable AppSpec; `<output>.tailor/` for a read-only input copy (real ACL-deny test), with the input fingerprint including sidecars unchanged.
- [ ] AC-7 After a successful run, `execution-report.json`, `validation-report.json` and `post-execution-report.json` exist in the artefacts location. The validation report's `treeFingerprint` equals the fingerprint of the committed output excluding sidecars.
- [ ] AC-8 After any post-validation failure, no `<output>`, `<output>.staging-*` or symbols output remains. The reports still exist in the artefacts location.
- [ ] AC-9 Cancellation during post-validation leaves no output or staging (WU-600 `TLR6010`).
- [ ] AC-10 Two successful runs produce byte-identical output AppSpecs, validation reports and post-execution reports.

## Test Requirements

- Unit: `tests/Tailor.Execution.Tests/{PostValidation,Pipeline}/`, with synthetic plans, fake executors (fault injection) and temp directories. Trait `WU=602`.
- Integration: `tests/Tailor.IntegrationTests/Execution/` over matrix copies with WU-504 scenario TransformSpecs, driven through the in-process pipeline (no CLI). Traits `Category=Integration`, `Category=Matrix`, `WU=602`.
- Run: `dotnet test --project tests/Tailor.Execution.Tests --filter-trait "WU=602"`, `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=602"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `TLR62xx` codes listed for WU-1001.

## Agent Notes

- The artefacts-location rules are shared with `plan` (WU-506). If WU-506 merges first, extend its resolver instead of adding a second one.
- Keep `ExecutionPipeline` free of System.CommandLine types. WU-603 maps CLI options onto `ExecutionRequest`.

## Open Questions

- Artefacts location for `apply` when the AppSpec lives in a read-only input is provisional (architecture §20). Also: should `apply` ever write sidecars into the input (the `.tailor/` default), or always use `<output>.tailor/`? Proposed: follow architecture §5 for consistency with `plan`.
- **Resolved** — failed output assertion after execution: exit 5 (output rolled back), architecture §13.
- `TLR6205` (reports unwritable after commit): the output is already committed and cannot be rolled back atomically. Confirm warning semantics.

## Test Evidence

_To be completed by the implementer._
