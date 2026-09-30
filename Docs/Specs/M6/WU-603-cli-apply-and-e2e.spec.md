# WU-603 cli-apply-and-e2e

| Field | Value |
|---|---|
| ID | WU-603 |
| Title | cli-apply-and-e2e |
| Milestone | M6 Execution Engine (v0.2.0-preview) |
| Status | Not started |
| Depends on | WU-602, WU-506, WU-405, WU-604 |
| Parallel with | WU-702, WU-703 |
| Target project(s)/paths | `src/DotNetRepack.Cli/Commands/Apply/`, `src/DotNetRepack.Cli/Composition/`, `tests/DotNetRepack.Cli.Tests/Apply/`, `tests/DotNetRepack.IntegrationTests/Cli/Apply/`, `tests/DotNetRepack.IntegrationTests/TransformSpecs/`, `tests/DotNetRepack.IntegrationTests/Support/` (launch helper) |
| Size | L |
| Branch / PR | `wu/603-cli-apply-and-e2e` / `WU-603: cli-apply-and-e2e` |

## Goal

Ship `dotnet-repack apply` end to end: pre-flight safety, plan, execute, post-validate, commit, reports, exit codes and Ctrl+C handling. Prove the v0.2.0-preview capability with end-to-end tests on the test-app matrix for filtering, layout, symbols (directory and zip), docs and culture pruning, including launch smoke runs in the test harness.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [RQ §4.3](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §9](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | New output + new AppSpec, directory rules, dotnet-style CLI, safe failure |
| [RQ §2.2](../../Requirements/Repackage_tool_Requirements_v1.1.md) | No application execution by the tool (launching happens only in tests) |
| [TS §2.4](../../Requirements/Transformation_Specification.md#2-lifecycle), [TS §24](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies), [TS §25.4](../../Requirements/Transformation_Specification.md#25-output-state-requirements) | Execution lifecycle, policies, output assertions |
| [TS §15](../../Requirements/Transformation_Specification.md#15-resource-and-localisation-policy), [TS §16](../../Requirements/Transformation_Specification.md#16-debug-symbol-policy), [TS §17](../../Requirements/Transformation_Specification.md#17-documentation-policy), [TS §18](../../Requirements/Transformation_Specification.md#18-file-and-folder-layout-rules) | E2E scenarios |
| Architecture [§4](../../Architecture/DotNetRepack.architecture.md#4-processing-pipeline), [§11](../../Architecture/DotNetRepack.architecture.md#11-execution-and-safety), [§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§14](../../Architecture/DotNetRepack.architecture.md#14-cli), [§16](../../Architecture/DotNetRepack.architecture.md#16-testing-strategy), [§19](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) item 21 | Pipeline, exit codes, synopsis, launch smoke in harness only |
| Plan M6 criteria 1–4; release point v0.2.0-preview | AC-4–AC-13 |

## Scope

**In**
- `apply <appDir> --spec <file> --transform <file> --output <dir> [--dry-run] [--spec-out <file>] [--symbols-output <path>]` plus global options (architecture §14).
- Composition: `PlanPipeline` (WU-506) → `OutputSafetyGuard` (WU-600) → `ExecutionPipeline` (WU-602). Dry-run also runs the pre-flight safety checks, without creating anything.
- Ctrl+C: `Console.CancelKeyPress` → cancel the token → rollback. A second Ctrl+C is not intercepted.
- Exit-code mapping for every stage.
- E2E scenario suite, launch smoke helper, v0.2.0 readiness checklist.

**Out**
- R2R, FD⇄SC, retarget and patch scenarios (M7–M9). Release publishing (WU-1002). Config file (WU-1000). `apply --plan` replay.

## Deliverables

| Item | Detail |
|---|---|
| `Cli.Commands.Apply.ApplyCommand` | Replaces the WU-105 stub and the WU-506 dry-run-only branch |
| `Cli.Composition` | Registers Platform.Windows `IPathCanonicaliser`, WU-504 and WU-507 handlers, the WU-604 deps.json pruning handler and modifier, projection handler, `NoAcquisitionPlanner`, WU-601 executors |
| `Support/LaunchSmoke` (tests only) | Starts `<output>/<host>.exe --smoke` (WU-003 contract) with `ArgumentList`, a 30 s timeout and captured output. Asserts exit 0 |
| Scenario TransformSpecs (`tests/DotNetRepack.IntegrationTests/TransformSpecs/`) | Reuse the WU-504 set (`filtering`, `docs`, `resources-en`, `symbols-dir`, `symbols-zip`, `other-rid`) and add `symbols-preserve`, `symbols-exclude`, `layout`, each with `output.assert` where applicable |
| `ApplyE2ETests` | Scenario × matrix entries below |
| v0.2.0 readiness | Checklist in the PR: M6 criteria mapped to test names; `--help` for `apply` updated in snapshots |

**Exit codes** (architecture §13)

| Exit | Stage / condition |
|---|---|
| 0 | Committed; warnings only (non-strict) |
| 1 | Input AppSpec invalid, TransformSpec/assertion/conflict/collision/safety errors, projected validation failure, **output-path safety violations** (`RPK600x`, structural) |
| 2 | Usage errors, missing input files |
| 3 | `--strict` with warnings (nothing is committed; see Design Notes) |
| 4 | Acquisition failure |
| 5 | Execution, post-validation (including failed output assertions) or commit failure (output rolled back) |
| 70 | Internal error (rollback still guaranteed by `StagingSession` dispose) |
| 130 | Cancelled (Ctrl+C); output rolled back |

**E2E matrix** (copies of `artifacts/testapps`, never in place)

| Scenario | Entries | Checks |
|---|---|---|
| `filtering`, `docs`, `resources-en` | ConsoleApp, WinFormsApp, WpfApp × net8/net10 × fd/sc × il | Exit 0; output AppSpec validates; assertions pass; ConsoleApp launch smoke exits 0 |
| `symbols-dir`, `symbols-zip` | ConsoleApp, PluginHost × net10 × fd/sc × il | No PDB in the output; all PDBs of retained binaries in the symbols output; zip byte-identical across two runs |
| `layout` | ConsoleApp net10 fd il | Moved files at the new paths; output validates |
| `other-rid` | ConsoleApp `net8.0-fdportable-il`, `net10.0-fdportable-il` | Only non-`win-x64`-compatible `runtimes/<rid>/` removed; launch smoke exits 0 |
| Safety | ConsoleApp net10 fd il | Input = output, nested, junction-aliased, non-empty output → rejected before any write |
| Failure injection | ConsoleApp net10 fd il | Executor fault (DI-registered faulting executor), failed output assertion → exit 5; cancellation → exit 130; no output, no staging |

## Design Notes

- Launching is a test-harness function only ([architecture §19](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) item 21). The tool never starts the application.
- `--strict` promotes warnings before commit. A run that would exit 3 must roll back, so a committed output always means exit 0.
- Dry-run adds the WU-600 pre-flight checks to the WU-506 path, and still creates nothing.
- GUI apps (WinForms/WPF) are validated structurally. Launch smoke runs only for console apps in M6 (plan M6 criterion 1). GUI `--smoke` can be added under `Category=Launch` when stable.
- The ConsoleApp net8 FD smoke needs the .NET 8 runtime on the agent. Mark it `Category=Launch` and skip with an explicit reason when the runtime is missing locally. CI must not skip.

## Acceptance Criteria

- [ ] AC-1 `apply --help` matches the architecture §14 synopsis (Verify snapshot).
- [ ] AC-2 `apply --dry-run` with a non-empty `--output` exits 1 with `RPK6003` and writes only artefacts. With a valid output path it creates no output (reuses the WU-506 AC-6 harness).
- [ ] AC-3 `apply` with `filtering` on ConsoleApp net10 FD exits 0, creates `<output>/repack.appspec.json`, and leaves the input tree fingerprint (including sidecars) unchanged.
- [ ] AC-4 For every `filtering`/`docs`/`resources-en`/`symbols-dir`/`symbols-zip` entry in the E2E matrix, `apply` exits 0 and a subsequent `validate <output> --spec <output>/repack.appspec.json` exits 0 (M6 criterion 1).
- [ ] AC-5 ConsoleApp outputs of `filtering`, `resources-en`, `symbols-dir`, `symbols-zip` and `other-rid` launch with `--smoke` and exit 0 (net8 and net10, FD and SC).
- [ ] AC-6 `symbols-zip` produces a byte-identical zip on two runs into fresh output paths (M6 criterion 4). `symbols-dir` mirrors PDB relative paths.
- [ ] AC-7 Input = output, output nested in input, output as a junction to the input, and a non-empty output each exit 1 with the WU-600 code, and no staging or output is created (M6 criterion 2).
- [ ] AC-8 An injected executor fault and an injected failed output assertion each exit 5 with no output directory and no `*.staging-*` sibling (M6 criterion 3).
- [ ] AC-9 Cancelling during execution (test triggers the same token as Ctrl+C) exits 130, with no output or staging remaining.
- [ ] AC-10 `--strict` with a warning-producing scenario (selector matches nothing) exits 3 and leaves no output.
- [ ] AC-11 `--spec-out <file>` places the output AppSpec there and not inside the output.
- [ ] AC-12 Two `apply` runs of the same scenario into different output paths produce identical output trees (sorted `(path, size, sha256)` fingerprint) and identical output AppSpecs.
- [ ] AC-13 The Verifier can tick plan M6 criteria 1–4 from the test names listed in the PR's readiness checklist.

## Test Requirements

- Unit: `tests/DotNetRepack.Cli.Tests/Apply/` (parsing, composition, exit mapping, cancellation wiring). Trait `WU=603`.
- E2E: `tests/DotNetRepack.IntegrationTests/Cli/Apply/`, in-process CLI over matrix copies. Traits `Category=Integration`, `Category=Matrix`, `WU=603`. Launch tests also carry `Category=Launch`.
- Run: `dotnet test --project tests/DotNetRepack.Cli.Tests --filter-trait "WU=603"`, `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=603"`.
- Requires `build/Build-TestApps.ps1` output. Locally, skip with a reason when it is absent; CI fails when it is absent.
- Record Test Evidence below and in the PR (matrix manifest hash, scenario × entry count, launch results).

## Definition of Done

- Zero warnings, tests green (including `Category=Launch` in CI), format clean.
- All ACs ticked by the Verifier. Plan status `Done`. M6 criteria ticked. Ready for the v0.2.0-preview tag once WU-1002 is available (plan risk R11).

## Agent Notes

- Reuse the WU-404/WU-506 in-process CLI helpers and the WU-405 `MatrixFixture`. WU-704 and WU-805 will reuse `LaunchSmoke` and the scenario set, so keep them in `tests/DotNetRepack.IntegrationTests/Support/`.
- Register fault-injecting executors only in tests, through `CliApplication.RunAsync(..., services)`.

## Open Questions

- **Resolved** — WU-405 dependency (`MatrixFixture`): added to the plan.
- **Resolved** — cancellation exit code: 130.
- **Resolved** — output-path safety violations: exit 1 (structural, TS §24.4).
- **Resolved** — deps.json pruning for v0.2.0: WU-604 (dependency) prunes removed assets; WU-802 builds on it.

## Test Evidence

_To be completed by the implementer._
