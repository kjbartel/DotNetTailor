# WU-303: reference-resolution

| Field | Value |
|---|---|
| ID | WU-303 |
| Title | reference-resolution |
| Milestone | M3 Effective Application Model |
| Status | Not started |
| Depends on | WU-301, WU-201 |
| Parallel with | WU-302 |
| Target project(s)/paths | `src/Tailor.Model/References/`, `src/Tailor.Model/Identities/`, `src/Tailor.Inspection/Frameworks/` (catalogue contract only), `tests/Tailor.Model.Tests/References/` |
| Size | M |
| Branch / PR | `wu/303-reference-resolution` / `WU-303: reference-resolution` |

## Goal

Resolve every managed assembly reference from declared, ordered reference roots per folder context with a per-context duplicate policy, classify framework references against a framework catalogue (or as "framework-provided (unverified)"), and never probe outside the app tree.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §13](../../Requirements/Application_Specification.md#13-managed-assembly-model), [AS §15](../../Requirements/Application_Specification.md#15-dependency-and-reference-model) | Identity, reference paths, discovery, duplicates |
| [RD §5.1](../../Requirements/R2R_tool_Design.md#51-resolution-context)–[§5.3](../../Requirements/R2R_tool_Design.md#53-resolution-rules) | Resolution contexts, identity, no probing, missing references |
| Architecture [§7.4, §7.5](../../Architecture/Tailor.architecture.md#7-effective-application-model-semantics), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) items 10, 20 | Framework catalogue, `knownUnresolved` |
| Plan M4 criterion 3 (wrong reference root, removed file) | AC-4, AC-9 |

## Scope

**In**
- Assembly identity index over classified managed files.
- Resolution contexts from `MatchedFolder` effective `references` and `duplicates`.
- Root kinds: `current`, `parent`, `root`, `folder:<id>`, anchored subpaths (`current/runtimes`), root-relative paths.
- Duplicate policies `error | first | highestVersion`.
- `IFrameworkCatalogue` contract + empty implementation; unverified framework warnings.
- `knownUnresolved` downgrade (architecture inconsistency #20).

**Out**
- RuntimeList-backed catalogue (WU-701 implements `IFrameworkCatalogue`).
- Native/P/Invoke resolution, deps.json-driven resolution, graphs (WU-304).

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Model.Identities.AssemblyIdentityIndex` | Built from files classified `managed`/`platformManaged`; excludes reference assemblies; uses WU-201 identity (name, version, culture, PKT) |
| `Tailor.Model.References.ResolutionContext` | `FolderPath`, `FolderDefinitionId`, ordered `Roots`, `DuplicatePolicy` |
| `ReferenceRoot` parser | `<anchor>[/<subpath>]` where anchor ∈ `current`, `parent`, `root`, `folder:<id>`; plain relative path = root-relative |
| `ReferenceResolver.Resolve(...)` → `ReferenceResolutionResult` | `Edges` (`FromPath`, `ReferenceIdentity`, `Outcome`, `ResolvedPath?`, `RootIndex?`), `Diagnostics` |
| `ReferenceOutcome` | `Resolved`, `FrameworkVerified`, `FrameworkUnverified`, `KnownUnresolved`, `Unresolved`, `Ambiguous`, `VersionTooLow` |
| `Tailor.Inspection.Frameworks.IFrameworkCatalogue` + `EmptyFrameworkCatalogue` | `TryGetFrameworkAssembly(frameworkName, frameworkVersion, assemblyName, out FrameworkAssemblyInfo)`; lives in Inspection so Acquisition (WU-701) can implement it; the Cli wires the implementation, `EmptyFrameworkCatalogue` is the fallback (architecture §3.2) |
| Diagnostic codes (proposed, `TLR33xx`) | `TLR3301` unresolved reference, `TLR3302` framework reference unverified (warning, one per distinct assembly name), `TLR3303` duplicate candidates under `error`, `TLR3304` candidate version lower than referenced, `TLR3305` invalid root (escapes root, unknown/ambiguous `folder:<id>`), `TLR3306` known-unresolved reference (warning), `TLR3307` stale `knownUnresolved` entry (warning) |

## Design Notes

- Match: name + culture + PKT (when the reference has one) equal; candidate version ≥ referenced version, else `TLR3304`.
- Root search is non-recursive, except roots whose target folder carries a `recurse: true` definition: the recursed subtree is searched in deterministic pre-order. Candidates under a `<rid>` segment incompatible with `platform.rids` (WU-203 RID graph) are ignored.
- `folder:<id>`: nearest ancestor-or-self with that definition id; else the unique folder with that id; else `TLR3305`.
- Duplicates = more than one matching candidate across all roots of the context. `first` = earliest root, then path order; `highestVersion` = highest version, tie → `first`.
- Framework references: when unresolved in-tree **and** the app declares shared frameworks, query `IFrameworkCatalogue`; hit → `FrameworkVerified`; miss with an empty catalogue and a framework public key token (proposed list: `b03f5f7f11d50a3a`, `cc7b13ffcd2ddd51`, `7cec85d7bea7798e`, `31bf3856ad364e35`, `adb9793829ddae60`) → `FrameworkUnverified` + `TLR3302`. Otherwise → `Unresolved`.
- `knownUnresolved` entries (by assembly name, optional referencing path) downgrade `Unresolved` to `KnownUnresolved` + `TLR3306`; entries that match nothing → `TLR3307`.
- Resolution only uses `IAppTree`; no GAC, `DOTNET_ROOT`, SDK or environment access.

## Acceptance Criteria

- [ ] AC-1 All root kinds resolve correctly on a synthetic tree (theory per kind, incl. `current/runtimes` recursive and `folder:<id>` nearest-ancestor).
- [ ] AC-2 Roots are searched in declared order; a context without a root containing the target yields `TLR3301` (no implicit inheritance from parent contexts).
- [ ] AC-3 Duplicate policies: `error` → `TLR3303` listing all candidate paths; `first` and `highestVersion` pick the expected candidate.
- [ ] AC-4 A root escaping the app root, or an unknown/ambiguous `folder:<id>`, yields `TLR3305`.
- [ ] AC-5 A candidate with lower version than referenced yields `TLR3304`.
- [ ] AC-6 FD app with `EmptyFrameworkCatalogue`: `System.Runtime` reference → `FrameworkUnverified`, one `TLR3302` per assembly name; a fake catalogue hit → `FrameworkVerified` with no diagnostic.
- [ ] AC-7 SC app: framework assemblies resolve in-tree (`Resolved`), no `TLR3302`.
- [ ] AC-8 `knownUnresolved` downgrades a missing reference to warning `TLR3306`; a stale entry yields `TLR3307`.
- [ ] AC-9 Removing a referenced app assembly from a synthetic tree yields `TLR3301` naming referencing file, identity and searched roots.
- [ ] AC-10 No-probing: a resolver test with a tree lacking framework files never touches paths outside the tree (fake `IAppTree` asserts every accessed path is in-tree) and results are identical on machines with/without installed runtimes.
- [ ] AC-11 Candidates under an incompatible `<rid>` folder (e.g. `runtimes/linux-x64`) are ignored for `win-x64`.

## Test Requirements

- Unit: `tests/Tailor.Model.Tests/References/`, synthetic trees with fake identity facts (no real PE needed); trait `WU=303`.
- One integration smoke over the plugin host FD/SC matrix entries: zero `TLR3301` with a hand-authored spec (full matrix golden files in WU-305).
- Run: `dotnet test --project tests/Tailor.Model.Tests --filter-trait "WU=303"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; codes in the Model diagnostics catalogue; `IFrameworkCatalogue` has an XML summary pointing to WU-701; changes limited to target paths (plus the plan status row).

## Agent Notes

- Until WU-701 lands, every FD matrix app produces `TLR3302` warnings; downstream "zero errors" criteria remain achievable, `--strict` does not.
- Cache per-context candidate lookups; contexts repeat across recursed folders.

## Open Questions

- Inconsistency #20 (`knownUnresolved`) is **Open**; if not confirmed or absent from the WU-101 schema, AC-8 is blocked — mark `Blocked (#20)` rather than inventing schema.
- Canonical root syntax (`current/runtimes` anchor + subpath) is not specified in architecture §7.5; confirm.
- Framework-candidate rule (public key token list) is a deterministic but new rule; confirm or replace with a framework-name list.
- Is `VersionTooLow` an error (provisional) or warning?
