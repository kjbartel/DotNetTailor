# WU-401: folder-role-heuristics-and-rule-compaction

| Field | Value |
|---|---|
| ID | WU-401 |
| Title | folder-role-heuristics-and-rule-compaction |
| Milestone | M4 Analysis & Validation |
| Status | Not started |
| Depends on | WU-400 |
| Parallel with | WU-402, WU-403, WU-500, WU-501 |
| Target project(s)/paths | `src/Tailor.Analysis/Heuristics/`, `src/Tailor.Analysis/Compaction/`, `src/Tailor.Analysis/DraftAppSpecGenerator.cs`, `tests/Tailor.Analysis.Tests/Heuristics/`, `tests/Tailor.Analysis.Tests/Compaction/`, `tests/Tailor.IntegrationTests/Analysis/` |
| Size | L |
| Branch / PR | `wu/401-folder-role-heuristics-and-rule-compaction` / `WU-401: folder-role-heuristics-and-rule-compaction` |

## Goal

Produce a concise, human-editable draft AppSpec from an unknown tree: assign folder roles heuristically, compact rules (recursion, reusable definitions, wildcards, catch-all), annotate confidence, and guarantee the draft builds an EAM with zero errors.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §2.2](../../Requirements/Application_Specification.md#22-analysis), [AS §2.3](../../Requirements/Application_Specification.md#23-user-refinement) | Heuristic draft, user refinement |
| [AS §3.2](../../Requirements/Application_Specification.md#32-rule-based-representation), [AS §3.5](../../Requirements/Application_Specification.md#35-human-editability), [AS §11.5](../../Requirements/Application_Specification.md#115-wildcard-preference) | Rule-based, concise, wildcard preference |
| [AS §10.4](../../Requirements/Application_Specification.md#104-recursion)–[§10.7](../../Requirements/Application_Specification.md#107-catch-all-folders), [AS §16.2](../../Requirements/Application_Specification.md#162-plugin-identification), [AS §17.2](../../Requirements/Application_Specification.md#172-classification) | Recursion, reuse, roles, catch-all, plugins, cultures |
| [AS §9.4](../../Requirements/Application_Specification.md#94-confidence) | Confidence annotations |
| Architecture [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies) (Analysis owns heuristics), [§6.2](../../Architecture/Tailor.architecture.md#62-appspec-shape-illustrative-the-wu-101-schema-is-normative) | Draft shape |
| Plan M4 criteria 1, 2 | AC-8, AC-9 |

## Scope

**In**: folder-role heuristics, classification/association rule emission, reference roots and duplicate policies, `knownUnresolved` emission, rule compaction, stable ids, `DraftAppSpecGenerator` combining WU-400 output.

**Out**: capabilities (WU-402), validation engine (WU-403), CLI (WU-404), writing files (the CLI writes via WU-101 canonical serialisation).

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Analysis.Heuristics.FolderRoleHeuristics` | Produces `FolderRoleAssignment` (path, role, confidence, evidence) per folder of the bootstrap EAM |
| `Tailor.Analysis.Compaction.RuleCompactor` | Converts per-folder assignments into a compact folder-definition tree |
| `Tailor.Analysis.DraftAppSpecGenerator.Generate(IAppTree, ExecutionModelResult, EffectiveApplicationModel bootstrap)` → `DraftAppSpecResult` | `AppSpec` (WU-101 model), `Diagnostics`, `SelfCheck` (EAM built from the draft) |
| Diagnostic codes (proposed, `RPK41xx`) | `RPK4101` draft self-check failed (error; indicates a heuristic bug), `RPK4102` low-confidence role assignment (info) |

## Design Notes

| Heuristic | Rule | Confidence |
|---|---|---|
| Runtime | Folder named `runtimes` whose children match `<rid>` → role `runtime`, `recurse: true` | Derived |
| Resources | Folder matching `<culture>` containing only satellite assemblies (+ associated files) → role `resources`, mask `<culture>` | Derived |
| Plugin | Child folders of a container where each contains a managed assembly that references a root assembly and is not referenced by root assemblies → role `plugin`, mask `<Container>/*` via a reusable definition | Inferred |
| Content | Everything else → sibling `**` catch-all with `recurse: true`, role `content` | Derived |

- **Compaction order**: (1) collapse subtrees whose descendants share role and settings into `recurse: true`; (2) extract structures appearing ≥2 times (e.g. plugin layouts with nested `runtimes`) into `folders.definitions` + `idRef`; (3) merge same-role siblings into `*`/token masks when no sibling conflicts; (4) drop explicit children equivalent to the catch-all. Never enumerate files for regular layouts.
- **References**: root `[current, current/runtimes]`; plugin definition `[current, current/runtimes, root, root/runtimes]`; catch-all `[current, root]` only when it contains managed assemblies; `duplicates: error` unless the bootstrap resolution found duplicates in that context, then `highestVersion` with `Inferred` confidence.
- **Unresolved non-framework references** found in the bootstrap EAM → `knownUnresolved` entries (`Inferred`) so the draft validates with warnings.
- **Ids** derive from folder names (camelCase, sanitised), deduplicated with numeric suffixes in path order.
- Confidence goes where the WU-101 schema allows; the draft never contains capabilities, timestamps or history.
- Self-check uses `EffectiveModelBuilder` (Model); Analysis must not reference Validation.

## Acceptance Criteria

- [ ] AC-1 Synthetic multi-RID `runtimes/{win-x64,linux-x64}/native` tree yields a single `runtime` definition with `recurse: true` and no per-RID children.
- [ ] AC-2 Culture folders `de`, `fr`, `zh-Hans` yield one `<culture>` definition with role `resources`.
- [ ] AC-3 A plugin container with nested `runtimes` per plugin yields one reusable plugin definition referenced via `idRef`, with the nested runtime layout expressed once.
- [ ] AC-4 Irregular folders fall into a single `**` catch-all (`content`, `recurse: true`).
- [ ] AC-5 No file paths are enumerated in the draft for any matrix app (test asserts no file-level entries).
- [ ] AC-6 Folder-definition count for each matrix draft is ≤ the number of distinct role structures + 2 (root, catch-all) (computed test).
- [ ] AC-7 Drafts are deterministic: two generations are byte-identical after canonical serialisation.
- [ ] AC-8 Self-check: the EAM built from each matrix draft has zero error diagnostics (warnings such as `RPK3302` allowed), except variants flagged `expectedInvalid` in the WU-003 manifest (the cyclic plugin variant), whose only error is `RPK3401`.
- [ ] AC-9 Drafts carry confidence annotations for execution, framework contexts and inferred roles, and contain `idRef`, recursion and a catch-all where the layout allows (golden file per matrix entry, reviewed in PR).
- [ ] AC-10 A plugin-host draft classifies each plugin folder as `plugin` and the plugin graph matches the WU-003 intended topology.

## Test Requirements

- Unit: `tests/Tailor.Analysis.Tests/Heuristics/` and `/Compaction/` with synthetic trees; trait `WU=401`.
- Integration: `tests/Tailor.IntegrationTests/Analysis/DraftAppSpecTests` over the full matrix; golden files of drafts; trait `Category=Integration`, `Category=Matrix`, `WU=401`.
- Run: `dotnet test --project tests/Tailor.Analysis.Tests --filter-trait "WU=401"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=401"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; changes limited to target paths (plus the plan status row).

## Agent Notes

- Keep heuristics as independent, individually tested rules returning evidence; compaction must not depend on heuristic internals.
- Golden-file review is the human-editability gate: keep drafts short enough to read in one screen for regular apps.

## Open Questions

- `knownUnresolved` emission depends on architecture inconsistency #20 (Open). Without it, drafts for apps with optional unresolvable references cannot pass AC-8.
- Does the WU-101 schema support `confidence` on folder definitions and framework contexts? If not, AC-9 is limited to `execution`.
- Plugin heuristic threshold: a single qualifying child folder counts (provisional).
