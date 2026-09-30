# WU-301: file-classification-engine

| Field | Value |
|---|---|
| ID | WU-301 |
| Title | file-classification-engine |
| Milestone | M3 Effective Application Model |
| Status | Not started |
| Depends on | WU-300, WU-200 |
| Parallel with | WU-201, WU-202, WU-600 |
| Target project(s)/paths | `src/DotNetRepack.Model/Classification/`, `tests/DotNetRepack.Model.Tests/Classification/`, `tests/DotNetRepack.IntegrationTests/Model/` |
| Size | M |
| Branch / PR | `wu/301-file-classification-engine` / `WU-301: file-classification-engine` |

## Goal

Assign exactly one primary classification to every in-scope file, using per-folder applicable classification groups, glob + binary-inspection matchers, explicit priority and a mandatory effective catch-all. Unknown files are never dropped.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §11.2](../../Requirements/Application_Specification.md#112-classification-groups)–[§11.6](../../Requirements/Application_Specification.md#116-catch-all-classification) | Groups, matching, precedence, wildcard preference, catch-all |
| [AS §3.3](../../Requirements/Application_Specification.md#33-complete-coverage), [AS §3.4](../../Requirements/Application_Specification.md#34-catch-all-behaviour), [AS §24](../../Requirements/Application_Specification.md#24-global-invariants) items 3, 7 | Complete coverage, no silent omissions |
| [AS §14.3](../../Requirements/Application_Specification.md#143-classification) | `.dll` is not assumed managed |
| [RD §4](../../Requirements/R2R_tool_Design.md#4-file-classification-model) | Every file classified exactly once |
| Architecture [§7.2, §7.4](../../Architecture/DotNetRepack.architecture.md#7-effective-application-model-semantics) | Matchers, predicates, vocabulary, inspection facts |
| Plan M3 criteria 3 (classification ties), 5 (coverage) | AC-5, AC-9 |

## Scope

**In**
- Applicable-group resolution per `MatchedFolder`; built-in group vocabulary with default definitions.
- Matchers: glob list, inspection predicates (`managed`, `native`, `mixed`, `r2r`, `satellite`, `referenceAssembly`), explicit exceptions (`exclude` globs).
- Priority ordering, tie errors, catch-all resolution, lazy cached file facts.

**Out**
- Associations (WU-302), identities/references (WU-303), heuristic group selection (WU-401).
- New inspection logic (WU-200/WU-201 own PE/metadata facts).

## Deliverables

| Item | Detail |
|---|---|
| `DotNetRepack.Model.Classification.IFileFactsProvider` | Lazy, cached per path; wraps WU-200 PE facts (and WU-201 satellite/reference-assembly facts when available) over `IAppTree.OpenRead` |
| `BuiltInClassificationGroups` | Default definitions for `managed`, `platformManaged`, `native`, `platformNative`, `config`, `resource`, `symbols`, `xmlDoc`, `content`. A spec group with the same id replaces the built-in |
| `ClassificationEngine.Classify(AppSpec, FolderMatchResult, IFileFactsProvider)` → `ClassificationResult` | `Files` (sorted by path), `Diagnostics` |
| `ClassifiedFile` | `Path`, `FolderPath`, `FolderDefinitionId`, `GroupId`, `IsCatchAll`, `Status` (`Classified`, `Ambiguous`, `Unclassified`), `MatchedGroupIds` (for diagnostics) |
| Diagnostic codes (proposed, `RPK31xx`) | `RPK3101` classification tie, `RPK3102` no effective catch-all, `RPK3103` unknown group id, `RPK3104` inspection failed for predicate (warning, predicate evaluates false), `RPK3105` invalid matcher |

## Design Notes

- Applicable groups for a folder: the folder definition's group list if present, else all global groups ([AS §10.2](../../Requirements/Application_Specification.md#102-folder-definitions); cf. FS `file_types`).
- Effective catch-all: folder-level → global `classifications.catchAll` → built-in `content` **only when the property is absent**. An explicit `null`/empty value or an id that does not resolve → `RPK3102` (enables the WU-405 "missing catch-all" scenario).
- Evaluation per file: candidates = applicable groups whose glob matches and whose `exclude` does not; evaluate predicates only for glob-matched candidates (lazy inspection). Highest `priority` wins. Two or more matching groups at the top priority → `RPK3101` naming file and group ids; file `Status = Ambiguous`.
- No group matches and catch-all missing → `Status = Unclassified`, file stays in the result (never dropped) with `RPK3102`.
- Malformed PE: surface the WU-200 diagnostic, emit `RPK3104`, treat predicate as false.
- Sidecars are already excluded by WU-300; files in folders with `RPK3002`/`RPK3006` are not classified and are listed via those diagnostics.

## Acceptance Criteria

- [ ] AC-1 Each in-scope file of a synthetic tree appears exactly once in `ClassificationResult.Files` (property test over randomised synthetic trees with a fixed seed).
- [ ] AC-2 A native `foo.dll` and a managed `bar.dll` in the same folder classify as `native` and `managed` respectively, via predicate, not extension.
- [ ] AC-3 Higher `priority` wins deterministically regardless of document order (test permutes group order).
- [ ] AC-4 A folder-level group list restricts applicable groups (e.g. `runtimes` folder yields `platformManaged`/`platformNative`).
- [ ] AC-5 Two matching groups with equal priority produce `RPK3101`; the file is retained with `Status = Ambiguous`.
- [ ] AC-6 Files matching no group fall to the effective catch-all with `IsCatchAll = true`.
- [ ] AC-7 Explicit `catchAll: null` or an unresolvable catch-all id yields `RPK3102`; unmatched files remain with `Status = Unclassified`.
- [ ] AC-8 A truncated PE fixture yields `RPK3104` (warning) and classification continues without exceptions.
- [ ] AC-9 Integration coverage test: for every matrix app with a minimal root-recurse AppSpec, every file has exactly one primary classification and `Unclassified`/`Ambiguous` count is 0.
- [ ] AC-10 Inspection is invoked only for files whose glob matched a predicate-bearing group (test with counting fake provider).

## Test Requirements

- Unit: `tests/DotNetRepack.Model.Tests/Classification/`, synthetic `InMemoryAppTree` with fake `IFileFactsProvider`; small real PE fixture bytes for AC-2/AC-8. Trait `WU=301`.
- Integration: `tests/DotNetRepack.IntegrationTests/Model/ClassificationCoverageTests` over `artifacts/testapps/manifest.json` (WU-003); trait `Category=Integration`, `Category=Matrix`. Skip with an explicit reason locally when the matrix is absent; CI must not skip.
- Run: `dotnet test --project tests/DotNetRepack.Model.Tests --filter-trait "WU=301"` and `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=301"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green.
- Codes added to the Model diagnostics catalogue.
- Changes limited to target paths (plus the plan status row).

## Agent Notes

- Keep `IFileFactsProvider` in Model so WU-303/WU-400 reuse its cache; do not re-open files per stage.
- Built-in group defaults are data (a static table), not branching logic.

## Open Questions

- Does the WU-101 schema allow per-folder group lists and explicit `catchAll: null`? If not, AC-4/AC-7 need a schema change via WU-101 (schema-change skill).
- Built-in default group definitions overridable by id (provisional) vs. must always be declared in the spec.
