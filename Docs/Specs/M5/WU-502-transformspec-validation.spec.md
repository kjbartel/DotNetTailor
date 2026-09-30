# WU-502 transformspec-validation

| Field | Value |
|---|---|
| ID | WU-502 |
| Title | transformspec-validation |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-501, WU-403, WU-104 |
| Parallel with | WU-404, WU-405 |
| Target project(s)/paths | `src/Tailor.Validation/TransformSpec/`, `src/Tailor.Planning/Validation/`, `tests/Tailor.Validation.Tests/TransformSpec/`, `tests/Tailor.Planning.Tests/Validation/` |
| Size | M |
| Branch / PR | `wu/502-transformspec-validation` / `WU-502: transformspec-validation` |

## Goal

Turn a loaded TransformSpec plus a validated input AppSpec into a `ValidatedTransformSpec` that planning can consume: variables resolved, semantics checked, input assertions evaluated, selectors compiled, and the failure-policy set built. Structural errors can never be downgraded.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §2.2](../../Requirements/Transformation_Specification.md#2-lifecycle), [TS §5](../../Requirements/Transformation_Specification.md#5-specification-identity-and-schema) | Validation before planning; schema/identity diagnostics passed through |
| [TS §6](../../Requirements/Transformation_Specification.md#6-input-application-requirements) (6.2–6.4) | Input assertions against the AppSpec; failure prevents execution |
| [TS §7.1](../../Requirements/Transformation_Specification.md#7-transformation-operations), [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) item 2, [RQ §4.3](../../Requirements/Repackage_tool_Requirements_v1.1.md) | At least one material operation; a validated AppSpec is required |
| [TS §24](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies) | Condition policies (`error\|warning\|skip\|preserve`), unconditional structural errors |
| [TS §28.4](../../Requirements/Transformation_Specification.md#28-variables-and-parameters) | Variables resolved before planning (WU-104 resolver) |
| [TS §29.2](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials) | No credentials (WU-102 structural checks passed through) |
| Architecture [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies), [§13](../../Architecture/Tailor.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) items 7, 16 | Project split, policy model, "material operation" definition |
| Plan M5 criterion 2 (failed input assertions) | AC-5, AC-6 |

## Scope

**In**
- `Tailor.Validation.TransformSpec`: semantic checks and input-assertion evaluation. These do not use selectors or the planner.
- `Tailor.Planning.Validation`: the orchestrating pipeline. It compiles every selector (WU-500), computes rule precedence levels (WU-501) for the report, applies the `selectorMatchesNothing` policy, and builds the `PolicySet`.
- A reusable `StateAssertionEvaluator` (input side). WU-505 extends it for output-only members.

**Out**
- Conflict detection between intents (done during planning: WU-503 runs the WU-501 resolver on intents contributed by WU-504).
- Handler-specific validation (`ITransformationHandler.Validate`, WU-503+). Unknown `operations.extensions` categories (WU-503, which owns the handler registry).
- CLI wiring (WU-506).

## Deliverables

| Type | API / responsibility |
|---|---|
| `Validation.TransformSpec.TransformSpecSemanticValidator.Validate(ResolvedTransformSpec, AppSpecValidationResult)` → `IReadOnlyList<Diagnostic>` | Checks table below |
| `Validation.TransformSpec.StateAssertionEvaluator.Evaluate(StateAssertions, AppSpec, EffectiveApplicationModel, AssertionSide)` → `IReadOnlyList<AssertionOutcome>` | One outcome per asserted member: `Member`, `Expected`, `Actual`, `Passed`, JSON pointer |
| `Validation.TransformSpec.PolicySetBuilder.Build(Defaults?, FailureMode)` → `PolicySet` | `PolicySet.For(conditionKey)` → `ConditionPolicy`; built-in defaults table below |
| `Planning.Validation.TransformSpecValidationPipeline.Validate(LoadedSpecification<TransformSpecDocument>, IReadOnlyDictionary<string,string> vars, AppSpecValidationResult, FailureMode)` → `Result<ValidatedTransformSpec>` | Order: loader diagnostics → WU-104 resolver → input AppSpec state gate → semantic checks → input assertions → selector compilation + match report → policy application |
| `ValidatedTransformSpec` | `Document` (resolved), `Variables`, `PolicySet`, `CompiledSelectors` (by JSON pointer), `SelectorMatchReport`, `RuleLevels`, `AssertionOutcomes`, `Diagnostics` |
| `TransformSpecDiagnostics` | `RPK4401`–`RPK4499` (Validation category) |

**Semantic checks**

| Code | Condition | Structural |
|---|---|---|
| `RPK4401` | Input AppSpec state is `Invalid` or `Unvalidated` ([RQ §4.3](../../Requirements/Repackage_tool_Requirements_v1.1.md)) | Yes |
| `RPK4402` | No material operation: `operations` has no category, **and** `rules`, `layout`, `additions` are empty, **and** `symbols`/`documentation` are absent or `preserve`, **and** `defaults` changes nothing (architecture item 16) | Yes |
| `RPK4403` | Selector references an unknown `folderId`, classification group or association type of the input AppSpec | Yes |
| `RPK4404` | `defaults.policies` assigns a policy to a structural condition, or `skip`/`preserve` to a condition that does not support it | Yes |
| `RPK4405` | Input assertion failed (one diagnostic per failed member, with expected and actual values) | Yes (see Open Questions) |
| `RPK4406` | Unsupported target platform for v1 (a RID other than `win-x64` in `deploymentModel.rid` or in assertions) | Yes |
| `RPK4407` | `symbols.output.path` / `layout.destination` / `additions.destination` escapes its root after variable resolution | Yes |
| `RPK4408` | Requested selector matches nothing (severity from `PolicySet.For("selectorMatchesNothing")`) | No |

**Built-in condition policy defaults** (overridable in `defaults.policies`, see [TS §24.2](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies))

| Condition key | Default |
|---|---|
| `selectorMatchesNothing` | `warning` |
| `optionalAssociationMissing` | `skip` |
| `optimisationFailure` | `error` |
| `packageUpdateUnavailable` | `error` |
| `compatibilityUncertain` | `error` |
| `unknownContent` | `preserve` |
| `validationWarning` | `warning` |

## Design Notes

- Architecture §3.1 puts TransformSpec validation in Validation and selectors in Planning, and forbids Validation → Planning. The split above respects that: selector-free checks live in Validation, and the pipeline that needs selectors lives in Planning.
- Input assertions are evaluated against the **validated input AppSpec** and its EAM, never against heuristics. `tfm` and `version` use glob/range semantics as in WU-102. `folderIds`, `plugins` and `assemblies` are checked for presence in the EAM.
- `--strict` / `--permissive` combine with `PolicySet` through `PolicyEvaluator` (WU-100). Structural descriptors stay `Error` ([TS §24.4](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies)).
- All diagnostics are collected; validation never stops at the first error, except that a failed WU-104 resolution stops before semantic checks (unresolved values are meaningless).
- `ValidatedTransformSpec` is immutable and is the only TransformSpec input of the planner (WU-503).

## Acceptance Criteria

- [ ] AC-1 A spec with a declared variable and no value yields `RPK1402` (from WU-104). The pipeline stops before semantic checks and reports only resolution errors.
- [ ] AC-2 An input AppSpec whose validation state is `Invalid` yields `RPK4401` and no `ValidatedTransformSpec`.
- [ ] AC-3 A spec with only a header, or only `symbols: {policy: preserve}`, yields `RPK4402`. A spec with only one exclude rule, or only `symbols: {policy: separate}`, passes.
- [ ] AC-4 Selectors referencing an unknown `folderId`, classification or association type each yield `RPK4403` with a JSON pointer.
- [ ] AC-5 Each TS §6.2 assertion member in WU-102 `StateAssertions` has a passing and a failing test against a matrix AppSpec (e.g. `deploymentModel: selfContained` on an FD app → `RPK4405` with expected `selfContained` and actual `frameworkDependent`).
- [ ] AC-6 `RPK4405` stays `Error` under `--permissive` and with any `defaults.policies` entry (test).
- [ ] AC-7 `defaults.policies` that set a structural condition, or an unsupported mode, yield `RPK4404`.
- [ ] AC-8 A rule selector that matches nothing yields `RPK4408` as a warning by default, as an error with `selectorMatchesNothing: error`, is suppressed with `skip`, and is escalated to failure under `--strict`.
- [ ] AC-9 `ValidatedTransformSpec` contains a compiled selector and a precedence level for every selector-bearing member. Its golden file for the TS §33 fixture over the matrix PluginHost net8 FD entry is byte-stable across two runs.
- [ ] AC-10 `Tailor.Validation` has no reference to `Tailor.Planning` (assembly-reference test).

## Test Requirements

- Unit: `tests/Tailor.Validation.Tests/TransformSpec/` and `tests/Tailor.Planning.Tests/Validation/`, with synthetic AppSpecs/EAMs and in-memory TransformSpecs (WU-103 `InMemoryDocumentSource`). Trait `WU=502`.
- Integration: one class in `tests/Tailor.IntegrationTests/Planning/` over matrix copies and WU-305 AppSpecs, with traits `Category=Integration`, `Category=Matrix`, `WU=502`.
- Run: `dotnet test --project tests/Tailor.Validation.Tests --filter-trait "WU=502"`, `dotnet test --project tests/Tailor.Planning.Tests --filter-trait "WU=502"`, `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=502"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `RPK44xx` codes listed for WU-1001.

## Agent Notes

- Reuse `AppSpecValidationResult.Model` from WU-403. Do not rebuild the EAM.
- Keep `StateAssertionEvaluator` model-agnostic (AppSpec + EAM in, outcomes out) so that WU-505 (projected) and WU-602 (staged output) can call it unchanged.

## Open Questions

- TS §6.4 allows an "explicit and safe override mechanism" for failed input assertions. None is defined for v1, so `RPK4405` is proposed as structural.
- The exact "material operation" rule (`RPK4402`), in particular whether a `defaults`-only spec (e.g. `defaults.documentation: exclude`) counts. Proposed: yes, it counts.
- Default condition policies above are proposals. WU-102 marks the condition key list as provisional.
- The plan dependency on WU-501 is weak: conflicts are only detectable once WU-504 contributes intents, so this WU uses WU-501 only for `RuleLevels`. Consider moving conflict detection wording in M5 criterion 2 to WU-503/WU-504.

## Test Evidence

_To be completed by the implementer._
