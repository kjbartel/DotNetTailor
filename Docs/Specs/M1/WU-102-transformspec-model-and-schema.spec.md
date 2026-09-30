# WU-102 transformspec-model-and-schema

| Field | Value |
|---|---|
| ID | WU-102 |
| Title | transformspec-model-and-schema |
| Milestone | M1 Core Primitives & Specification Documents |
| Status | Not started |
| Depends on | WU-100, WU-007 (consumes `Specifications.Common` from WU-101, see Agent Notes) |
| Parallel with | WU-101, WU-105, M2 |
| Target project(s)/paths | `src/Tailor.Specifications/TransformSpec/`, `schemas/transformspec/v1/transformspec.schema.json`, `tests/Tailor.Specifications.Tests/` (`TransformSpec/`, `Fixtures/TransformSpec/`) |
| Size | L |

## Goal

Define the v1 TransformSpec object model (intent only), its tolerant read and canonical write via the shared `Specifications.Common` pipeline, and the generated, committed JSON Schema with drift protection.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Identity, schema | [TS §5](../../Requirements/Transformation_Specification.md), [TS §32](../../Requirements/Transformation_Specification.md) (16) | [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules) |
| Sections | [TS §6](../../Requirements/Transformation_Specification.md)–[§25](../../Requirements/Transformation_Specification.md), [TS §27](../../Requirements/Transformation_Specification.md) (`includes` as data), [TS §28](../../Requirements/Transformation_Specification.md) (declarations only), [TS §29](../../Requirements/Transformation_Specification.md) | [§6.3](../../Architecture/Tailor.architecture.md#63-transformspec-shape-illustrative-the-wu-102-schema-is-normative), [§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions) |
| Extensibility | [TS §7.2](../../Requirements/Transformation_Specification.md), [TS §14.2](../../Requirements/Transformation_Specification.md), [TS §32](../../Requirements/Transformation_Specification.md) (17) | [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies) (wiring by category) |
| Conceptual example | [TS §33](../../Requirements/Transformation_Specification.md) | — |
| No credentials | [TS §29.2](../../Requirements/Transformation_Specification.md), [TS §12.4](../../Requirements/Transformation_Specification.md) | [§17](../../Architecture/Tailor.architecture.md#17-security) |

## Scope

**In**
- `TransformSpecDocument` model covering every TS section in the table below; `TransformSpecFormat`.
- Selector model (`all`/`any`/`not` + semantic and physical predicates) as data.
- Per-document structural checks; schema generation, commit, drift test; fixtures incl. the TS §33 example.

**Out**
- Include resolution (WU-103); variable resolution (WU-104) — `${…}` strings are stored verbatim.
- Selector evaluation (WU-500), precedence/conflicts (WU-501), semantic validation incl. "at least one operation" (WU-502).
- Plan model/schema (WU-506).

## Deliverables

Namespace `Tailor.Specifications.TransformSpec`. Root `sealed record TransformSpecDocument : SpecificationDocument`, adding `id?`, `description?` ([TS §5.2](../../Requirements/Transformation_Specification.md)). All top-level sections optional in the schema.

| JSON member | Model | TS ref |
|---|---|---|
| `variables` | `IReadOnlyDictionary<string, VariableDeclaration>` (`default?`, `description?`); name pattern `^[A-Za-z_][A-Za-z0-9_]*$` | §28 |
| `input.assert` | `StateAssertions`: `applicationId?`, `version?` (range string), `tfm?` (pattern, e.g. `net8.0*`), `deploymentModel?`, `frameworks[]?` (`name`, `version?` range), `runtimeVersion?`, `rids[]?`, `architectures[]?`, `folderIds[]?`, `plugins[]?`, `assemblies[]?` | §6 |
| `defaults` | `Defaults`: `include?` (`preserve\|exclude`, default `preserve`), `symbols?`, `documentation?`, `cultures?` (`StringList`), `duplicates?`, `policies?` (`IReadOnlyDictionary<string, ConditionPolicy>`, keys from a known condition list, e.g. `selectorMatchesNothing`, `optionalAssociationMissing`, `optimisationFailure`, `packageUpdateUnavailable`, `compatibilityUncertain`, `unknownContent`, `validationWarning`) | §21, §24 |
| `operations` | `Operations` with typed known categories below plus `extensions?` (`IReadOnlyDictionary<string, JsonObject>` keyed by category, schema `additionalProperties` open only here) | §7 |
| `operations.retarget` | `tfm`, `frameworks[]?` (`name`, `version`), `compatibility?` (`strict\|allowKnown\|allowAll` — see Open Questions), `select?` | §11 |
| `operations.patch` | `runtime?` (`version`: `RuntimeVersionPolicy` record `{ policy: matchSource\|latestPatch\|exact\|range, value? }`), `libraries[]?` (`package`, `allow` `exact\|patch\|minor\|range`, `version?`, `range?` — modelled as a separate `LibraryVersionPolicy` type), `sources[]?` | §12 |
| `operations.deploymentModel` | `target` `frameworkDependent\|selfContained`, `runtimeVersion?` (`RuntimeVersionPolicy`), `rid?`, `architecture?`, `os?` | §13 |
| `operations.optimisation.readyToRun` | `select?`, `onFailure?` (`ConditionPolicy`), `composite?` (bool), `inputBubble?` (bool) | §14 |
| `rules[]` | `Rule`: `id` (required), `action` `include\|exclude`, `select?` (absent = whole app), `associations?` `primaryOnly\|selected\|group` (+ `associationTypes[]?`), `exception?` (bool → precedence level 5) | §9, §10, §15, §20, §22 |
| `symbols` | `policy` `preserve\|exclude\|separate`, `select?`, `output?` (`format` `directory\|zip`, `path?`) | §16 |
| `documentation` | `policy` `preserve\|exclude`, `select?` | §17 |
| `layout[]` | `LayoutRule`: `id`, `select`, `destination` (root-relative folder), `flatten?` | §18 |
| `additions[]` | `AdditionRule`: `id`, `source` (`package` `{id, version?}` + `path` \| `file` spec-relative path), `destination` | §19 |
| `sources[]` | `PackageSource`: `name?` \| `url?` (no credential members) | §29 |
| `output.assert` | `StateAssertions` plus `absentCultures[]?`, `readyToRun?` (`select`, `state` `compiled\|notCompiled`), `symbols?` | §25 |

`Selector` (single record; properties in one object are AND-ed): `all[]?`, `any[]?`, `not?`, `folderId?`, `folderRole?`, `classification?`, `association?`, `assemblyRole?`, `plugin?`, `frameworkRole?`, `rid?`, `culture?`, `tfm?`, `name?`, `path?` — each predicate a `StringList` (string or array) ([§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions), [TS §8](../../Requirements/Transformation_Specification.md)). Empty object `{}` is invalid; absent means whole app ([TS §8.5](../../Requirements/Transformation_Specification.md)).

Per-document structural checks (`RPK1200`–`1299`): duplicate `rules[].id` / `layout[].id` / `additions[].id`; empty selector object; `not` with zero predicates; library entry `allow: range` without `range`, `exact` without `version`; `sources[]` entry with both or neither of `name`/`url`, or a `url` containing userinfo (`user:pass@`) → structural error; `layout.destination`/`additions.destination` failing `RelativePath`; invalid variable name.

Schema: `schemas/transformspec/v1/transformspec.schema.json`, same generator and post-processing as WU-101; `kind` const `TransformSpec`; `TransformSpecFormat.Instance` supports `1.0`.

Fixtures (`Fixtures/TransformSpec/`): `minimal`, `architecture-example` ([§6.3](../../Architecture/Tailor.architecture.md#63-transformspec-shape-illustrative-the-wu-102-schema-is-normative)), `ts33-conceptual-example` (every bullet of [TS §33](../../Requirements/Transformation_Specification.md) expressed: retarget `net10.0-windows`, SC `win-x64`, `latestPatch` runtime, selected library with `allow`, preserve app/plugin assemblies, exclude other-RID runtime assets, keep `en`/`en-*`, preserve associated config, exclude XML docs, symbols `separate`, R2R on app+plugin roles except an excluded name, catch-all preserve), `commented`, plus invalid: `unknown-member`, `wrong-kind`, `major-2`, `credential-in-url`, `duplicate-rule-id`, `empty-selector`, `unknown-policy-condition`.

## Design Notes

- Reuse `SpecificationReader`/`Writer`/`SchemaGenerator` from WU-101; do not fork them.
- Keep `${name}` strings verbatim; therefore members that may hold variables must be `string`-typed in the model (TFM, versions, RID, package version). Enum-typed members cannot carry variables — see Open Questions.
- `operations.extensions` is the only open-schema point; everything else is `additionalProperties: false` ([TS §32](../../Requirements/Transformation_Specification.md) item 17 vs strict schema).
- No credential-shaped members anywhere in the model ([§17](../../Architecture/Tailor.architecture.md#17-security)).
- Runtime and library version policies are separate types ([§10](../../Architecture/Tailor.architecture.md#10-acquisition)). Each exposes a pure mapping onto a NuGet range string per the architecture §10 table: `RuntimeVersionPolicy.ToRangeString(string sourceVersion, string targetMajorMinor)` and `LibraryVersionPolicy.ToRangeString(string currentVersion)` (semantic-version arithmetic only; no NuGet package reference). Handlers (WU-803/804/900/901/902) parse the result and call the WU-700 resolver. There is no implicit runtime version: the model allows `runtimeVersion` to be absent, and the owning handlers report a missing value as a validation error when their operation changes the runtime.
- Canonical output and round-trip semantics as WU-101.

## Acceptance Criteria

- [ ] AC-1 The model covers every row of the member table; a reflection test asserts each JSON member name exists in the generated schema.
- [ ] AC-2 Round-trip (read → write → read) over all valid fixtures is lossless and the second write is byte-identical (golden file per fixture).
- [ ] AC-3 `ts33-conceptual-example.transform.json` is schema-valid; its golden file contains a rule or operation for each of the 12 TS §33 bullets (checklist test mapping bullet → JSON pointer).
- [ ] AC-4 `${targetTfm}` values survive round-trip unchanged.
- [ ] AC-5 `wrong-kind` → `RPK1003`; `major-2` → `RPK1005`; `unknown-member` → `RPK1010` with a JSON pointer (e.g. `/rules/0/selct`).
- [ ] AC-6 `credential-in-url`, `duplicate-rule-id`, `empty-selector` each yield a distinct `RPK12xx` error with a JSON pointer; `unknown-policy-condition` yields a schema or `RPK12xx` error.
- [ ] AC-7 `SchemaDriftTests` fails when `schemas/transformspec/v1/transformspec.schema.json` differs from the generated schema (same update switch as WU-101).
- [ ] AC-8 The committed schema has `kind` const `TransformSpec`, `additionalProperties: false` everywhere except `operations.extensions` values, and `oneOf` string/array for selector predicates.
- [ ] AC-9 An `operations.extensions["optimisation.pgo"]` object round-trips without schema errors.
- [ ] AC-10 Reading any fixture never throws.
- [ ] AC-11 `RuntimeVersionPolicy.ToRangeString` and `LibraryVersionPolicy.ToRangeString` produce the architecture §10 ranges for every policy (theory test, e.g. `latestPatch` with target `8.0` → `[8.0.0, 8.1.0)`, library `patch` from `13.0.1` → `[13.0.1, 13.1.0)`).

## Test Requirements

- xUnit v3 + golden files (`Tailor.Testing.Golden`) in `tests/Tailor.Specifications.Tests/`, namespace `…Tests.TransformSpec`. Fixtures copied to output. Trait `WU=102`.
- Run: `dotnet test --project tests/Tailor.Specifications.Tests --filter-trait "WU=102"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green; `dotnet format --verify-no-changes` clean.
- Schema committed; drift test passing.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- `Specifications.Common` is delivered by WU-101. If WU-101 is not merged, start with the TransformSpec model types and fixtures, then rebase onto WU-101's `Common/` before wiring the reader and schema; do not create a second copy.
- Use the selector vocabulary from [§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions) exactly; no new predicates.
- The TS §33 fixture is reused by WU-903 and the WU-1003 template; keep it realistic.

## Open Questions

- Compatibility policy values for `retarget.compatibility` are not defined in TS/architecture; proposed `strict|allowKnown|allowAll`.
- Whether enum-typed members (e.g. `deploymentModel.target`, `libraries[].allow`) must accept `${var}`; the proposal is no (TS §28.2 lists only TFM, runtime version, RID, app version, package version policy — the last is ambiguous).
- Final list of policy condition keys ([TS §24.2](../../Requirements/Transformation_Specification.md)); the list above is provisional and may be extended by later WUs (minor schema bump).

## Test Evidence

_To be completed by the implementer._
