# WU-304: dependency-and-plugin-graphs

| Field | Value |
|---|---|
| ID | WU-304 |
| Title | dependency-and-plugin-graphs |
| Milestone | M3 Effective Application Model |
| Status | Not started |
| Depends on | WU-303 |
| Parallel with | WU-302 |
| Target project(s)/paths | `src/Tailor.Model/Graphs/`, `tests/Tailor.Model.Tests/Graphs/`, `tests/Tailor.IntegrationTests/Model/` |
| Size | S |
| Branch / PR | `wu/304-dependency-and-plugin-graphs` / `WU-304: dependency-and-plugin-graphs` |

## Goal

Build the assembly dependency graph and the plugin graph from resolved references, enforce that the plugin graph is a DAG using Tarjan SCC, and report cycles naming plugins, assemblies and paths.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §15.5](../../Requirements/Application_Specification.md#155-derived-dependency-graph), [AS §16](../../Requirements/Application_Specification.md#16-plugin-model) | Derived graph, plugin identification, one-way dependencies |
| [RD §6.1](../../Requirements/R2R_tool_Design.md#61-plugin-definition)–[§6.4](../../Requirements/R2R_tool_Design.md#64-validation-rules) | DAG invariant, diagnostics content |
| Architecture [§7.6](../../Architecture/Tailor.architecture.md#7-effective-application-model-semantics) | Tarjan SCC, diagnostic contents |
| Plan M3 criterion 4 | AC-5, AC-6 |

## Scope

**In**: assembly graph, plugin unit identification, plugin graph, SCC detection, cycle diagnostics, assembly-level SCC reporting (informational).

**Out**: artefact serialisation (WU-305), selector `plugin` predicate (WU-500), DI-based relationships ([AS §16.4](../../Requirements/Application_Specification.md#164-dependency-injection)).

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Model.Graphs.AssemblyGraph` | Nodes: in-tree managed assemblies (by path) + external framework nodes (`verified`/`unverified`); edges from `ReferenceResolutionResult` with outcome |
| `PluginUnit` | `Id` (folder path), `DefinitionId`, `Path`, `ParentPluginId?`, `Assemblies`; plugin = folder with role `plugin` matched `Explicit`; recursed descendants belong to it; nested plugin folders are separate units |
| `PluginGraph` | Nodes: plugin units + `application` node; edge `A→B` when an assembly in `A` references an assembly resolved in `B` (`B ≠ A`), with contributing `(fromPath, toPath)` pairs |
| `StronglyConnectedComponents` | Generic iterative Tarjan (no recursion-depth risk), deterministic node order |
| `GraphBuilder.Build(...)` → `GraphResult` | `AssemblyGraph`, `PluginGraph`, `Diagnostics` |
| Diagnostic codes (proposed, `RPK34xx`) | `RPK3401` plugin dependency cycle (error), `RPK3402` assembly dependency cycle outside plugin boundaries (info) |

## Design Notes

- For each plugin SCC with >1 node, emit one `RPK3401`: message lists an elementary cycle starting at the ordinal-smallest plugin id (shortest path back via BFS, deterministic), e.g. `Plugins/A → Plugins/B → Plugins/A`; related locations list every contributing assembly reference `fromPath → toPath` and the SCC member set.
- Edges to the `application` node are legal and never form reportable plugin cycles unless the application references a plugin that references the application (still reported: application is a node).
- Deterministic ordering: nodes by path, edges by `(from, to)`.

## Acceptance Criteria

- [ ] AC-1 Assembly graph contains one node per in-tree managed assembly and one edge per resolved/framework reference outcome (synthetic test).
- [ ] AC-2 Plugin units: `Plugins/*` explicit matches become units; recursed subfolders belong to their unit; nested plugins are distinct units with `ParentPluginId`.
- [ ] AC-3 One-way chain `A → B → C` yields a DAG with no diagnostics.
- [ ] AC-4 Mutual `A ↔ B` and 3-cycle `A → B → C → A` each yield exactly one `RPK3401` with the cycle path and the contributing assembly paths.
- [ ] AC-5 Integration: the cyclic-plugin test app (WU-003) yields `RPK3401` listing the full cycle path; golden file of the diagnostic committed.
- [ ] AC-6 Integration: the one-way plugin chain test app yields no `RPK3401`.
- [ ] AC-7 SCC implementation is iterative and handles a 10 000-node chain without stack overflow (unit test).
- [ ] AC-8 Graph output is identical across two builds (equality test) and independent of input enumeration order (test shuffles inputs).

## Test Requirements

- Unit: `tests/Tailor.Model.Tests/Graphs/` synthetic graphs; trait `WU=304`.
- Integration: `tests/Tailor.IntegrationTests/Model/PluginGraphTests` over the plugin-host matrix entries (one-way and cyclic, FD/SC, net8/net10) with hand-authored minimal AppSpecs; trait `Category=Integration`, `Category=Matrix`, `WU=304`.
- Run: `dotnet test --project tests/Tailor.Model.Tests --filter-trait "WU=304"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=304"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; codes in the Model diagnostics catalogue; changes limited to target paths (plus the plan status row).

## Agent Notes

- The minimal plugin-host AppSpecs written here should be reused by WU-305 fixtures; place them in `tests/Tailor.IntegrationTests/AppSpecs/`.

## Open Questions

- Should the application node participating in a cycle use the same code `RPK3401` (provisional) or a distinct code?
