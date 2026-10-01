# WU-103 multi-document-loader

| Field | Value |
|---|---|
| ID | WU-103 |
| Title | multi-document-loader |
| Milestone | M1 Core Primitives & Specification Documents |
| Status | Not started |
| Depends on | WU-101, WU-102 |
| Parallel with | WU-105, M2, WU-300 |
| Target project(s)/paths | `src/Tailor.Specifications/Loading/`, `tests/Tailor.Specifications.Tests/Loading/`, `tests/Tailor.Specifications.Tests/Fixtures/Includes/` |
| Size | M |

## Goal

Load a root specification plus its `includes[]` into one effective document with deterministic precedence, cycle detection and per-member provenance, using one kind-agnostic implementation for AppSpec and TransformSpec.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Multi-document AppSpec | [AS §4](../../Requirements/Application_Specification.md), [AS §21](../../Requirements/Application_Specification.md), [AS §24](../../Requirements/Application_Specification.md) (10) | [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules) |
| Multi-document TransformSpec | [TS §4](../../Requirements/Transformation_Specification.md), [TS §27](../../Requirements/Transformation_Specification.md), [TS §22.5](../../Requirements/Transformation_Specification.md) | [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules), [§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions) (provenance: document) |
| Independent schema versions | [AS §5.3](../../Requirements/Application_Specification.md), [TS §5.3](../../Requirements/Transformation_Specification.md) | [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules) |
| Portability | [AS §23.3](../../Requirements/Application_Specification.md) | [§17](../../Architecture/Tailor.architecture.md#17-security) |

## Scope

**In**
- Include path resolution, depth-first ordered traversal, cycle detection, kind/version checks per document.
- DOM-level merge with precedence and keyed-member replacement; deserialisation of the merged DOM into the typed model.
- Provenance map (effective JSON pointer → source document + pointer).
- Abstraction over document source for in-memory tests.

**Out**
- Variable resolution (WU-104).
- Semantic validation of the effective document (WU-403, WU-502).
- Tool config file discovery (WU-1000).

## Deliverables

Namespace `Tailor.Specifications.Loading`.

| Type | API / responsibility |
|---|---|
| `interface IDocumentSource` | `bool Exists(string fullPath)`, `ReadOnlyMemory<byte> Read(string fullPath)`; `FileSystemDocumentSource`, test-only `InMemoryDocumentSource` |
| `sealed class SpecificationLoader` | `Result<LoadedSpecification<TDocument>> Load<TDocument>(string rootPath, SpecificationFormat<TDocument> format, IDocumentSource source)` |
| `sealed record LoadedSpecification<TDocument>` | `Effective` (typed, `Includes` empty), `Documents` (ordered `LoadedDocument` list: display name, full path, schema version, depth), `Provenance` |
| `sealed class ProvenanceMap` | `SourceLocation Resolve(JsonPointer effectivePointer)` → `(DocumentDisplayName, JsonPointer)`; falls back to nearest ancestor pointer |
| `static IncludeMerger` | DOM merge (internal, unit-tested) |
| `static LoadingDiagnostics` | `TLR1300`–`TLR1399` |

Diagnostics (minimum):

| Code | Condition | Severity |
|---|---|---|
| `TLR1301` | Include file not found | Error |
| `TLR1302` | Include cycle; message lists the chain `a.json → b.json → a.json` | Error (structural) |
| `TLR1303` | Include kind differs from root kind | Error |
| `TLR1304` | Include path absolute, URL, or empty | Error |
| `TLR1305` | Duplicate `id` within one keyed array of one document (if not already `TLR11xx`/`TLR12xx`) | Error |
| `TLR1306` | Include major version differs from root major version | Error |

## Design Notes

- **Path resolution**: each `includes[]` entry is relative to the **including file's directory** (architecture [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules)); `\` and `/` accepted; `..` allowed (shared org documents, [AS §21.4](../../Requirements/Application_Specification.md)); absolute paths and URLs rejected ([AS §23.3](../../Requirements/Application_Specification.md)).
- **Identity** for cycle detection and de-duplication: `Path.GetFullPath` result compared with `StringComparer.OrdinalIgnoreCase` (Windows policy). Diamond includes (same file reached twice without a cycle) are allowed and loaded once at the position of first occurrence.
- **Display names** in diagnostics/provenance are paths relative to the root document's directory with `/` separators (no machine paths in artefacts, [§15](../../Architecture/Tailor.architecture.md#15-determinism)).
- **Per-document read**: every document goes through WU-101's `SpecificationReader` stages 1–3 and 5 (parse, header incl. version, schema, structural). Version is checked per document against the format; AppSpec and TransformSpec versions are independent by construction.
- **Precedence** ([§6.1](../../Architecture/Tailor.architecture.md#61-common-rules)): depth-first, ordered. Merge order low → high: `include[0]` subtree, `include[1]` subtree, …, including document. Within the fold:
  - Objects merge member-wise recursively; higher precedence scalar wins.
  - Arrays whose elements are all objects with a string `id` are **keyed**: items merge by `id` (ordinal); a higher-precedence item replaces the lower one entirely and keeps the lower item's position; new ids are appended in document order.
  - All other arrays (e.g. `rids`, `glob`, `includes`) are replaced wholesale by the higher-precedence value.
  - Dictionaries (`variables`, `defaults.policies`) merge by key; higher precedence wins.
- Document order never silently resolves a same-level conflict: duplicates inside one document are errors ([TS §22.5](../../Requirements/Transformation_Specification.md)).
- The merged DOM is deserialised with the format's options; `$schema`, `kind`, `schemaVersion`, `generator` come from the root.
- The loader is generic over `TDocument : SpecificationDocument`; no kind-specific branching.

## Acceptance Criteria

- [ ] AC-1 A single root document without `includes` loads to an effective model equal to WU-101/WU-102's single-document read.
- [ ] AC-2 Include paths resolve relative to the including file (fixture `a/root.json` includes `shared/x.json` which includes `../y.json`); resolved documents appear in `Documents` in depth-first order.
- [ ] AC-3 A cycle `root → a → b → a` yields `TLR1302` whose message contains `a.json → b.json → a.json` (relative display names) and nothing is deserialised.
- [ ] AC-4 A self-include yields `TLR1302`; a diamond (`root → a, root → b, a → c, b → c`) loads `c` once without error.
- [ ] AC-5 Missing include → `TLR1301` with location = including document + pointer `/includes/<n>`; absolute or URL include → `TLR1304`.
- [ ] AC-6 Precedence: root overrides includes; `include[1]` overrides `include[0]` for keyed items (e.g. `rules` id `no-xml-docs`, `folders.definitions` id `runtimes`); unkeyed arrays are replaced wholesale; covered by theory tests for both kinds.
- [ ] AC-7 Duplicate `id` inside one document's keyed array is an error even when another document defines the same id.
- [ ] AC-8 An AppSpec including a TransformSpec yields `TLR1303`; an include with major `2.0` under a `1.0` root yields `TLR1005` or `TLR1306`.
- [ ] AC-9 `ProvenanceMap.Resolve` returns the originating document and pointer for a merged rule and for a nested member of a replaced keyed item.
- [ ] AC-10 Loading is deterministic: the canonical write of `Effective` is byte-identical across two loads and independent of `IDocumentSource` enumeration order (golden file).
- [ ] AC-11 The same `SpecificationLoader` code path loads both kinds (test with one AppSpec and one TransformSpec multi-document fixture set).

## Test Requirements

- xUnit v3 + golden files (`Tailor.Testing.Golden`); prefer `InMemoryDocumentSource` for unit tests; one on-disk fixture set under `Fixtures/Includes/` to exercise `FileSystemDocumentSource` and relative resolution. Trait `WU=103`.
- Run: `dotnet test --project tests/Tailor.Specifications.Tests --filter-trait "WU=103"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green; `dotnet format --verify-no-changes` clean.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- Keep the merge purely on `JsonNode`; do not add merge logic to model types.
- Cap include depth (e.g. 32) and total documents (e.g. 256) with a diagnostic to bound untrusted input.
- WU-104 relies on `variables` being merged by key here.

## Open Questions

- **Resolved** — include path base: relative to the including file; architecture §6.1 updated (§19 item 23).
- Whether keyed items should be replaced whole (this spec) or merged member-wise.
- Whether `..` in include paths should be confined to some root (e.g. repository) — currently unrestricted except absolute/URL.

## Test Evidence

_To be completed by the implementer._
