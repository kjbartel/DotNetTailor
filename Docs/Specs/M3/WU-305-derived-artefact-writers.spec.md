# WU-305: derived-artefact-writers

| Field | Value |
|---|---|
| ID | WU-305 |
| Title | derived-artefact-writers |
| Milestone | M3 Effective Application Model |
| Status | Not started |
| Depends on | WU-302, WU-304, WU-202 |
| Parallel with | WU-600, WU-700, WU-801 |
| Target project(s)/paths | `src/Tailor.Model/` (`EffectiveModelBuilder`, `Artefacts/`, `Fingerprints/`, `Execution/`), `tests/Tailor.Model.Tests/Artefacts/`, `tests/Tailor.Model.Tests/Execution/`, `tests/Tailor.IntegrationTests/AppSpecs/`, `tests/Tailor.IntegrationTests/Model/` |
| Size | M |
| Branch / PR | `wu/305-derived-artefact-writers` / `WU-305: derived-artefact-writers` |

## Goal

Compose the M3 stages into one `EffectiveApplicationModel` build and write the six derived artefacts deterministically (byte-identical across runs), with hand-authored AppSpecs for every test app proving the M3 milestone.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §22](../../Requirements/Application_Specification.md#22-derived-analysis-artefacts) | Derived, non-authoritative artefacts |
| [AS §13.3](../../Requirements/Application_Specification.md#133-rule-based-representation), [AS §15.5](../../Requirements/Application_Specification.md#155-derived-dependency-graph) | Identities/graphs materialised outside the spec |
| [AS §3.6](../../Requirements/Application_Specification.md#36-determinism), [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Determinism, separate artefacts |
| Architecture [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline) (model build order), [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§15](../../Architecture/Tailor.architecture.md#15-determinism) | Artefact set, canonical JSON, tree fingerprint |
| Plan M3 criteria 1, 2, 5 | AC-6, AC-7, AC-8 |

## Scope

**In**: model orchestration (match → classify → associate → identities → references → graphs), tree fingerprint (sidecar exclusion over `Core.Hashing.TreeFingerprint`), spec hash, deterministic deployment-model/framework/TFM detection primitives (`Model.Execution`, consumed by WU-400 and WU-403), six artefact writers, hand-authored AppSpecs + golden files for the matrix.

**Out**: `validation-report.json` (WU-403), `capabilities.json` (WU-402), CLI wiring (WU-404), artefact JSON Schemas.

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Model.EffectiveModelBuilder.Build(AppSpec, IAppTree, EffectiveModelOptions)` → `EffectiveApplicationModel` | Options: `SidecarSet`, `IFrameworkCatalogue`, knowledge services. Model aggregates all stage results + merged `Diagnostics` (sorted by code, path, pointer) |
| `Tailor.Model.Fingerprints.AppTreeFingerprint` | Builds `TreeEntry` values for in-scope files (sidecars excluded) and delegates to `Core.Hashing.TreeFingerprint` (WU-100); no second hashing implementation |
| `Tailor.Model.Execution.RuntimeFactsDetector` | Pure, non-heuristic detection over WU-202/WU-200 facts: per entry point deployment model (`includedFrameworks` → SC, `framework(s)` → FD, host-file fallback reported separately), framework references and versions, TFM (runtimeconfig, else `TargetFrameworkAttribute`), RID from deps.json `runtimeTarget`. Each fact carries its source; no confidence scoring (WU-400 adds it) |
| `SpecHash` | SHA-256 of the canonical JSON of the merged AppSpec (provisional) |
| `Tailor.Model.Artefacts.DerivedArtefactWriter.WriteAll(model, directory)` | Uses WU-100 canonical JSON writer; each file has header `kind`, `schemaVersion` (`1.0`), `specHash`, `treeFingerprint` |
| `inventory.json` | Folders (path, definition id/chain, role, match kind) and files (path, size, sha256) |
| `classification-map.json` | File → group id, catch-all flag, status |
| `assemblies.json` | Managed assemblies: identity, TFM, architecture, R2R/composite, reference-assembly, satellite, role (application/plugin/framework), associations |
| `dependency-graph.json` | Nodes + edges with outcome and root index |
| `plugin-graph.json` | Plugin units, edges with contributing references, SCC/cycle list |
| `runtime-inventory.json` | runtimeconfig frameworks / includedFrameworks per entry, deps.json `runtimeTarget`, host files present (`hostfxr.dll`, `hostpolicy.dll`, `coreclr.dll`), framework references (verified/unverified) |
| Hand-authored AppSpecs | `tests/Tailor.IntegrationTests/AppSpecs/<app>.<fd\|sc>.appspec.json` for every WU-003 app |

## Design Notes

- No timestamps, absolute paths, machine names or GUIDs in artefacts ([§15](../../Architecture/Tailor.architecture.md#15-determinism)). Paths are `RelativePath` with `/`.
- Collections sorted ordinal-ignore-case by path/id; property order fixed by record declaration.
- Writers never write into the app tree unless the target directory is the sidecar `.tailor/` supplied by the caller.
- Artefacts are regenerable views; they carry no authority and are not read back by the engine.

## Acceptance Criteria

- [ ] AC-1 `EffectiveModelBuilder` runs the stages in the architecture order and merges diagnostics deterministically.
- [ ] AC-2 The app-tree fingerprint (via `Core.Hashing.TreeFingerprint`) changes when a file's content, size or path changes and is unchanged when only sidecars change (unit tests).
- [ ] AC-3 All six artefacts are written with the common header; each is canonical JSON (UTF-8 no BOM, LF, 2-space) — verified by a canonical-form test.
- [ ] AC-4 No artefact contains the absolute root path, machine name or a timestamp (test greps outputs against the temp root path and `Environment.MachineName`).
- [ ] AC-5 Golden files of all six artefacts for a synthetic tree are committed.
- [ ] AC-6 Hand-authored AppSpecs exist for every WU-003 app × {FD, SC}; building the EAM over every matrix entry yields zero errors, except variants flagged `expectedInvalid` in the WU-003 manifest (the cyclic plugin variant), which yield exactly their listed codes (`TLR3401`), and artefacts match committed golden files (known non-deterministic files from the WU-003 manifest are scrubbed).
- [ ] AC-7 Two consecutive runs over every matrix entry produce byte-identical artefacts.
- [ ] AC-8 Coverage: for every matrix entry, `classification-map.json` lists every in-scope file exactly once.
- [ ] AC-9 `RuntimeFactsDetector` reports the correct deployment model, frameworks and TFM for every matrix entry (FD/SC per the manifest) and is referenced by `runtime-inventory.json`; Model has no reference to Analysis or Validation.

## Test Requirements

- Unit: `tests/Tailor.Model.Tests/Artefacts/` with synthetic trees; trait `WU=305`.
- Integration: `tests/Tailor.IntegrationTests/Model/DerivedArtefactGoldenTests` over `artifacts/testapps/manifest.json`; trait `Category=Integration`, `Category=Matrix`, `WU=305`. CI must not skip.
- Run: `dotnet test --project tests/Tailor.Model.Tests --filter-trait "WU=305"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=305"`.
- Record Test Evidence (including matrix `manifest.json` hash) in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; M3 milestone criteria 1, 2, 5 demonstrably covered; changes limited to target paths (plus the plan status row).

## Agent Notes

- Framework patch versions in matrix runtimeconfigs follow the SDK used by WU-003; golden files will change on SDK bumps — keep scrubbers minimal and documented.
- Keep writers thin: map model → DTO records → canonical writer. No logic in writers.

## Open Questions

- **Resolved** — WU-202 dependency: added to the plan.
- **Resolved** — FD/SC/TFM detection shared by WU-400 and WU-403: `Model.Execution` primitives owned here (architecture §3.2).
- Spec hash over the merged canonical AppSpec (provisional) vs raw root-file bytes.
- Plan assigns no owner for "hand-authored AppSpecs for every test app" (M3 criterion 1); assigned here provisionally.
