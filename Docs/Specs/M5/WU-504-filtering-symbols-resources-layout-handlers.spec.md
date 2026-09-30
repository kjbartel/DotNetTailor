# WU-504 filtering-symbols-resources-layout-handlers

| Field | Value |
|---|---|
| ID | WU-504 |
| Title | filtering-symbols-resources-layout-handlers |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-503 |
| Parallel with | WU-507, WU-601, WU-702 |
| Target project(s)/paths | `src/DotNetRepack.Transforms/{Filtering,Symbols,Documentation,Resources,Layout,Common}/`, `tests/DotNetRepack.Transforms.Tests/{Filtering,Symbols,Documentation,Resources,Layout}/` |
| Size | L |
| Branch / PR | `wu/504-filtering-symbols-resources-layout-handlers` / `WU-504: filtering-symbols-resources-layout-handlers` |

## Goal

Implement the phase-6 (`FilteringLayout`) handlers that turn `rules[]`, `defaults`, `symbols`, `documentation` and `layout[]` into precedence-ranked intents over the projected state. Associated-file policies are respected, preservation is the default, and other-RID assets are removed only when a rule says so explicitly.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §9](../../Requirements/Transformation_Specification.md#9-include-and-exclude-rules) | Include/exclude by semantic rules, catch-all preservation |
| [TS §10](../../Requirements/Transformation_Specification.md#10-associated-file-policies) | Primary only / selected associations / whole group |
| [TS §15](../../Requirements/Transformation_Specification.md#15-resource-and-localisation-policy) | Culture retention by pattern, associations respected |
| [TS §16](../../Requirements/Transformation_Specification.md#16-debug-symbol-policy) | `preserve` / `exclude` / `separate`, scoped by selector |
| [TS §17](../../Requirements/Transformation_Specification.md#17-documentation-policy) | `preserve` / `exclude` XML documentation |
| [TS §18](../../Requirements/Transformation_Specification.md#18-file-and-folder-layout-rules) | Layout mapping, default location retained, collisions |
| [TS §20.2](../../Requirements/Transformation_Specification.md#20-removal-rules), [TS §21](../../Requirements/Transformation_Specification.md#21-transformation-defaults) | Semantic removal, safe defaults |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Folder layout normalisation as a material packaging change |
| Architecture [§9](../../Architecture/DotNetRepack.architecture.md#9-transformation-handlers), [§19](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) items 4, 16 | Handler table, preserve-by-default, material operations |
| Plan M5 criteria 2, 4 | AC-5, AC-11, AC-12 |

## Scope

**In**
- Handlers (all `Phase = FilteringLayout`, registered by category): `filtering` (`rules[]`, `defaults.include`), `resources` (`defaults.cultures`), `symbols` (`symbols`, `defaults.symbols`), `documentation` (`documentation`, `defaults.documentation`), `layout` (`layout[]`).
- `AssociationGroupExpander`: TS §10.3 `primaryOnly` / `selected` (+ `associationTypes`) / `group`.
- Intent construction with WU-501 `IntentSource`/levels. Destination intents for layout.
- `Validate` checks specific to these sections.

**Out**
- Intent resolution, materialisation, collision and removal-safety checks (WU-503). Symbols output writing (WU-601). Projected AppSpec (WU-505).
- `additions[]` ([TS §19](../../Requirements/Transformation_Specification.md#19-addition-rules)): WU-507.
- deps.json updates for removed assets: minimal pruning in WU-604 (v0.2.0); FD⇄SC changes in WU-802.

## Deliverables

Namespace `DotNetRepack.Transforms`.

| Type | Behaviour |
|---|---|
| `Filtering.FilteringHandler` | For each `rules[]` entry: select over `HandlerContext.State`, expand associations, add a `disposition` intent (`preserve` for include, `exclude` for exclude) at the WU-501 level. `defaults.include` → `Baseline` intents |
| `Resources.ResourcesHandler` | `defaults.cultures` (retention patterns) → `exclude` intents at `GlobalDefault` for artefacts whose culture is not retained; satellites are expanded as groups of their culture folder; neutral resources are untouched |
| `Symbols.SymbolsHandler` | `symbols.policy` over `association: symbols` (scoped by `symbols.select`): `preserve` → `preserve`, `exclude` → `exclude`, `separate` → `separateSymbols`. PDBs whose primary resolves to `exclude` follow the primary (removed, not extracted) |
| `Documentation.DocumentationHandler` | `documentation.policy` over `association: xmlDoc` (scoped by `documentation.select`) → `preserve`/`exclude` |
| `Layout.LayoutHandler` | `layout[]`: `destination` intent = `destination/<relative path>` or, with `flatten`, `destination/<file name>`. Associated files follow their primary with the same relative offset |
| `Common.AssociationGroupExpander` | `Expand(match, mode, types)` → artefacts. Group expansion applies only when the matched artefact is a primary. Default mode: `group` |
| `Common.SectionIntentFactory` | Builds `IntentSource` with document and JSON pointer from WU-103 provenance |
| `TransformsFilteringDiagnostics` | `RPK5401`–`RPK5499` |

**Handler validation**

| Code | Condition |
|---|---|
| `RPK5401` | `symbols.select` / `documentation.select` matches only artefacts that have no `symbols` / `xmlDoc` association, so the section has no effect (warning) |
| `RPK5402` | `layout.destination` equals or is under a folder whose AppSpec role is `runtime`/`resources` while the rule moves managed assemblies (warning: probing may break) |
| `RPK5403` | Associated file retained while its primary is removed (orphan), severity from `PolicySet` `unknownContent` → default info |
| `RPK5404` | Exclude rule targets `rid` assets but the selector is not RID-scoped (no `rid` predicate) — informational hint to keep removal explicit |

## Design Notes

- Handlers only contribute **intents**. WU-503 resolves them via WU-501 and materialises the actions, so no ordering exists between these five handlers.
- **Preservation by default.** Every artefact without a winning intent is preserved ([TS §9.5](../../Requirements/Transformation_Specification.md#9-include-and-exclude-rules), [TS §21.4](../../Requirements/Transformation_Specification.md#21-transformation-defaults)). No handler removes RID-specific assets, cultures, symbols or docs unless the TransformSpec says so. The `enterprise-win-x64` removals are template content (architecture item 4).
- **Other-RID removal** is a plain exclude rule, e.g. `{ "action": "exclude", "select": { "not": { "rid": "${targetRid}" } } }`. Three-valued selector logic (WU-500) keeps non-RID files out of scope.
- **Associated groups.** Group expansion gives associated files the same level as the rule. A separate narrower rule on an associated file can override it at a higher level; an equal level conflicts (`RPK5104`).
- Symbols `separate`: extracted PDBs keep their relative path inside the symbols root.
- Handlers select over `HandlerContext.State` (projected after phases 3–5), so files added by deployment-model changes (WU-803) are filtered too.

## Acceptance Criteria

- [ ] AC-1 With an empty `rules[]` and all sections absent, the handlers add no intents and every file is `Preserve` (plan golden file).
- [ ] AC-2 `exclude` with `association: xmlDoc` removes every `.xml` doc and nothing else on the ConsoleApp net10 FD matrix entry.
- [ ] AC-3 `exclude` of `ConsoleApp.Library.dll` with default `group` mode removes its `.pdb`, `.xml` and satellites. `primaryOnly` removes only the DLL and yields `RPK5403` for the orphans.
- [ ] AC-4 `defaults.cultures: ["en", "en-*"]` removes `de/` and `fr/` satellites and keeps neutral resources, on net8 and net10 FD/SC matrix entries.
- [ ] AC-5 A level-3 `culture` exclude plus a level-5 exception rule that includes `de/ConsoleApp.resources.dll` keeps that file. The same include at level 3 yields `RPK5101`.
- [ ] AC-6 `symbols.policy` `preserve`, `exclude` and `separate` each produce the expected `Preserve`/`Remove`/`ExtractSymbols` actions for every PDB. PDBs of excluded primaries are `Remove`, not `ExtractSymbols`.
- [ ] AC-7 `symbols.select: {assemblyRole: application}` with `separate` extracts only application PDBs. Framework PDBs keep the default.
- [ ] AC-8 `documentation.policy: exclude` removes XML docs. A level-4 `path` include of one XML doc keeps it.
- [ ] AC-9 `layout` with `destination: lib` moves the selected assemblies to `lib/…`; their PDB/XML follow. `flatten` produces `lib/<name>`.
- [ ] AC-10 An exclude with `not: {rid: "win-x64"}` on `ConsoleApp/<tfm>-fdportable-il` removes only non-compatible `runtimes/<rid>/…` files. Without such a rule, no `runtimes/` file is removed.
- [ ] AC-11 Two layout rules that flatten two files with the same name into one folder yield `RPK5301` (from WU-503).
- [ ] AC-12 Plans for the filtering, symbols, docs and resources scenarios match committed golden files and are byte-identical across two runs.

## Test Requirements

- Unit: `tests/DotNetRepack.Transforms.Tests/<Handler>/`, with synthetic EAMs and a real `Planner` with only these handlers registered. Trait `WU=504`.
- Integration (matrix copies, WU-305 AppSpecs): ConsoleApp, WpfApp and PluginHost × net8/net10 × FD/SC (`il`), plus ConsoleApp `fdportable-il`. Traits `Category=Integration`, `Category=Matrix`, `WU=504`.
- Run: `dotnet test --project tests/DotNetRepack.Transforms.Tests --filter-trait "WU=504"`; `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=504"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `RPK54xx` codes listed for WU-1001.

## Agent Notes

- Transforms references Planning (architecture §3.1). Do not reference Execution.
- Keep each handler in its own folder and file so that WU-1003 guides can point at them.
- Scenario TransformSpecs created here (`filtering`, `symbols-dir`, `symbols-zip`, `docs`, `resources-en`, `other-rid`) should live under `tests/DotNetRepack.IntegrationTests/TransformSpecs/` for reuse by WU-505, WU-506 and WU-603.

## Open Questions

- **Resolved** — deps.json consistency after filtering: WU-604 (M6) prunes removed satellite, RID and library assets so v0.2.0 outputs launch.
- **Resolved** — `additions[]` owner: WU-507.
- Where does `separate` symbols output go when neither `symbols.output.path` nor `--symbols-output` is given? Proposed default: `<output>.symbols` (directory) or `<output>.symbols.zip`. This needs an architecture decision (WU-600/WU-601).
- Default association mode for rules (`group` proposed; TS §10.4 examples imply it for removal).

## Test Evidence

_To be completed by the implementer._
