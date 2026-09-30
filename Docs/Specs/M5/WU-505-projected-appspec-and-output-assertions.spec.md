# WU-505 projected-appspec-and-output-assertions

| Field | Value |
|---|---|
| ID | WU-505 |
| Title | projected-appspec-and-output-assertions |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-503, WU-504 |
| Parallel with | WU-507, WU-601, WU-702 |
| Target project(s)/paths | `src/Tailor.Planning/Projection/`, `src/Tailor.Validation/TransformSpec/` (output-assertion extension of `StateAssertionEvaluator`), `tests/Tailor.Planning.Tests/Projection/`, `tests/Tailor.Validation.Tests/TransformSpec/` |
| Size | M |
| Branch / PR | `wu/505-projected-appspec-and-output-assertions` / `WU-505: projected-appspec-and-output-assertions` |

## Goal

In phase 9, derive the projected output AppSpec (state only, no history) from the input AppSpec plus the final projected state. Rebuild the EAM over a projected tree, validate it, and evaluate `output.assert` (TS §25) on the projection. The same evaluator is reused after execution (WU-602).

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §25](../../Requirements/Transformation_Specification.md#25-output-state-requirements) (25.2, 25.3) | Output assertions evaluated on the projected state during planning |
| [TS §30.4](../../Requirements/Transformation_Specification.md#30-relationship-to-application-specification), [TS §19.4](../../Requirements/Transformation_Specification.md#19-addition-rules), [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) items 14, 15 | Output AppSpec describes resulting state; history-free; projected validation |
| [TS §26.3](../../Requirements/Transformation_Specification.md#26-dry-run-and-planning-behaviour), [RQ §7](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Projected AppSpec as a dry-run output |
| [AS §1](../../Requirements/Application_Specification.md#1-purpose), [AS §3](../../Requirements/Application_Specification.md#3-design-principles), [AS §24](../../Requirements/Application_Specification.md#24-global-invariants), [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md) | State only, rule based, catch-all, no logs or history |
| Architecture [§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions) (projected AppSpec bullet), [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline) phase 9, [§5](../../Architecture/Tailor.architecture.md#5-artefacts) | Derivation, placement, canonical JSON |
| Plan M5 criterion 4 | AC-6, AC-7 |

## Scope

**In**
- `ProjectedAppTree : IAppTree` over `ProjectedState`. Content is served from the input tree (`Preserve`/`Move`/`Copy`) or from action-supplied projected content. File facts can be overridden (e.g. R2R set by WU-702).
- `ProjectedAppSpecBuilder`: input AppSpec (merged, includes flattened) + projected state + `TargetState` → output AppSpec.
- Phase-9 handler `projection` (category `projection`, built into Planning): build projected AppSpec → `EffectiveModelBuilder` over `ProjectedAppTree` → `AppSpecValidator` (WU-403) → output assertions.
- `StateAssertionEvaluator` output-side members: `absentCultures`, `readyToRun`, `symbols`.

**Out**
- Handler-specific AppSpec deltas for deployment model, retarget or patch (WU-803/804/901/900 provide `IProjectedSpecContributor`s). Writing the projected AppSpec file (WU-506). Post-execution comparison (WU-602).

## Deliverables

| Type | API / responsibility |
|---|---|
| `Planning.Projection.ProjectedAppTree` | `IAppTree` over the primary output root only (the symbols root is excluded). Sorted enumeration. `OpenRead` delegates to the input tree or to `IProjectedContentProvider` |
| `IProjectedContentProvider` | Supplied by actions producing new content (`ModifyConfig` projected bytes, packages via the WU-503 acquisition result). Unknown content → facts-only entry (see Design Notes) |
| `IProjectedFactsOverride` | Per-path fact overrides (e.g. `IsReadyToRun = true`) contributed by handlers |
| `IProjectedSpecContributor` | `Contribute(ProjectedAppSpecDraft, HandlerContext)`, for later handlers (deployment model, framework contexts, platform) |
| `ProjectedAppSpecBuilder.Build(AppSpec input, ProjectedState, TargetState, IEnumerable<IProjectedSpecContributor>)` → `AppSpec` | Rules below |
| `ProjectionHandler` (phase 9) | Produces `ProjectionResult {AppSpec, Model, ValidationResult, AssertionOutcomes, Diagnostics}` and adds it to `PlanResult` |
| `StateAssertionEvaluator` (extended) | `AssertionSide.Output` enables `absentCultures`, `readyToRun {select, state}`, `symbols` (`preserve`: PDBs present in primary; `exclude`: none in primary or symbols; `separate`: none in primary, all in symbols root) |
| `ProjectionDiagnostics` | `RPK5501`–`RPK5599` |

**Projection rules**

| AppSpec part | Rule |
|---|---|
| Header | `kind: AppSpec`, same `schemaVersion`, `generator` = this tool. **No** `includes`: the output is one flattened document |
| `application`, `platform`, `execution`, `frameworkContexts` | Copied from input, then modified only by `IProjectedSpecContributor`s (none in M5) and `TargetState` |
| `folders` | Input rules unchanged. Definitions whose folders no longer exist are kept (rules describe patterns, not inventory) |
| `classifications` | Unchanged, including the catch-all |
| `associations` | Unchanged, except `required: true` is set to `false` for an association type the plan removes or extracts from the primary root (e.g. symbols `separate`/`exclude`) |
| Forbidden content | No plan ids, rule ids, source paths, hashes, timestamps or validation state ([AS §20](../../Requirements/Application_Specification.md#20-validation-state), architecture item 1) |

**Diagnostics**

| Code | Condition |
|---|---|
| `RPK5501` | Projected AppSpec fails validation against the projected tree (wraps the WU-403 diagnostics as related locations) |
| `RPK5502` | Output assertion failed on the projection (one per member, with expected and actual values) — structural |
| `RPK5503` | Assertion or validation cannot be evaluated because content is unknown at plan time (e.g. generated binary without facts). Severity from `PolicySet` `validationWarning` |

## Design Notes

- The projection re-uses the whole M3 model build, so a layout move that breaks reference resolution fails planning through `RPK5501` instead of at run time.
- Facts-only entries: when an action cannot provide projected bytes, the entry carries size/hash `null` and explicit facts. The facts provider (WU-301 `IFileFactsProvider`) must accept overrides. Checks that need bytes report `RPK5503`.
- The symbols root is not part of the projected app tree. Symbols assertions use `ProjectedState` directly.
- The canonical write of the projected AppSpec (WU-101 writer) must be byte-stable. The output AppSpec after execution (WU-602) must equal it for pure copy/filter plans.

## Acceptance Criteria

- [ ] AC-1 For a plan with only `Preserve` actions, the projected AppSpec equals the flattened input AppSpec, except for `generator` (golden file).
- [ ] AC-2 The projected AppSpec contains no `includes`, no validation state, and no plan, rule or provenance data (JSON walk test against a forbidden-member list).
- [ ] AC-3 Excluding all PDBs sets `required: false` on a previously required `symbols` association. The projected AppSpec then validates with no `RPK3201`.
- [ ] AC-4 A layout move of `ConsoleApp.Library.dll` to a folder outside the entry assembly's reference roots yields `RPK5501` wrapping `RPK3301`.
- [ ] AC-5 Each output assertion member (all WU-102 `output.assert` members supported without M7+ handlers) has a passing and a failing test. Failures yield structural `RPK5502` with expected/actual.
- [ ] AC-6 For the WU-504 filtering, symbols (dir and zip), docs and resources scenarios on ConsoleApp net10 FD, the projected AppSpec validates (`Validated`/`ValidatedWithWarnings`) and all their output assertions pass (plan M5 criterion 4).
- [ ] AC-7 `absentCultures: ["de", "fr"]` passes after `defaults.cultures: ["en", "en-*"]` and fails without it.
- [ ] AC-8 `ProjectedAppTree` enumeration equals the `Preserve`/`Move`/`Copy`/`Add`/`Replace` destinations in the primary root, and never includes `Remove`d or `ExtractSymbols` files (property test).
- [ ] AC-9 Projection performs no filesystem writes (covered by the WU-503 AC-10 harness with the projection handler enabled).
- [ ] AC-10 Projected AppSpec bytes are identical across two runs.

## Test Requirements

- Unit: `tests/Tailor.Planning.Tests/Projection/`, `tests/Tailor.Validation.Tests/TransformSpec/` (output members), with synthetic trees. Trait `WU=505`.
- Integration: scenario TransformSpecs from WU-504 over matrix copies. Traits `Category=Integration`, `Category=Matrix`, `WU=505`.
- Run: `dotnet test --project tests/Tailor.Planning.Tests --filter-trait "WU=505"`, `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=505"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `RPK55xx` codes listed for WU-1001.

## Agent Notes

- WU-504 is a dependency: AC-6 uses its handlers and scenario TransformSpecs. AC-1–AC-5 may still use fake phase-6 handlers for isolation.
- The M3 `IFileFactsProvider` may need an override hook. Add it in Model in a backwards-compatible way and note it in the PR.

## Open Questions

- Should folder definitions that match nothing in the output be pruned from the projected AppSpec? Proposed: keep them (rules, not inventory). Confirm that WU-300 does not report empty definitions as errors.
- Should the output AppSpec keep the input's multi-document structure instead of flattening? Proposed: flatten, because included documents may live in the read-only input.
- Relaxing `required` on associations is a state change the TransformSpec does not state explicitly. Confirm it is acceptable.

## Test Evidence

_To be completed by the implementer._
