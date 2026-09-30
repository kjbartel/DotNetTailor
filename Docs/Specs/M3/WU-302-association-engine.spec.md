# WU-302: association-engine

| Field | Value |
|---|---|
| ID | WU-302 |
| Title | association-engine |
| Milestone | M3 Effective Application Model |
| Status | Not started |
| Depends on | WU-301 |
| Parallel with | WU-303, WU-304 |
| Target project(s)/paths | `src/DotNetRepack.Model/Associations/`, `tests/DotNetRepack.Model.Tests/Associations/` |
| Size | S |
| Branch / PR | `wu/302-association-engine` / `WU-302: association-engine` |

## Goal

Link associated files (symbols, XML docs, `.config`, runtimeconfig, deps.json, satellite resources) to exactly one primary file using name-based rules, and distinguish legitimately absent optional associations from missing required ones.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §12](../../Requirements/Application_Specification.md#12-file-associations-and-semantic-groups) (12.2–12.6) | Primary files, types, naming, semantics, missing files |
| [AS §17.2](../../Requirements/Application_Specification.md#172-classification), [AS §18.2](../../Requirements/Application_Specification.md#182-debug-symbols), [AS §18.3](../../Requirements/Application_Specification.md#183-xml-documentation) | Satellites, PDBs, XML docs |
| [AS §24](../../Requirements/Application_Specification.md#24-global-invariants) item 8 | Explicit associations |
| Architecture [§7.3](../../Architecture/DotNetRepack.architecture.md#7-effective-application-model-semantics) | Normative rule set |
| Plan M4 criterion 3 (missing required association) | AC-5 |

## Scope

**In**: rule model evaluation, tokens `{name}`, `{file}`, `<culture>`, `appliesTo` group filter, required/optional, ownership conflicts, satellite sanity check.

**Out**: transformation semantics over groups (Planning, M5); classification changes (associated files keep their own classification).

## Deliverables

| Item | Detail |
|---|---|
| `DotNetRepack.Model.Associations.AssociationEngine.Associate(AppSpec, ClassificationResult, IFileFactsProvider)` → `AssociationResult` | `Associations`, `AbsentOptional`, `Diagnostics` |
| `Association` | `PrimaryPath`, `AssociatedPath`, `Type` (`symbols`, `xmlDoc`, `config`, `runtimeConfig`, `depsJson`, `satelliteResource`, extensible string), `RuleId`, `Required` |
| `AbsentOptionalAssociation` | `PrimaryPath`, `RuleId` — recorded in the model, not emitted as diagnostic |
| `AssociationPattern` | Parser for `{name}.pdb`, `{name}.xml`, `{file}.config`, `{name}.runtimeconfig.json`, `{name}.deps.json`, `<culture>/{name}.resources.dll`; patterns are relative to the primary's folder and confined to it and its direct children |
| Diagnostic codes (proposed, `RPK32xx`) | `RPK3201` missing required association, `RPK3202` ambiguous owner (equal-priority primaries), `RPK3203` invalid pattern (escapes folder, unknown token), `RPK3204` satellite name matched but file is not a satellite of the primary (warning) |

## Design Notes

- `{name}` = file name without the last extension; `{file}` = full file name. `appliesTo` (group ids) restricts primaries; default: `managed`, `platformManaged`, `native`, `platformNative`.
- An associated file linked to more than one primary: prefer the primary whose group has the higher classification priority (so `App.pdb` binds to managed `App.dll` over native apphost `App.exe`); equal priority → `RPK3202`.
- Satellite rule: `<culture>` resolved via WU-203; verify satellite facts (culture + `*.resources` name) from WU-201 when available, else name only.
- A file may be associated to one primary and be primary for other rules (e.g. satellite with its own PDB).
- Missing optional → `AbsentOptional` entry (architecture "informational"), keeping diagnostics readable.

## Acceptance Criteria

- [ ] AC-1 `App.dll` with `App.pdb`, `App.xml`, `App.dll.config`, `App.runtimeconfig.json`, `App.deps.json`, `de/App.resources.dll` yields six associations with the correct types.
- [ ] AC-2 With apphost `App.exe` (native) and `App.dll` (managed) present, `App.pdb` associates with `App.dll` only.
- [ ] AC-3 Every associated file has exactly one primary (property test over synthetic trees).
- [ ] AC-4 A missing optional associated file produces an `AbsentOptional` entry and no diagnostic.
- [ ] AC-5 A missing required associated file produces `RPK3201` naming primary, rule and expected path.
- [ ] AC-6 Equal-priority competing primaries produce `RPK3202`.
- [ ] AC-7 Patterns containing `..` or absolute segments produce `RPK3203`.
- [ ] AC-8 Associated files retain their WU-301 classification (e.g. `App.pdb` → `symbols`).
- [ ] AC-9 Output ordering is deterministic (Verify snapshot of a synthetic plugin-style tree).

## Test Requirements

- Unit only: `tests/DotNetRepack.Model.Tests/Associations/`, synthetic `InMemoryAppTree` + fake facts; trait `WU=302`.
- Run: `dotnet test --project tests/DotNetRepack.Model.Tests --filter-trait "WU=302"`.
- Matrix coverage is exercised by WU-305 snapshots.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; codes in the Model diagnostics catalogue; changes limited to target paths (plus the plan status row).

## Agent Notes

- Build a per-folder name index once; association lookup must be O(files), not O(files²).

## Open Questions

- Does the WU-101 schema carry `appliesTo` on association rules? If not, use the default primary set and raise a schema change.
- Should `runtimeConfig`/`depsJson` be implicitly required for declared entry-point assemblies? (Provisional: no; WU-403 validates entry points separately.)
