# WU-506 plan-artefacts-and-cli-plan

| Field | Value |
|---|---|
| ID | WU-506 |
| Title | plan-artefacts-and-cli-plan |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-504, WU-505, WU-404, WU-507 |
| Parallel with | WU-602, WU-604 |
| Target project(s)/paths | `src/DotNetRepack.Planning/Artefacts/`, `schemas/plan/v1/plan.schema.json`, `src/DotNetRepack.Cli/Commands/Plan/`, `src/DotNetRepack.Cli/Commands/Apply/` (dry-run path only), `src/DotNetRepack.Cli/Composition/`, `tests/DotNetRepack.Planning.Tests/Artefacts/`, `tests/DotNetRepack.Cli.Tests/`, `tests/DotNetRepack.IntegrationTests/Cli/Plan/` |
| Size | M |
| Branch / PR | `wu/506-plan-artefacts-and-cli-plan` / `WU-506: plan-artefacts-and-cli-plan` |

## Goal

Serialise planning results into deterministic artefacts (`*.plan.json` with schema `plan/v1`, per-file action report, projected AppSpec). Ship `plan` and `apply --dry-run` end to end, with a test-enforced guarantee that they make zero filesystem mutations outside `--artifacts` and the NuGet package cache (the cache only when not `--offline`).

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [RQ §7](../../Requirements/Repackage_tool_Requirements_v1.1.md), [TS §26](../../Requirements/Transformation_Specification.md#26-dry-run-and-planning-behaviour) | Dry-run without mutation; projected AppSpec, plan, per-file action report |
| [TS §31](../../Requirements/Transformation_Specification.md#31-relationship-to-transformation-plans-and-logs), [TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials), [TS §3.6](../../Requirements/Transformation_Specification.md#3-design-principles) | Plan content, pinned external identities, determinism |
| [RQ §6](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Ordering reflected in outputs; separate artefacts; auditable, deterministic |
| [TS §5](../../Requirements/Transformation_Specification.md#5-specification-identity-and-schema) | Versioned schemas (applied to `kind: Plan`) |
| Architecture [§5](../../Architecture/DotNetRepack.architecture.md#5-artefacts), [§6.1](../../Architecture/DotNetRepack.architecture.md#61-common-rules), [§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§14](../../Architecture/DotNetRepack.architecture.md#14-cli), [§15](../../Architecture/DotNetRepack.architecture.md#15-determinism) | Artefact set, header rules, exit codes, verbs, canonical JSON |
| Plan M5 criteria 1, 3 | AC-6–AC-9 |

## Scope

**In**
- Plan DTOs and the canonical writer. Plan schema generation, commit and drift test. `schema export plan` enabled (replaces `RPK0104`).
- Per-file action report and output file manifest.
- `plan <appDir> --spec <file> --transform <file> [--out-plan <file>]` and `apply … --output <dir> --dry-run` (same pipeline; `--output` is recorded, never touched).
- `PlanPipeline` composition: load AppSpec → validate (WU-403) → load TransformSpec + `--var` (WU-103/104) → WU-502 → WU-503 (with WU-504/505/507 handlers) → write artefacts.
- Console summary (counts per action kind and phase, diagnostics).

**Out**
- `apply` execution, output-path safety checks and staging (WU-600–WU-603).
- `apply --plan <file>` replay (open question in the plan).

## Deliverables

| Item | Detail |
|---|---|
| `Planning.Artefacts.PlanDocument` | `$schema`, `kind: Plan`, `schemaVersion: 1.0`, `generator`, `inputs {appSpecHash, treeFingerprint, transformSpecHash, variables[] {name, value, source}}`, `targetState`, `acquisitions[] {id, version, source, sha512}`, `phases[] {phase, handlers[]}`, `actions[]` (WU-503 `PlanAction`, sorted by `order`), `projectedAppSpecHash`, `outputManifest[] {root, path, size?, sha256?}`, `diagnostics[]` |
| `schemas/plan/v1/plan.schema.json` | Generated with the WU-101 `SchemaGenerator`. Drift test as in WU-101/102 |
| `PlanArtefactWriter.WriteAll(PlanResult, ArtefactPaths)` | Writes `<name>.plan.json`, `<name>.projected.appspec.json`, `action-report.json` |
| `action-report.json` | Per input and output file: `path`, `action`, `destination?`, `phase`, `ruleId?`, `handler`, `reason` (TS §26.4 wording: copy, add, remove, replace, move, modify configuration, optimise, extract to symbols output, preserve unchanged). Sorted by path |
| `Cli.Commands.Plan.PlanCommand` | Replaces the WU-105 stub action |
| `Cli.Commands.Apply.ApplyCommand` (dry-run branch) | `--dry-run` runs `PlanPipeline` and exits. Without `--dry-run` it keeps the stub (`RPK0100`) until WU-603 |
| Locations | `--out-plan` or `<artifacts>/repack.plan.json`. Artefacts dir = `--artifacts`, else `.repack/` next to the AppSpec (architecture §5). A non-writable artefacts dir → exit 2 |
| `PlanCliDiagnostics` | `RPK0410`–`RPK0419` (CLI range, as in WU-404) |

**Exit codes** (architecture §13)

| Exit | Condition |
|---|---|
| 0 | Plan produced; warnings only (non-strict) |
| 1 | Input AppSpec invalid, TransformSpec semantic/assertion/conflict/collision/safety errors, projected validation failure |
| 2 | Usage errors, missing files, non-writable artefacts location |
| 3 | `--strict` with warnings |
| 4 | Acquisition failure (`RPK5305`, offline miss) |
| 130 | Cancelled (Ctrl+C); only partial artefacts may remain |

## Design Notes

- **Determinism.** No timestamps, absolute paths, machine names or durations in the plan, projected AppSpec or action report. The `generator.version` is the only tool-dependent value. Hashes use WU-100 `ContentHash`. `transformSpecHash` = SHA-256 of the canonical merged, **resolved** TransformSpec.
- **Pinning.** Every external identity (acquisitions, resolved variables, target versions) is written as an exact value, never as a policy ([TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials)).
- `apply --dry-run` accepts `--output` and `--symbols-output` but only records them. Pre-flight output-path checks are added in WU-603.
- On errors, the plan is still written (with diagnostics) when planning reached phase 9, to aid diagnosis. Otherwise only `validation-report.json`/diagnostics are written.
- The plan is not authoritative ([TS §31](../../Requirements/Transformation_Specification.md#31-relationship-to-transformation-plans-and-logs)). No engine component reads it back in v1.

## Acceptance Criteria

- [ ] AC-1 `schemas/plan/v1/plan.schema.json` is committed and equals the generated schema (drift test). `schema export plan` writes it byte-identically and exits 0.
- [ ] AC-2 A plan for the WU-504 `filtering` scenario is schema-valid, has `kind: Plan`, and lists every action with `phase`, `order`, `provenance`.
- [ ] AC-3 `action-report.json` lists every in-scope input file and every output file exactly once, with the TS §26.4 action wording (coverage test).
- [ ] AC-4 `inputs.variables` records each resolved variable with its source. `acquisitions` records exact versions and sha512 (fake acquisition planner).
- [ ] AC-5 `plan` without `--out-plan` writes `<artifacts>/repack.plan.json`, `repack.projected.appspec.json` and `action-report.json`. `--out-plan <file>` writes the plan there.
- [ ] AC-6 **Zero mutation** (M5 criterion 1): for each scenario (filtering, symbols dir, symbols zip, docs, resources, additions), on a temp copy of a matrix app with `--artifacts` outside the app, `plan` and `apply --dry-run --output <tmp>/out --symbols-output <tmp>/sym` leave (a) the input tree fingerprint **including sidecars and empty directories** unchanged, (b) `<tmp>/out` and `<tmp>/sym` non-existent, and (c) no new entries anywhere under `<tmp>` except the artefacts directory (before/after directory snapshot). The global packages folder is redirected to an isolated temp cache and must also be unchanged (no handler acquires in v0.2.0).
- [ ] AC-7 Two `plan` runs over the same inputs produce byte-identical `*.plan.json`, projected AppSpec and `action-report.json` (M5 criterion 3).
- [ ] AC-8 The plan contains no absolute path of the temp root, no `Environment.MachineName` and no ISO-8601 timestamp (grep test).
- [ ] AC-9 Scenario exit codes: equal-precedence conflict → 1 (`RPK5101`); output collision → 1 (`RPK5301`); unsafe removal → 1 (`RPK5302`); failed input assertion → 1 (`RPK4405`); warnings + `--strict` → 3; missing `--transform` file → 2.
- [ ] AC-10 `apply` without `--dry-run` still returns the WU-105 not-implemented result (unchanged until WU-603).
- [ ] AC-11 `plan` and `apply --dry-run` produce identical artefacts for the same inputs.

## Test Requirements

- Unit: `tests/DotNetRepack.Planning.Tests/Artefacts/` (DTO mapping, canonical form, schema drift) and `tests/DotNetRepack.Cli.Tests/` (parsing, locations, exit mapping). Trait `WU=506`.
- Integration: `tests/DotNetRepack.IntegrationTests/Cli/Plan/`, in-process CLI over matrix copies (never in place) with WU-504 scenario TransformSpecs. Traits `Category=Integration`, `Category=Matrix`, `WU=506`.
- Run: `dotnet test --project tests/DotNetRepack.Planning.Tests --filter-trait "WU=506"`, `dotnet test --project tests/DotNetRepack.Cli.Tests --filter-trait "WU=506"`, `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=506"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean, schema committed with no drift. All ACs ticked by the Verifier. Plan status `Done`. M5 criteria 1 and 3 demonstrably covered.

## Agent Notes

- Reuse the WU-404 CLI in-process helpers and `TreeFingerprint`. Build the directory-snapshot helper for AC-6 in a shared test-support folder so that WU-603 and WU-704 can reuse it.
- Register `NoAcquisitionPlanner` in the Cli composition for v0.2.0.

## Open Questions

- **Resolved** — WU-404 dependency (composition root; WU-105 stubs are transitive): added to the plan.
- Artefact file names (`repack.plan.json`, `repack.projected.appspec.json`, `action-report.json`) are proposals. Architecture §5 only fixes `*.plan.json`.
- **Resolved** — dry-run wording: zero mutations outside `--artifacts` and the NuGet package cache (cache only when not `--offline`); plan M5 criterion 1 and architecture §19 item 28 updated.

## Test Evidence

_To be completed by the implementer._
