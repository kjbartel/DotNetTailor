# WU-104 transformspec-variables

| Field | Value |
|---|---|
| ID | WU-104 |
| Title | transformspec-variables |
| Milestone | M1 Core Primitives & Specification Documents |
| Status | Not started |
| Depends on | WU-102, WU-103 |
| Parallel with | WU-105, M2, M3 |
| Target project(s)/paths | `src/Tailor.Specifications/Variables/`, `src/Tailor.Specifications/TransformSpec/` (attribute annotations only), `tests/Tailor.Specifications.Tests/Variables/` |
| Size | S |

## Goal

Resolve declarative `${name}` parameters in an effective TransformSpec from CLI values and document defaults, so that planning only ever sees fully resolved values; any unresolved or undeclared variable is an error.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Parameters, constraints, resolution | [TS §28](../../Requirements/Transformation_Specification.md), [TS §32](../../Requirements/Transformation_Specification.md) (5, 12) | [§6.3](../../Architecture/Tailor.architecture.md#63-transformspec-shape-illustrative-the-wu-102-schema-is-normative) (variables bullet) |
| CLI `--var` | [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§14](../../Architecture/Tailor.architecture.md#14-cli) |
| Determinism / provenance | [TS §3.6](../../Requirements/Transformation_Specification.md), [TS §29.3](../../Requirements/Transformation_Specification.md) | [§15](../../Architecture/Tailor.architecture.md#15-determinism) |

## Scope

**In**
- `${name}` detection, escaping, substitution in variable-capable string members of `TransformSpecDocument`.
- Sources in precedence order: CLI values (`--var`, passed in as a dictionary) > document default (after WU-103 merge).
- Resolution record (name, value, source) for later inclusion in the plan (WU-503).

**Out**
- Parsing `--var` from the command line (WU-105 supplies the dictionary).
- Environment variables / config file as sources (WU-1000).
- Variables in AppSpec (not supported; `${` is literal there).
- Any expression, function, conditional or nested evaluation ([TS §28.3](../../Requirements/Transformation_Specification.md)).

## Deliverables

Namespace `Tailor.Specifications.Variables`.

| Type | API / responsibility |
|---|---|
| `[AttributeUsage(Property)] sealed class VariableSubstitutionAttribute` | Marks string members that may contain `${…}` (applied in `TransformSpec` model: TFM, framework/runtime/package version strings, RID, `input/output.assert` version and TFM patterns, `application version` assertions) |
| `sealed record VariableValue(string Name, string Value, VariableSource Source)` | `enum VariableSource { CommandLine, DocumentDefault }` |
| `sealed class VariableResolver` | `Result<ResolvedTransformSpec> Resolve(LoadedSpecification<TransformSpecDocument>, IReadOnlyDictionary<string, string> commandLineValues)` |
| `sealed record ResolvedTransformSpec` | `Document` (no remaining `${`), `Variables` (sorted by name, ordinal) |
| `static VariableSyntax` | `IsValidName`, tokenizer for `${name}` and escape `$${` |
| `static VariableDiagnostics` | `TLR1400`–`TLR1499` |

Diagnostics:

| Code | Condition |
|---|---|
| `TLR1401` | Reference to an undeclared variable (location = member pointer via `ProvenanceMap`) |
| `TLR1402` | Declared variable with no default and no CLI value (unresolved) |
| `TLR1403` | CLI value for an undeclared variable |
| `TLR1404` | Malformed reference (`${`, `${}`, `${1x}`, `${a.b}`) |
| `TLR1405` | `${…}` in a member not marked `[VariableSubstitution]` |

## Design Notes

- Syntax: `${name}` with name `^[A-Za-z_][A-Za-z0-9_]*$`; multiple references and surrounding text allowed (`net${major}.0-windows`); `$${` emits a literal `${`.
- Single pass, no recursion: substituted values and defaults are inserted literally and are not re-scanned, so cycles are impossible.
- Precedence: CLI > default ([§6.3](../../Architecture/Tailor.architecture.md#63-transformspec-shape-illustrative-the-wu-102-schema-is-normative)). Defaults from included documents arrive already merged by WU-103.
- All diagnostics are errors and are reported together (not first-only); variable errors are structural ("invalid specification", [TS §24.4](../../Requirements/Transformation_Specification.md)).
- Substitution walks the typed model via the attribute (reflection cached per type) or a hand-written visitor; either way the result is a new immutable document.
- Resolution happens before any planning ([TS §28.4](../../Requirements/Transformation_Specification.md)); WU-502 calls the resolver.

## Acceptance Criteria

- [ ] AC-1 `${targetTfm}` with default `net10.0-windows` and no CLI value resolves to `net10.0-windows`, source `DocumentDefault`.
- [ ] AC-2 CLI value `targetTfm=net9.0-windows` overrides the default; source `CommandLine`.
- [ ] AC-3 Declared variable with no default and no CLI value → `TLR1402` naming the variable and the member pointer where it is used.
- [ ] AC-4 Use of undeclared `${foo}` → `TLR1401` naming `foo`; CLI value for undeclared `bar` → `TLR1403`.
- [ ] AC-5 Malformed references (`${`, `${}`, `${1x}`, `${a.b}`) → `TLR1404` each.
- [ ] AC-6 `net${major}.0` with `major=8` → `net8.0`; `$${literal}` → `${literal}`; a value containing `${x}` is inserted literally and not re-expanded.
- [ ] AC-7 `${x}` in a non-annotated member (e.g. rule `id`) → `TLR1405`.
- [ ] AC-8 After successful resolution no annotated member contains an unescaped `${` (property test over fixtures).
- [ ] AC-9 A variable default declared in an included document is overridden by the root document's default (multi-document fixture via WU-103).
- [ ] AC-10 `ResolvedTransformSpec.Variables` is sorted ordinally and identical across two runs.
- [ ] AC-11 Multiple errors in one document are all reported in one result.

## Test Requirements

- xUnit v3; synthetic in-memory TransformSpecs via `InMemoryDocumentSource`; reuse `ts33-conceptual-example` with a `targetTfm` variable. Trait `WU=104`.
- Run: `dotnet test --project tests/Tailor.Specifications.Tests --filter-trait "WU=104"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green; `dotnet format --verify-no-changes` clean.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- Annotating WU-102 model members is expected; keep schema output unchanged (attributes do not affect `JsonSchemaExporter`). The WU-102 drift test must still pass.
- Do not add environment-variable lookup here; WU-1000 will add sources through the same dictionary input.

## Open Questions

- Should an unused CLI `--var` (declared but unreferenced) warn? Proposed: no diagnostic.
- Should enum-typed members accept variables (see WU-102 Open Questions)? This spec assumes no.
- Should `TLR1403` (CLI value for undeclared variable) be an error or a warning? Proposed: error, to catch typos.

## Test Evidence

_To be completed by the implementer._
