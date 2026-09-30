# WU-500 selector-engine

| Field | Value |
|---|---|
| ID | WU-500 |
| Title | selector-engine |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-305, WU-102 |
| Parallel with | WU-400–WU-403 |
| Target project(s)/paths | `src/Tailor.Planning/Selectors/`, `tests/Tailor.Planning.Tests/Selectors/`, `tests/Tailor.IntegrationTests/Planning/Selectors/` |
| Size | M |
| Branch / PR | `wu/500-selector-engine` / `WU-500: selector-engine` |

## Goal

Evaluate WU-102 `Selector` trees (`all`/`any`/`not` + semantic predicates + physical globs) against a set of artefacts from the Effective Application Model, deterministically and without side effects. Report which selectors match nothing.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §8](../../Requirements/Transformation_Specification.md#8-selectors) (8.2–8.5) | Semantic predicates, physical globs, `all`/`any`/`not`, explicit default scope |
| [TS §3.2](../../Requirements/Transformation_Specification.md#3-design-principles), [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) items 3, 4, 7 | Rule based, application-aware, deterministic |
| [TS §24.2](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies) | "Requested selector matches nothing" is a reportable condition |
| [AS §10](../../Requirements/Application_Specification.md#10-folder-model), [AS §11](../../Requirements/Application_Specification.md#11-file-classification-model), [AS §12](../../Requirements/Application_Specification.md#12-file-associations-and-semantic-groups), [AS §16](../../Requirements/Application_Specification.md#16-plugin-model) | Semantic facts the predicates read |
| Architecture [§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions), [§7](../../Architecture/Tailor.architecture.md#7-effective-application-model-semantics), [§15](../../Architecture/Tailor.architecture.md#15-determinism) | Grammar, EAM facts, ordering |

## Scope

**In**
- `ISelectableArtefactSet` abstraction and an adapter over `EffectiveApplicationModel` (WU-305). WU-503 adds a second adapter for projected state.
- Compilation of `Selector` → `CompiledSelector` (globs pre-compiled; values validated).
- Evaluation of every predicate in the table below, three-valued logic, default scope.
- `SelectorMatchReport` (matches per selector, "matches nothing" flag).

**Out**
- Precedence levels and conflicts (WU-501). Policy for "matches nothing" (WU-502 policy set, applied by WU-503). Semantic checks that predicate values exist in the AppSpec (WU-502).

## Deliverables

Namespace `Tailor.Planning.Selectors`.

| Type | API / responsibility |
|---|---|
| `ISelectableArtefact` | `Path` (`RelativePath`), `FolderChain` (effective folder nodes from the file's folder up to root: id, role), `Classification`, `AssociationType?` + `PrimaryPath?`, `AssemblyRole?`, `PluginId?`, `FrameworkRole`, `RidContext?`, `Culture?`, `Tfm?`, `IsManagedAssembly` |
| `ISelectableArtefactSet` | `Artefacts` (sorted by `PathPolicy` comparer), `TryGet(RelativePath)` |
| `EffectiveModelArtefactSet` | Adapter over `EffectiveApplicationModel`; sidecars excluded |
| `SelectorCompiler.Compile(Selector?, SelectorCompileContext)` → `Result<CompiledSelector>` | `null` → `CompiledSelector.WholeApplication`; invalid glob or culture/RID syntax → `RPK50xx` with JSON pointer |
| `CompiledSelector.Evaluate(ISelectableArtefact)` → `TriState` | `True`, `False`, `Unknown` |
| `SelectorEngine.Select(CompiledSelector, ISelectableArtefactSet)` → `SelectionResult` | `Matches` (sorted), `MatchesNothing` |
| `SelectorMatchReport` | Per selector location (document + JSON pointer): match count, `MatchesNothing` |
| `SelectorDiagnostics` | `RPK5001`–`RPK5099` |

**Predicates** (each value is a `StringList`. Values in one list are OR-ed. Predicates in one object are AND-ed.)

| Predicate | Fact | Value semantics | Undefined for → `Unknown` |
|---|---|---|---|
| `folderId` | `FolderChain` ids | Exact, ordinal-ignore-case. Matches the file's folder node **or any ancestor** ("under" semantics) | — (always defined) |
| `folderRole` | `FolderChain` roles | Same "under" semantics | — |
| `classification` | Primary classification group id | Exact | — |
| `association` | Association type of the file as an associated file (`symbols`, `xmlDoc`, `config`, `runtimeConfig`, `depsJson`, `satelliteResource`, …) | Exact. Primaries evaluate to `False` | — |
| `assemblyRole` | `application` \| `plugin` \| `framework` (WU-305 role) | Exact | Non-assemblies |
| `plugin` | Plugin unit id (WU-304) of the nearest containing plugin | Glob over the unit id (e.g. `Plugins/*`). Files outside plugins evaluate to `False` | — |
| `frameworkRole` | `application` \| `framework` \| `host` (see Open Questions) | Exact | — |
| `rid` | RID context from `<rid>` folder tokens | A value `R` matches when the artefact's RID context is in the RID-graph compatibility closure of `R` (WU-203). Example: `win-x64` matches `win-x64`, `win` and `any` | Files without RID context |
| `culture` | Culture context from `<culture>` folders or satellite facts | Exact or `*` glob (`en`, `en-*`), case-insensitive | Invariant/neutral files |
| `tfm` | `TargetFrameworkAttribute` TFM of a managed assembly | Glob (`net8.0*`) | Non-assemblies |
| `name` | File name | Glob, case-insensitive (`Legacy.*`) | — |
| `path` | Root-relative path | Microsoft.Extensions.FileSystemGlobbing semantics (`**`), `/` separators | — |

## Design Notes

- **Three-valued logic (Kleene).** `not(Unknown) = Unknown`. `all`: any `False` → `False`, else any `Unknown` → `Unknown`, else `True`. `any`: any `True` → `True`, else any `Unknown` → `Unknown`, else `False`. Only `True` selects. This lets `{ "not": { "culture": ["en", "en-*"] }, "classification": "resource" }` (architecture §6.3) leave neutral resources untouched, and lets `{ "not": { "rid": "win-x64" } }` touch only RID-specific assets.
- **Default scope.** An absent `select` compiles to `WholeApplication`, which matches every in-scope artefact. This is explicit in the API and in the WU-102 schema description ([TS §8.5](../../Requirements/Transformation_Specification.md#8-selectors)). `{}` is already rejected by WU-102.
- The engine evaluates over **files**. Folder semantics are reached through `FolderChain`. Folder-level exclusion is expressed as "all files under folder X".
- Evaluation is pure. It never reads file content, because all facts come from the EAM. It is thread-safe, and results are sorted with the `PathPolicy` comparer.
- Globs and value lists are compiled once per selector. Evaluation cost is O(artefacts × predicates).
- Architecture §8 lists `name`. The user list for this WU does not, but `name` is in the WU-102 model, so it is implemented.

## Acceptance Criteria

- [ ] AC-1 Every predicate in the table has a theory test over a synthetic `InMemoryAppTree` EAM that covers match, non-match and (where applicable) `Unknown`.
- [ ] AC-2 `all`/`any`/`not` follow the Kleene truth tables (exhaustive table test over `True`/`False`/`Unknown`).
- [ ] AC-3 The TS §8.4 example "all managed assemblies under plugin folders except `Legacy.*`" (`all: [{folderRole: plugin}, {classification: managed}, {not: {name: "Legacy.*"}}]`) selects exactly the expected files on the plugin synthetic tree, including nested plugin folders.
- [ ] AC-4 `not: {culture: ["en", "en-*"]}` with `classification: resource` selects `de/*` and `fr/*` satellites only. Invariant resources are not selected.
- [ ] AC-5 `not: {rid: "win-x64"}` on the `ConsoleApp/<tfm>-fdportable-il` matrix variant selects only `runtimes/<rid>/…` files whose RID is not in the `win-x64` compatibility closure.
- [ ] AC-6 An absent selector selects every in-scope artefact and no sidecar.
- [ ] AC-7 A selector that matches nothing sets `MatchesNothing = true` and appears in `SelectorMatchReport` with its document and JSON pointer. The engine emits no diagnostic itself.
- [ ] AC-8 Invalid glob, invalid culture pattern and invalid RID value each produce a distinct `RPK50xx` error with a JSON pointer and no exception.
- [ ] AC-9 Results are identical for shuffled artefact input order (permutation test) and across two runs (golden file of `SelectorMatchReport` for the TS §33 fixture over a matrix PluginHost entry).
- [ ] AC-10 The engine performs no filesystem access: a fake `IAppTree` that throws on `OpenRead` runs the full suite green.

## Test Requirements

- Unit: `tests/Tailor.Planning.Tests/Selectors/`. Synthetic EAMs built from `InMemoryAppTree` + fake facts providers (WU-300/WU-301). Trait `WU=500`.
- Integration: `tests/Tailor.IntegrationTests/Planning/Selectors/` over matrix copies with WU-305 hand-authored AppSpecs. Traits `Category=Integration`, `Category=Matrix`, `WU=500`.
- Run: `dotnet test --project tests/Tailor.Planning.Tests --filter-trait "WU=500"` and `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=500"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, `dotnet format --verify-no-changes` clean.
- All ACs ticked by the Verifier. Plan status `Done`. `RPK50xx` codes listed for WU-1001.
- Changes are limited to the target paths, plus the plan status row.

## Agent Notes

- `Tailor.Planning` may reference only Validation (and through it Model, Specifications, Core), per architecture §3.1.
- Keep `ISelectableArtefact` free of EAM types so that WU-503's projected-state adapter can implement it.
- Reuse WU-203 `IRidKnowledge`/`ICultureKnowledge` for RID closure and culture validation. Do not duplicate the RID graph.

## Open Questions

- `frameworkRole` vocabulary is not defined in TS, the architecture or WU-102. Proposed: `application` (app-owned), `framework` (`platformManaged`/`platformNative` or catalogue-owned), `host` (apphost, `hostfxr`, `hostpolicy`).
- `folderId`/`folderRole` "under" semantics (ancestor match) vs exact-node match. "Under" is proposed to satisfy TS §8.4. It also means `folderRole: applicationRoot` matches every file.
- RID-compatibility matching for `rid` (value matches its compatibility closure) is a proposal. The alternative is exact/glob matching, plus a dedicated "other-RID" construct.
- Should `association` also match primaries by the association types they own (group semantics)? Proposed: no. Group expansion belongs to WU-504.

## Test Evidence

_To be completed by the implementer._
