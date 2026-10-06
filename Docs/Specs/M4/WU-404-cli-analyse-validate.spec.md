# WU-404: cli-analyse-validate

| Field | Value |
|---|---|
| ID | WU-404 |
| Title | cli-analyse-validate |
| Milestone | M4 Analysis & Validation |
| Status | Ready |
| Depends on | WU-401, WU-402, WU-403, WU-105, WU-103 |
| Parallel with | WU-502, WU-503 |
| Target project(s)/paths | `src/Tailor.Cli/Commands/Analyse/`, `src/Tailor.Cli/Commands/Validate/`, `src/Tailor.Cli/Composition/`, `tests/Tailor.Cli.Tests/`, `tests/Tailor.IntegrationTests/Cli/` |
| Size | M |
| Branch / PR | `wu/404-cli-analyse-validate` / `WU-404: cli-analyse-validate` |

## Goal

Ship `analyse` (alias `analyze`) and `validate` end to end: correct spec and artefact locations, exit codes, strict/permissive modes, and a test-enforced guarantee that the input tree is never modified apart from sidecars.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [RQ §4.1](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §4.2](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §9](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Non-mutating analysis, validation, spec location, dotnet-style CLI |
| [AS §23](../../Requirements/Application_Specification.md#23-location-and-portability) | Default/alternate spec location |
| Architecture [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline), [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§13](../../Architecture/Tailor.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§14](../../Architecture/Tailor.architecture.md#14-cli), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 2 | Pipeline, artefacts, exit codes, verbs, sidecars |
| Plan M4 criteria 1, 4, 5, 7 (local pack smoke only) | AC-4, AC-6–AC-9, AC-12 |

## Scope

**In**: `analyse` and `validate` commands, DI composition of Model/Analysis/Validation services, spec/artefact location rules, writability check, exit-code mapping, diagnostics console output, `dotnet pack` local smoke.

**Out**: `inspect` verb (WU-406), config file (WU-1000), release workflow and publishing (WU-1002).

## Deliverables

| Item | Detail |
|---|---|
| `analyse <appDir> [--spec-out <file>]` + globals (`--artefacts`, `--strict`/`--permissive`, `--verbosity`) | Pipeline: bootstrap EAM → WU-400 → WU-401 draft → WU-402 → write AppSpec (canonical) → `AppSpecValidator` self-validation → write derived artefacts, `capabilities.json`, `validation-report.json` |
| `validate <appDir> --spec <file>` | Load (with includes) → `AppSpecValidator` → write `validation-report.json` |
| Spec location | Default `<appDir>/tailor.appspec.json` if `appDir` is writable; else `--spec-out` required |
| Artefacts location | `--artefacts` or `.tailor/` next to the AppSpec |
| `IWritabilityProbe` | Default implementation creates and deletes a probe file inside `<appDir>/.tailor/` only; fake for tests |
| Diagnostic codes (proposed, `TLR04xx`, within WU-105 range) | `TLR0401` app directory not found, `TLR0402` input not writable (`--spec-out` required), `TLR0403` AppSpec already exists at target |

| Exit | Condition |
|---|---|
| 0 | `Validated` / `ValidatedWithWarnings` |
| 1 | `Invalid` (errors), bundle refusal |
| 2 | Usage errors, `TLR0401`–`TLR0403` |
| 3 | `--strict` and warnings present |
| 4 | Tree unreadable / IO environment failure (`Unvalidated`) |
| 70 | Unhandled exception |
| 130 | Cancelled (Ctrl+C) |

## Design Notes

- Sidecars passed to the model: the AppSpec path(s) and artefacts dir when inside `appDir`.
- `analyse` writes the AppSpec even when self-validation fails (for user editing) and exits 1.
- Console output: one line per diagnostic `path(pointer): severity TLRnnnn: message`, sorted; summary line with state.
- Cli is the only place wiring Analysis + Validation together (architecture §3.1).

## Acceptance Criteria

- [ ] AC-1 `analyse` and `analyze` are both accepted; `--help` lists both verbs with the synopsis from architecture §14.
- [ ] AC-2 `analyse <dir>` on a writable copy of a matrix app writes `tailor.appspec.json` and `.tailor/{inventory,classification-map,assemblies,dependency-graph,plugin-graph,runtime-inventory,capabilities,validation-report}.json`.
- [ ] AC-3 `analyse --spec-out <outside>` writes the AppSpec there and artefacts next to it; the app tree fingerprint **including sidecars** is unchanged.
- [ ] AC-4 A non-writable input (fake probe; plus one real ACL-deny test) without `--spec-out` exits 2 with `TLR0402` and writes nothing.
- [ ] AC-5 An existing AppSpec at the target yields `TLR0403`, exit 2, file untouched.
- [ ] AC-6 For every matrix entry, the tree fingerprint excluding sidecars is identical before and after `analyse` and `validate`.
- [ ] AC-7 `analyse` → `validate` exits 0 with zero errors for every matrix entry except variants flagged `expectedInvalid` in the WU-003 manifest (the cyclic plugin variant exits 1 with exactly `TLR3401`).
- [ ] AC-8 `validate` with an edited invalid spec exits 1; with warnings only and `--strict` exits 3.
- [ ] AC-9 `validation-report.json` written by `validate` contains `specHash`, `treeFingerprint`, `state`.
- [ ] AC-10 Nonexistent `appDir` exits 2 with `TLR0401`; single-file bundle fixture exits 1.
- [ ] AC-11 A multi-document AppSpec (`includes`) validates identically to its flattened equivalent.
- [ ] AC-12 `dotnet pack src/Tailor.Cli` produces `dotnet-tailor`; `dotnet tool install --tool-path <tmp> --add-source <nupkgDir> dotnet-tailor` succeeds and `dotnet-tailor --help` exits 0 (integration test).

## Test Requirements

- Unit: `tests/Tailor.Cli.Tests/` — parsing, option validation, exit-code mapping, location rules with fakes; trait `WU=404`.
- Integration: `tests/Tailor.IntegrationTests/Cli/AnalyseValidateTests` copying matrix apps to temp dirs (never run against `artifacts/testapps` in place), before/after `TreeFingerprint`; `ToolPackTests` for AC-12; trait `Category=Integration`, `Category=Matrix`, `WU=404`.
- Run: `dotnet test --project tests/Tailor.Cli.Tests --filter-trait "WU=404"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=404"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; M4 criteria 1, 4, 5 and the local-smoke half of 7 demonstrably covered; changes limited to target paths (plus the plan status row).

## Agent Notes

- Invoke commands in-process for most tests (System.CommandLine `InvokeAsync` with captured console); keep one out-of-process run via the packed tool.
- AC-6 must also cover a read-only copy (`--spec-out` + `--artefacts` outside) to prove zero writes.

## Open Questions

- Overwrite policy for an existing AppSpec (provisional: refuse; alternatives `--force` or write alongside).
- **Resolved** — pack/install criterion overlap: this WU covers the local smoke (AC-12); WU-1002 (scheduled immediately after this WU) covers publishing.
