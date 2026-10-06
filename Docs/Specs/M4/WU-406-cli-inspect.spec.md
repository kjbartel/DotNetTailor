# WU-406: cli-inspect

| Field | Value |
|---|---|
| ID | WU-406 |
| Title | cli-inspect |
| Milestone | M4 Analysis & Validation |
| Status | Ready |
| Depends on | WU-404, WU-305 |
| Parallel with | WU-405, M5, WU-1002 |
| Target project(s)/paths | `src/Tailor.Cli/Commands/Inspect/`, `tests/Tailor.Cli.Tests/Inspect/`, `tests/Tailor.IntegrationTests/Cli/Inspect/` |
| Size | S |
| Branch / PR | `wu/406-cli-inspect` / `WU-406: cli-inspect` |

## Goal

Ship the `inspect` verb: show the derived artefacts and graphs of an application (inventory, classification, assemblies, dependency graph, plugin graph, runtime inventory) as human-readable text or canonical JSON, with `--plugin` scoping. Completes the v0.1.0-preview capability list.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §22](../../Requirements/Application_Specification.md#22-derived-analysis-artefacts) | Derived, non-authoritative views |
| [AS §16](../../Requirements/Application_Specification.md) | Plugin graph inspection |
| [RQ §4.2](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Non-mutating, dotnet-style CLI |
| Architecture [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§14](../../Architecture/Tailor.architecture.md#14-cli), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 8 | Artefact set, synopsis, `inspect --plugin` scoping |
| Plan M4 criterion "`inspect` shows every derived artefact" | AC-1–AC-8 |

## Scope

**In**: `inspect <appDir> --spec <file> [inventory|classification|assemblies|graph|plugins|runtime] [--plugin <id>] [--format text|json]`, reuse of the WU-404 load → EAM pipeline (no validation report written), text renderers, `--plugin` filtering.

**Out**: Writing artefacts to disk (use `analyse`/`validate`), heuristics (WU-400–WU-402), new artefact content (WU-305 owns the DTOs).

## Deliverables

| Item | Detail |
|---|---|
| `Cli.Commands.Inspect.InspectCommand` | Replaces the WU-105 stub. Loads the AppSpec (with includes, WU-103), builds the EAM (WU-305 `EffectiveModelBuilder`), renders the selected view to stdout. Default view: `inventory` |
| `--format json` | Writes the same DTO as the WU-305 artefact of that view, via the canonical JSON writer. Byte-identical to the corresponding `.tailor/*.json` artefact for the same inputs |
| `--format text` (default) | Deterministic, sorted, tabular text per view; graphs as indented trees (`plugins`) and edge lists (`graph`) |
| `--plugin <id>` | Restricts every view to the plugin unit `<id>` (plugin graph node id from WU-304) plus its upstream closure for `graph`/`plugins` |
| Diagnostic codes (proposed, `TLR042x`, CLI range) | `TLR0420` unknown view, `TLR0421` unknown plugin id |

| Exit | Condition |
|---|---|
| 0 | View rendered (model diagnostics with severity ≤ warning are printed to stderr) |
| 1 | Model build errors (EAM diagnostics with severity Error); partial views are still rendered |
| 2 | Usage errors, `TLR0420`, `TLR0421`, missing `--spec` |
| 3 | `--strict` with warnings |

## Design Notes

- `inspect` is read-only: it writes nothing to the app tree or `--artefacts`.
- Reuse the WU-404 composition root and in-process CLI helpers. Do not duplicate the load/EAM pipeline.
- JSON output reuses the WU-305 DTOs, so artefact schemas and `inspect --format json` cannot drift.
- `--plugin` scoping follows architecture §19 item 8: plugins are selected by identity, not by a separate command namespace.

## Acceptance Criteria

- [ ] AC-1 `inspect --help` matches the architecture §14 synopsis plus `--format` (golden file).
- [ ] AC-2 Each view (`inventory`, `classification`, `assemblies`, `graph`, `plugins`, `runtime`) renders text for a matrix ConsoleApp and PluginHost copy (golden files).
- [ ] AC-3 `--format json` output for each view is byte-identical to the corresponding artefact written by `analyse` for the same AppSpec and tree.
- [ ] AC-4 `inspect … plugins --plugin PluginA` on PluginHost lists only PluginA and its upstream closure; an unknown id exits 2 with `TLR0421`.
- [ ] AC-5 An unknown view name exits 2 with `TLR0420`.
- [ ] AC-6 The input tree fingerprint including sidecars, and the `--artefacts` directory, are unchanged after `inspect`.
- [ ] AC-7 On the cyclic plugin fixture (`expectedInvalid`), `plugins` renders the cycle and exits 1 with `TLR3401`.
- [ ] AC-8 Two runs produce byte-identical stdout for every view and format.

## Test Requirements

- Unit: `tests/Tailor.Cli.Tests/Inspect/` (parsing, renderers over synthetic EAMs). Trait `WU=406`.
- Integration: `tests/Tailor.IntegrationTests/Cli/Inspect/`, in-process CLI over matrix copies with WU-305 hand-authored AppSpecs. Traits `Category=Integration`, `Category=Matrix`, `WU=406`.
- Run: `dotnet test --project tests/Tailor.Cli.Tests --filter-trait "WU=406"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=406"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; `TLR042x` codes listed for WU-1001; M4 `inspect` criterion demonstrably covered; changes limited to target paths (plus the plan status row).

## Agent Notes

- Keep renderers in one class per view so help and guides (WU-1003) can reference them.

## Open Questions

- None.
