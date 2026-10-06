# WU-403: appspec-validation-engine

| Field | Value |
|---|---|
| ID | WU-403 |
| Title | appspec-validation-engine |
| Milestone | M4 Analysis & Validation |
| Status | Ready |
| Depends on | WU-305, WU-202 |
| Parallel with | WU-400–WU-402, WU-500, WU-501 |
| Target project(s)/paths | `src/Tailor.Validation/AppSpec/`, `tests/Tailor.Validation.Tests/AppSpec/`, `tests/Tailor.IntegrationTests/Validation/` |
| Size | M |
| Branch / PR | `wu/403-appspec-validation-engine` / `WU-403: appspec-validation-engine` |

## Goal

Validate an AppSpec against a physical tree covering every check in AS §20.3, compute the validation state (`Unvalidated | Validated | ValidatedWithWarnings | Invalid`) under default, strict and permissive modes, and write a deterministic `validation-report.json` with spec hash and tree fingerprint.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §20](../../Requirements/Application_Specification.md#20-validation-state) (20.2–20.4) | States, checks, revalidation |
| [AS §2.4](../../Requirements/Application_Specification.md#24-validation), [RQ §4.2](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Validation as distinct, non-mutating operation |
| [AS §12.6](../../Requirements/Application_Specification.md#126-missing-associated-files), [AS §16.3](../../Requirements/Application_Specification.md#163-plugin-dependencies) | Associations, plugin relationships |
| [TS §24](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies) | Failure policies (strict/permissive, structural) |
| Architecture [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§13](../../Architecture/Tailor.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 1 | Separate report, policy, fingerprint |
| Plan M4 criteria 3, 5 | AC-2–AC-9, AC-11 |

## Scope

**In**: validator orchestration over `EffectiveModelBuilder`, additional declaration checks (entry points, runtime/framework, platform, folder-role consistency), severity policy, state computation, report writer.

**Out**: TransformSpec validation (WU-502), CLI/exit-code mapping (WU-404), schema validation of the document (WU-101/WU-103 loader diagnostics are passed through).

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Validation.AppSpec.AppSpecValidator.Validate(LoadedAppSpec, IAppTree, AppSpecValidationOptions)` → `AppSpecValidationResult` | `State`, `Diagnostics`, `SpecHash`, `TreeFingerprint`, `Model`, `Mode`, `WarningsAsErrors` |
| `ValidationMode` | `Default`, `Strict`, `Permissive` |
| `IAppSpecCheck` | One class per check group (table below); run in fixed order |
| `ValidationReportWriter` | `validation-report.json`: `kind: ValidationReport`, `schemaVersion`, `specHash`, `treeFingerprint`, `state`, `mode`, `counts {errors, warnings, info}`, `diagnostics[]` (code, severity, message, path, JSON pointer, related) sorted |
| Diagnostic codes (proposed, `TLR43xx`) | `TLR4301` deployment model mismatch, `TLR4302` framework name/version mismatch, `TLR4303` TFM mismatch, `TLR4304` declared entry point missing, `TLR4305` apphost binding ≠ declared assembly, `TLR4306` declared RID incompatible with binaries/RID folders, `TLR4307` folder-role inconsistency (e.g. non-satellite managed assembly in `resources`, `plugin` folder without managed assembly) |

| AS §20.3 check | Source |
|---|---|
| Folder matching | WU-300 diagnostics |
| File classification, catch-all coverage | WU-301 |
| File associations | WU-302 |
| Managed dependencies, reference paths | WU-303 |
| Plugin relationships | WU-304 |
| Runtime/framework identification | `TLR4301`–`TLR4305` via Model `RuntimeFactsDetector` (WU-305) over WU-202 readers |
| Platform assumptions | `TLR4306` via WU-200/WU-203 |
| Folder-role consistency | `TLR4307` |

## Design Notes

- **State**: any error → `Invalid`; warnings only → `ValidatedWithWarnings`; none → `Validated`. `Unvalidated` is reported when validation could not run (document failed to load or tree unreadable); the result then carries the blocking diagnostics.
- **Strict**: warnings are treated as errors → `Invalid`, `WarningsAsErrors = true` (WU-404 maps to exit 3).
- **Permissive** (architecture §13): warnings never make the state fail; `Error` diagnostics whose descriptor is `IsPolicyConfigurable` (WU-100) are downgraded to warnings. Structural codes (`IsStructural`, e.g. `TLR3001`–`TLR3006`, `TLR3101`, `TLR3102`, `TLR3401`) are never downgraded ([TS §24.4](../../Requirements/Transformation_Specification.md#244-structural-errors)). Proposed policy-configurable codes: `TLR3301`, `TLR3304`, `TLR4302`, `TLR4306`, `TLR4307` (flagged on their descriptors, not listed in the validator).
- Validation never writes to the tree; the report writer writes only to the supplied artefacts directory.
- The report does not embed the AppSpec; `apply` always re-validates (architecture item 1).

## Acceptance Criteria

- [ ] AC-1 Every AS §20.3 check group is implemented and covered by at least one failing synthetic scenario test.
- [ ] AC-2 Wrong folder role (non-satellite assembly in a `resources` folder) yields `TLR4307` and `Invalid`.
- [ ] AC-3 Missing required association yields `TLR3201` and `Invalid`.
- [ ] AC-4 Wrong reference root yields `TLR3301` or `TLR3305` and `Invalid`.
- [ ] AC-5 A removed referenced file yields `TLR3301` and `Invalid`.
- [ ] AC-6 Declared `selfContained` on an FD tree yields `TLR4301`; declared framework version mismatch yields `TLR4302`.
- [ ] AC-7 Declared entry point missing / apphost bound to another dll yields `TLR4304` / `TLR4305`.
- [ ] AC-8 Warnings-only input → `ValidatedWithWarnings` (default), `Invalid` + `WarningsAsErrors` (strict).
- [ ] AC-9 Permissive downgrades exactly the descriptors flagged `IsPolicyConfigurable`; a structural code stays an error (theory test).
- [ ] AC-10 Unloadable spec → `Unvalidated` with the loader diagnostics.
- [ ] AC-11 `validation-report.json` contains `specHash`, `treeFingerprint`, `state`, `mode`; byte-identical across two runs; golden file committed.
- [ ] AC-12 Editing a sidecar does not change `treeFingerprint`; editing an in-scope file does.
- [ ] AC-13 Hand-authored matrix AppSpecs (WU-305) validate as `Validated` or `ValidatedWithWarnings` for every matrix entry except variants flagged `expectedInvalid` in the WU-003 manifest (the cyclic plugin variant: `Invalid`, exactly `TLR3401`).

## Test Requirements

- Unit: `tests/Tailor.Validation.Tests/AppSpec/` with synthetic trees and in-memory specs; trait `WU=403`.
- Integration: `tests/Tailor.IntegrationTests/Validation/AppSpecValidationTests` over the matrix with WU-305 AppSpecs; trait `Category=Integration`, `Category=Matrix`, `WU=403`.
- Run: `dotnet test --project tests/Tailor.Validation.Tests --filter-trait "WU=403"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=403"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; codes in the Validation diagnostics catalogue; changes limited to target paths (plus the plan status row).

## Agent Notes

- Do not reference `Tailor.Analysis` (architecture §3.1). Use the Model `RuntimeFactsDetector` (WU-305) for runtime/framework checks — they compare declared values with detected facts and need no heuristics.
- Keep checks pure functions over `EffectiveApplicationModel` + readers so WU-602 can reuse them on staged trees.

## Open Questions

- **Resolved** — duplicated FD/SC/TFM extraction: moved to Model `Model.Execution` (WU-305), used by WU-400 and this WU.
- **Resolved** — permissive semantics: descriptor-driven (`IsPolicyConfigurable`), architecture §13. The concrete flagged code list above is still proposed; confirm during review. Meaning of `Unvalidated` as in Design Notes.
- "Added file" (plan M4 criterion 3): with a catch-all, an added file is valid by design; detection only occurs when it breaks a rule (see WU-405 scenarios). Confirm this interpretation.
