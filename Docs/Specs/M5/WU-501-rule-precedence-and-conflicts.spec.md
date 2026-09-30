# WU-501 rule-precedence-and-conflicts

| Field | Value |
|---|---|
| ID | WU-501 |
| Title | rule-precedence-and-conflicts |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-500 |
| Parallel with | WU-400–WU-403 |
| Target project(s)/paths | `src/DotNetRepack.Planning/Precedence/`, `tests/DotNetRepack.Planning.Tests/Precedence/` |
| Size | M |
| Branch / PR | `wu/501-rule-precedence-and-conflicts` / `WU-501: rule-precedence-and-conflicts` |

## Goal

Assign every rule-derived intent a deterministic precedence level (TS §22.2), resolve intents per artefact so that the most specific level wins, and report equal-precedence incompatibilities as errors. Document order never resolves a conflict.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §22](../../Requirements/Transformation_Specification.md#22-rule-precedence-and-conflict-resolution) (22.2–22.5) | Specificity levels, conflicts are errors, no order dependence |
| [TS §9.6](../../Requirements/Transformation_Specification.md#9-include-and-exclude-rules) | Deterministic include/exclude precedence, explicit exceptions override wildcards |
| [TS §21](../../Requirements/Transformation_Specification.md#21-transformation-defaults) (21.3, 21.4) | Defaults are overridable and favour preservation |
| [TS §24.4](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies) | Ambiguous intent is an unconditional (structural) error |
| [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) items 9, 13 | Preservation by default, no silent conflict resolution |
| Architecture [§8](../../Architecture/DotNetRepack.architecture.md#8-selectors-precedence-and-actions), [§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes) | Levels 1–5, structural diagnostics |
| Plan M5 criterion 2 | AC-4–AC-7 |

## Scope

**In**
- `PrecedenceLevel` computation from a rule source and its selector.
- A generic intent model keyed by **intent aspect**: disposition, destination and target version. This lets WU-504 (disposition/destination) and WU-902 (target version) reuse the resolver.
- Per-artefact resolution and conflict diagnostics with related locations.
- A built-in preservation baseline.

**Out**
- Collecting intents from TransformSpec sections (WU-504 handlers). Group expansion over associations (WU-504). Output-path collisions between *different* artefacts (WU-503). Selector evaluation (WU-500).

## Deliverables

Namespace `DotNetRepack.Planning.Precedence`.

| Type | API / responsibility |
|---|---|
| `enum PrecedenceLevel` | `Baseline = 0`, `GlobalDefault = 1`, `Classification = 2`, `FolderSemantic = 3`, `PathOrFile = 4`, `ExplicitException = 5` |
| `PrecedenceCalculator.For(IntentSource, CompiledSelector)` → `PrecedenceLevel` | Mapping table below |
| `IntentSource` | `RuleId?`, `Section` (`defaults`, `rules`, `symbols`, `documentation`, `layout`, …), `Document`, `JsonPointer`, `IsException` |
| `IntentAspect` | Open string key: `disposition`, `destination`, `targetVersion` (extensible) |
| `Intent` | `Artefact` (`RelativePath`), `Aspect`, `Value` (string-comparable: `preserve`, `exclude`, `separateSymbols`, a destination path, a version), `Level`, `Source` |
| `IntentResolver.Resolve(IEnumerable<Intent>)` → `IntentResolution` | `Winners` (artefact × aspect → `Intent`), `Diagnostics`. Deterministic, independent of input order |
| `PrecedenceDiagnostics` | `RPK5101`–`RPK5199`, all structural (`IsStructural = true`) |

**Level mapping** (the level of a selector is the maximum over every predicate in its tree, including inside `not`)

| Source / predicate | Level |
|---|---|
| Built-in baseline `preserve`, `defaults.include` | 0 Baseline |
| `defaults.symbols`, `defaults.documentation`, `defaults.cultures`, and any section with an absent `select` | 1 GlobalDefault |
| `classification`, `association`, `assemblyRole`, `frameworkRole`, `tfm`, and the `symbols`/`documentation` sections (implicit association scope) | 2 Classification |
| `folderId`, `folderRole`, `plugin`, `rid`, `culture` | 3 FolderSemantic |
| `path`, `name` | 4 PathOrFile |
| Rule with `exception: true` | 5 ExplicitException |

**Conflict diagnostics**

| Code | Condition (same artefact, same aspect, same highest level, different values) |
|---|---|
| `RPK5101` | Include (`preserve`) vs exclude |
| `RPK5102` | Two different destinations |
| `RPK5103` | Two different target versions for the same component |
| `RPK5104` | Preserve vs remove of the same associated group (raised when the conflict arises through group expansion; carries the primary path) |
| `RPK5105` | Other incompatible dispositions (e.g. `separateSymbols` vs `exclude`) |

## Design Notes

- Resolution per artefact and aspect: take the highest level present. If every intent at that level has an equal `Value`, the lowest `(Document, JsonPointer)` becomes the provenance winner and the others are recorded as `Agreeing`. Otherwise report an error listing **all** conflicting sources, sorted by document and pointer. Document order is never used to pick a winner ([TS §22.5](../../Requirements/Transformation_Specification.md#22-rule-precedence-and-conflict-resolution)).
- Baseline: an artefact with no intents resolves to `preserve` at `Baseline` ([TS §9.5](../../Requirements/Transformation_Specification.md#9-include-and-exclude-rules), [TS §21.4](../../Requirements/Transformation_Specification.md#21-transformation-defaults)). `defaults.include: exclude` is explicit intent and is honoured, but still at level 0, so any narrower rule overrides it.
- Level 0 is introduced because `defaults.include` and `defaults.cultures` are both "global defaults" in TS §22.2. Putting both at level 1 would make every culture default conflict with the include default.
- Conflicts are diagnostics, not exceptions. Every conflict in a plan is reported in one pass.
- The resolver is aspect-agnostic, so new aspects need no change here ([TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) item 17).

## Acceptance Criteria

- [ ] AC-1 `PrecedenceCalculator` returns the table's level for each predicate alone, and the maximum for composite selectors, including a predicate under `not` (theory test).
- [ ] AC-2 Absent selector → level 1. `exception: true` → level 5 regardless of predicates.
- [ ] AC-3 A higher-level intent overrides a lower-level one for the same artefact and aspect (level 2 exclude vs level 4 include → include wins).
- [ ] AC-4 Include vs exclude at equal level → `RPK5101`, `Error`, structural, naming both rule ids and JSON pointers.
- [ ] AC-5 Two destinations at equal level → `RPK5102`. Two target versions at equal level → `RPK5103`.
- [ ] AC-6 Preserve vs remove of the same associated group at equal level → `RPK5104` with the primary path as related location.
- [ ] AC-7 Reversing the document order of two conflicting rules yields byte-identical diagnostics. Reversing two agreeing rules yields the same winner (permutation test).
- [ ] AC-8 Artefacts with no intents resolve to `preserve` at `Baseline`. `defaults.include: exclude` is overridden by any level ≥ 1 include.
- [ ] AC-9 `PolicyEvaluator` (WU-100) cannot downgrade any `RPK51xx` code under `Permissive` or with any condition policy (test).
- [ ] AC-10 Golden file of `IntentResolution` for a synthetic spec with 3 levels and 2 conflicts is byte-stable across two runs.

## Test Requirements

- Unit only: `tests/DotNetRepack.Planning.Tests/Precedence/`, synthetic intents and compiled selectors. Trait `WU=501`.
- Run: `dotnet test --project tests/DotNetRepack.Planning.Tests --filter-trait "WU=501"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `RPK51xx` codes listed for WU-1001.

## Agent Notes

- Keep `Intent.Value` a normalised string so that equality is ordinal and deterministic. Normalise destinations through `RelativePath`.
- Do not add knowledge of TransformSpec sections beyond `IntentSource`. WU-504 builds the intents.

## Open Questions

- The level assignment for `rid`, `culture`, `assemblyRole`, `frameworkRole` and `tfm` is not defined by TS §22.2. The table above is a proposal.
- `Baseline` (level 0) extends TS §22.2's five levels. Confirm it, or keep five levels and define an explicit precedence among the `defaults.*` members.
- Is `separateSymbols` vs `exclude` at equal level a conflict (proposed: yes) or should exclude win as the "more destructive, explicit" intent? TS §21.4 suggests conflict.

## Test Evidence

_To be completed by the implementer._
