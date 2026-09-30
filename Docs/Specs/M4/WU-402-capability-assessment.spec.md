# WU-402: capability-assessment

| Field | Value |
|---|---|
| ID | WU-402 |
| Title | capability-assessment |
| Milestone | M4 Analysis & Validation |
| Status | Not started |
| Depends on | WU-400 |
| Parallel with | WU-401, WU-403, WU-500, WU-501 |
| Target project(s)/paths | `src/Tailor.Analysis/Capabilities/`, `tests/Tailor.Analysis.Tests/Capabilities/`, `tests/Tailor.IntegrationTests/Analysis/` |
| Size | M |
| Branch / PR | `wu/402-capability-assessment` / `WU-402: capability-assessment` |

## Goal

Assess which transformations appear possible for the analysed state and write an advisory `capabilities.json` — never inside the AppSpec.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §19](../../Requirements/Application_Specification.md#19-capability-assessment) | Capabilities, states, advisory nature |
| [AS §22.2](../../Requirements/Application_Specification.md#222-example-artefacts) | Capability report as derived artefact |
| [RQ §5](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Transformation categories |
| Architecture [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§6.2](../../Architecture/Tailor.architecture.md#62-appspec-shape-illustrative-the-wu-101-schema-is-normative), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 17, [§9.1](../../Architecture/Tailor.architecture.md#91-readytorun-details) (skip reasons) | Separate artefact, R2R eligibility vocabulary |

## Scope

**In**: assessment of six capabilities from local facts, per-capability state + reasons, canonical `capabilities.json` writer.

**Out**: any network/package/catalogue lookup (M7+), compatibility analysis for retargeting (WU-901), R2R planning (WU-702).

## Deliverables

| Item | Detail |
|---|---|
| `Tailor.Analysis.Capabilities.CapabilityAssessor.Assess(ExecutionModelResult, EffectiveApplicationModel)` → `CapabilityReport` | Deterministic |
| `Capability` | `Id` (`retarget`, `runtimePatch`, `libraryPatch`, `fdToSc`, `scToFd`, `readyToRun`), `State` (`Supported`, `Unsupported`, `Conditional`, `Unknown`), `Reasons[]` |
| `CapabilityReason` | `Code` (stable camelCase, e.g. `alreadySelfContained`), `Message`, `Paths[]` (sorted, relative) |
| `CapabilityReportWriter` | `capabilities.json` with header `kind: CapabilityReport`, `schemaVersion`, `specHash`, `treeFingerprint`; canonical JSON |

## Design Notes

| Capability | Supported | Conditional | Unsupported | Unknown |
|---|---|---|---|---|
| `retarget` | — (compat analysis not available in M4) | TFM known, `net8.0+` (`compatibilityNotAnalysed`) | .NET Framework / pre-net5 TFM | TFM unknown |
| `runtimePatch` | FD with known framework versions | SC (`requiresRuntimePack`) | — | versions unknown |
| `libraryPatch` | — | deps.json lists `package` libraries (`requiresPackageAcquisition`) | no deps.json and no package metadata | deps.json unreadable |
| `fdToSc` | — | FD with win-x64 apphost or DLL entry (`requiresRuntimePack`) | already SC (`alreadySelfContained`) | deployment model unknown |
| `scToFd` | SC with host files present | SC with framework ownership unverifiable (`requiresRuntimeList`) | already FD (`alreadyFrameworkDependent`) | deployment model unknown |
| `readyToRun` | ≥1 IL-only, non-R2R, non-reference, x64/AnyCPU managed assembly | mixture with ineligible assemblies (reasons list counts per skip reason) | all assemblies already R2R / mixed-mode / reference | inspection failures |

- Reasons use the R2R skip vocabulary from architecture §9.1 where relevant.
- The draft AppSpec (WU-401) must never contain capability data; `capabilities.json` is written next to other derived artefacts.

## Acceptance Criteria

- [ ] AC-1 All six capabilities are present in every report, in fixed order, each with a state and ≥1 reason unless `Supported`.
- [ ] AC-2 FD matrix entries: `fdToSc` = `Conditional`, `scToFd` = `Unsupported (alreadyFrameworkDependent)`; SC entries: the inverse (`fdToSc` = `Unsupported (alreadySelfContained)`).
- [ ] AC-3 R2R-off entries report `readyToRun` = `Supported`; R2R-on entries report `Unsupported` or `Conditional` with `alreadyReadyToRun` reasons naming paths.
- [ ] AC-4 A synthetic .NET Framework TFM yields `retarget` = `Unsupported`; unknown TFM yields `Unknown`.
- [ ] AC-5 A synthetic mixed-mode assembly appears under an `ineligibleMixedMode` reason for `readyToRun`.
- [ ] AC-6 `capabilities.json` is canonical and byte-identical across two runs; golden file per matrix entry.
- [ ] AC-7 Serialising a draft AppSpec for any matrix entry contains no capability members (test).

## Test Requirements

- Unit: `tests/Tailor.Analysis.Tests/Capabilities/` with synthetic `ExecutionModelResult`/facts; trait `WU=402`.
- Integration: `tests/Tailor.IntegrationTests/Analysis/CapabilityTests` over the full matrix; trait `Category=Integration`, `Category=Matrix`, `WU=402`.
- Run: `dotnet test --project tests/Tailor.Analysis.Tests --filter-trait "WU=402"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=402"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; reason codes defined in one constants class; changes limited to target paths (plus the plan status row).

## Agent Notes

- AC-7 needs WU-401's generator; if WU-401 is not merged yet, assert against a WU-101 AppSpec model instance instead and note it in Test Evidence.

## Open Questions

- Should capabilities be reassessed once catalogues exist (M7+) — i.e. is `capabilities.json` schema expected to grow `Supported` states for `runtimePatch`/`fdToSc`? (Provisional: yes, same shape.)
