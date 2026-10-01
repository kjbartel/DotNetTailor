# WU-300: folder-matching-engine

| Field | Value |
|---|---|
| ID | WU-300 |
| Title | folder-matching-engine |
| Milestone | M3 Effective Application Model |
| Status | Not started |
| Depends on | WU-101, WU-203 |
| Parallel with | WU-200–WU-202, WU-103 |
| Target project(s)/paths | `src/Tailor.Model/Tree/`, `src/Tailor.Model/Folders/`, `tests/Tailor.Model.Tests/Folders/` |
| Size | L |
| Branch / PR | `wu/300-folder-matching-engine` / `WU-300: folder-matching-engine` |

## Goal

Match every folder of an application tree to exactly one effective folder definition from a loaded AppSpec, deterministically and confined to the app root. This is the first stage of the Effective Application Model (EAM) build.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §10.2](../../Requirements/Application_Specification.md#102-folder-definitions)–[§10.7](../../Requirements/Application_Specification.md#107-catch-all-folders) | Definitions, masks, recursion, reuse, roles, catch-all |
| [AS §3.6](../../Requirements/Application_Specification.md#36-determinism), [AS §3.7](../../Requirements/Application_Specification.md#37-portability) | Determinism, root-relative paths |
| [AS §23.3](../../Requirements/Application_Specification.md#233-path-handling) | Paths relative to app root |
| [RD §3.2](../../Requirements/R2R_tool_Design.md#32-core-concepts), [RD §3.4](../../Requirements/R2R_tool_Design.md#34-invariants) | One definition per folder, no ambiguity, no alias cycles, no escaping paths, deterministic traversal |
| [RQ §4.1](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Read-only access to the tree |
| Architecture [§7.1](../../Architecture/Tailor.architecture.md#71-folder-matching), [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) items 2, 5, 19 | Normative design |
| Plan M3 criteria 3 (ambiguity, alias cycle, escaping/reparse codes) | AC-6, AC-8, AC-10, AC-11 |

## Scope

**In**
- Read-only app-tree abstraction (physical + in-memory) used by all EAM stages.
- Mask parsing and matching: glob segments, `*`, `**`, `<culture>`, `<rid>`.
- `recurse`, `idRef` expansion with overrides, recursive nesting, alias-cycle detection.
- Sibling specificity precedence and equal-rank ambiguity errors.
- Root confinement, reparse-point handling, sidecar exclusion.
- Deterministic depth-first traversal and match result model.

**Out**
- File classification (WU-301), reference roots resolution (WU-303), heuristics (WU-401).
- Include/multi-document merging (WU-103): this WU consumes an already-merged `AppSpec` model.
- Schema changes to the AppSpec (WU-101 owns them; raise an Open Question instead).

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Model.Tree.IAppTree` | Read-only: `EnumerateDirectories(RelativePath)`, `EnumerateFiles(RelativePath)`, `GetEntry(RelativePath)`, `OpenRead(RelativePath)`. Entries expose `Path`, `IsDirectory`, `Length`, `IsReparsePoint`, `ResolvedTarget` (absolute or `null`). Enumeration sorted ordinal-ignore-case |
| `PhysicalAppTree` | BCL-based (`FileSystemInfo.LinkTarget`, `ResolveLinkTarget(true)`, `FileAttributes.ReparsePoint`); long-path safe; never writes |
| `InMemoryAppTree` + `InMemoryAppTreeBuilder` | In `src` (reused later for projected trees and by other test projects); fluent `AddFile(path, bytes)`, `AddDirectory`, `AddReparsePoint(path, target)` |
| `SidecarSet` | Relative paths excluded from scope: loaded AppSpec document(s) located inside the root and the artefacts directory (default `.tailor/`) when inside the root |
| `Tailor.Model.Folders.FolderMask` | Parsed mask: segments, `Rank` per segment (`Literal` > `SingleSegment` (`*`, `?`, `[…]`, `<culture>`, `<rid>`) > `CatchAll` (`**`)) |
| `FolderDefinitionResolver` | Expands `idRef` lazily per level with override semantics; detects alias cycles |
| `FolderMatcher.Match(AppSpec, IAppTree, SidecarSet, IRidKnowledge, ICultureKnowledge)` → `FolderMatchResult` | `Folders` (pre-order), `Diagnostics` |
| `MatchedFolder` | `Path`, `DefinitionId`, `DefinitionChain` (e.g. `root/plugins/plugins`), `Role`, `MatchKind` (`Root`, `Explicit`, `Recursed`), `Mask`, `ParentPath`, effective definition members (classification groups, references, duplicates) |
| Diagnostic codes (proposed, `TLR30xx`) | `TLR3001` invalid mask (syntax, absolute, `..`, `\`, drive, token inside a segment), `TLR3002` ambiguous equal-rank match, `TLR3003` unknown `idRef`, `TLR3004` `idRef` alias cycle, `TLR3005` folder not covered, `TLR3006` reparse point escapes root, `TLR3007` in-root directory reparse point not traversed (warning), `TLR3008` case-colliding entries, `TLR3009` duplicate definition id |

## Design Notes

- Follow architecture [§7.1](../../Architecture/Tailor.architecture.md#71-folder-matching); do not restate it in code comments.
- **Mask semantics.** Masks are `/`-separated, root-relative to the parent folder. `<culture>` and `<rid>` are whole-segment tokens validated via WU-203 knowledge. `**` matches **one or more** segments (never zero), so every `idRef` step consumes at least one folder level.
- **Candidate set** for folder `F`: child definitions of `F`'s nearest explicitly matched ancestor whose mask matches the remaining relative path. If none match, inherit from the nearest ancestor with `recurse: true` (`MatchKind.Recursed`). Child definitions apply relative to the explicitly matched folder only and do **not** re-apply below recursed folders; authors use `idRef` to repeat a structure (architecture §7.1, provisional).
- **Specificity.** Segment rank: `Literal` > `SingleSegment` > `CatchAll`. A mask's rank is the rank of its **least-specific segment**; ties are broken by segment count (more segments = more specific). The single highest candidate wins. Two or more candidates with the same rank and segment count that match `F` → `TLR3002` naming the folder and all definition ids and chains. `F` and its subtree are then not matched further; traversal continues elsewhere to collect all diagnostics.
- **idRef.** Explicit members on the referencing node replace the referenced members; collection members (`folders`, references, groups) are replaced wholesale, not merged. Resolution of a node that is itself only an alias (`A → B → A`, or `A → A`) without consuming a folder level is `TLR3004` listing the chain. Nesting a definition inside itself (FS `plugins` inside `plugins`) is legal because expansion happens per matched folder level.
- **Confinement.** Reject invalid masks before traversal. For reparse points: a target outside the canonical root → `TLR3006` (error, entry excluded); a directory target inside the root → `TLR3007` (warning, not traversed, guarantees termination); file reparse points inside the root are treated as ordinary files.
- **Sidecars** are excluded before matching and never produce diagnostics.
- Model depends only on Specifications and Inspection ([§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies)); do not reference `Platform.Abstractions`; use BCL link APIs inside `PhysicalAppTree`.

## Acceptance Criteria

- [ ] AC-1 `IAppTree`, `PhysicalAppTree`, `InMemoryAppTree` exist; enumeration order is ordinal-ignore-case and identical for both implementations over the same content (test).
- [ ] AC-2 Masks support literal, `*`, `?`, `**`, `<culture>`, `<rid>`; `<culture>` matches `de`, `en-US`, `zh-Hans` and rejects `runtimes`; `<rid>` matches `win-x64`, `linux-arm64` and rejects `de` (theory tests).
- [ ] AC-3 `recurse: true` assigns the definition (`MatchKind.Recursed`) to every descendant not matched by a child definition.
- [ ] AC-4 `idRef` reuses a definition; an explicit `mask`/`role` on the referencing node overrides the referenced value (test asserts effective members).
- [ ] AC-5 A definition nested inside itself via `idRef` (plugins within plugins, 3 levels deep) matches without error.
- [ ] AC-6 Alias cycles `A→A` and `A→B→A` produce `TLR3004` whose message lists the full chain; unknown `idRef` produces `TLR3003`.
- [ ] AC-7 Specificity: literal beats `*`/token, which beats `**` (theory covering all pairs). Multi-segment: `Plugins/*` (least-specific `*`) beats `**`; `Plugins/**` beats `**` by segment count; `Plugins/*` and `*/*` tie (same least-specific rank and segment count) and yield `TLR3002`.
- [ ] AC-7a `**` never matches zero levels: a mask `a/**` does not match folder `a` itself.
- [ ] AC-7b A child definition of `root` is not applied to a folder below a `recurse: true` descendant unless reached via `idRef`.
- [ ] AC-8 Two equally ranked matching siblings produce `TLR3002` naming the folder path and both definition ids.
- [ ] AC-9 A folder with no candidate and no recursive ancestor produces `TLR3005`.
- [ ] AC-10 Masks containing `..`, absolute paths, drive letters or `\` produce `TLR3001` before any traversal.
- [ ] AC-11 A junction/symlink resolving outside the root produces `TLR3006`; one resolving inside produces `TLR3007` and is not traversed (physical-tree test creating links in a temp dir; skipped with explicit reason only if link creation is not permitted).
- [ ] AC-12 The AppSpec file and `.tailor/` inside the root are absent from `FolderMatchResult` and produce no diagnostics.
- [ ] AC-13 Matching the same tree twice yields equal results; a golden file of the canonical result for the FS-equivalent layout ([folderspec.json](../../Requirements/folderspec.json) translated to canonical form) is committed.
- [ ] AC-14 The engine performs no writes (test: physical tree fingerprint before/after equal).

## Test Requirements

- xUnit v3 + golden files (`Tailor.Testing.Golden`) in `tests/Tailor.Model.Tests/Folders/`; tag tests `[Trait("WU", "300")]`.
- Unit tests use `InMemoryAppTree` synthetic trees only; physical tests (AC-11, AC-14) use temp dirs and clean up.
- Run: `dotnet test --project tests/Tailor.Model.Tests --filter-trait "WU=300"`.
- No matrix (`artifacts/testapps`) dependency in this WU.
- Record Test Evidence (commands, filters, pass/fail/skip counts, commit SHA) in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green (build with warnings as errors, tests, `dotnet format --verify-no-changes`).
- Diagnostic codes defined in one Model diagnostics catalogue class with message templates.
- Changes limited to target paths (plus the plan status row).
- Any deviation from architecture §7.1 recorded as an ADR and reflected in the architecture document.

## Agent Notes

- Use `Microsoft.Extensions.FileSystemGlobbing` only for per-segment glob semantics if it fits; a small custom segment matcher is acceptable if it keeps rank computation simple.
- Keep `FolderMatchResult` immutable; later stages (WU-301, WU-303) consume `MatchedFolder` effective members, so expose them rather than raw spec nodes.
- Cap diagnostics per code per run is not required; keep traversal linear.

## Open Questions

- **Resolved (provisional)** — multi-segment masks: rank by least-specific segment, then segment count; intermediate folders (`Plugins`) are matched independently by siblings (architecture §7.1).
- **Resolved (provisional)** — child definitions do not re-apply at recursed descendants; use `idRef`.
- **Resolved (provisional)** — `**` matches one or more levels, never zero (architecture §7.1).
- Are included subsidiary AppSpec documents inside the root sidecars (provisional: yes, caller passes them in `SidecarSet`)?
- Tokens inside a segment (e.g. `lib-<rid>`) rejected in v1 (provisional).
