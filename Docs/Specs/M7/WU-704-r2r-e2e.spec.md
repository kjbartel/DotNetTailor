# WU-704 r2r-e2e

| Field | Value |
|---|---|
| ID | WU-704 |
| Title | r2r-e2e |
| Milestone | M7 Acquisition & ReadyToRun (v0.3.0-preview) |
| Status | Ready |
| Depends on | WU-703, WU-603 |
| Parallel with | M8 |
| Target | `tests/Tailor.IntegrationTests/ReadyToRun/`, `tests/Tailor.IntegrationTests/Fixtures/r2r/*.transform.json`, `src/Tailor.Cli/Composition/` (Acquisition wiring); fixes in `src/Tailor.*` only where E2E exposes defects |
| Size | M |

## Goal

Prove M7 end to end through the CLI: `plan` and `apply` with an R2R TransformSpec over the FD and SC test apps (including the plugin app) produce R2R outputs that launch, explain every managed assembly, are deterministic and honour `--offline`.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §14](../../Requirements/Transformation_Specification.md#14-optimisation-specification), [TS §25](../../Requirements/Transformation_Specification.md#25-output-state-requirements), [TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials) | R2R scope, output assertions, pinned tooling |
| [RD §8](../../Requirements/R2R_tool_Design.md) | Eligibility states, plugins without co-compilation |
| [CK §6.2](../../Requirements/Read_to_run_Cake.md#6-target-framework-and-runtime-requirements) | crossgen2 R2R for `win-x64` |
| [RQ §5.4](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Semantics unchanged (launch) |
| [Plan M7 criteria](../../Plans/Tailor.plan.md#m7-acquisition--readytorun--v030-preview), [Architecture §16](../../Architecture/Tailor.architecture.md#16-testing-strategy) | Milestone gate, harness-only launch |

## Scope

**In**
- Scenarios from the R2R-off matrix inputs: console, WinForms, WPF, plugin host (one-way chain) × {net8.0, net10.0} × {FD, SC}.
- R2R-on matrix inputs: app assemblies reported `Skipped(AlreadyReadyToRun)`.
- Selector exclusion scenario (e.g. exclude one plugin assembly) → `Skipped(NotSelected)`.
- Optional SC `composite` scenario if WU-004 recommends it.
- `--offline` scenarios (warm cache succeeds; empty cache → exit 4).
- Launch smoke via the test-app smoke contract from WU-003 (harness only).
- Cli composition: register the Acquisition-backed `IAcquisitionPlanner` adapter (WU-702), `IPackageLocator` (WU-700), `IFrameworkCatalogue` (WU-701), the R2R handler and the `Optimise` executor (architecture §3.2).

**Out**
- New tool features. Combined R2R + retarget/patch (WU-903). Performance budgets (WU-1004).

## Deliverables

- TransformSpec fixtures (R2R all app/plugin assemblies; exclusion variant; composite SC variant if applicable).
- Integration tests + golden files of plans and eligibility reports.
- Defect fixes in the owning `src` project with focused unit tests.

## Design Notes

- Follow WU-004 spike/ADR; **spike/ADR decisions override this spec where they differ** (e.g. determinism fallback, composite support).
- Use the WU-405 regression harness and WU-603 CLI test helpers; do not create a parallel harness.
- Determinism check: run `apply` twice into different output directories; compare the tree fingerprint (`relativePath`, size, sha256) and the plan bytes.
- Crossgen2 major check reads the pinned package in the plan and the execution report.
- Optional semantic cross-check: the set of R2R app/plugin assemblies equals that of the SDK R2R-on counterpart in the matrix.

## Acceptance Criteria

- [ ] AC-1 For every scenario, every `Planned` assembly is reported as R2R by the WU-200 inspector in the output.
- [ ] AC-2 The plan's eligibility report has exactly one entry per managed output assembly; every Skipped/Ineligible entry has a reason (covers already-R2R, not selected and framework assemblies; mixed-mode/reference/composite via fixtures if absent from the matrix).
- [ ] AC-3 Two `apply` runs per scenario produce identical output tree fingerprints and byte-identical plans.
- [ ] AC-4 net8 targets pin a crossgen2 8.x package and net10 targets a 10.x package (plan + execution report).
- [ ] AC-5 Plugin units reference upstream plugins only; no unit contains more than one input assembly (non-composite scenarios).
- [ ] AC-6 All R2R outputs launch via the smoke contract (console exits 0; GUI apps per WU-003 contract).
- [ ] AC-7 The output AppSpec validates and output assertions requiring R2R pass.
- [ ] AC-8 Acquired packages appear in the plan with id, version, source and sha512.
- [ ] AC-9 `apply --offline` with an empty isolated cache exits with code 4 and leaves no output or staging directory.
- [ ] AC-10 `apply --offline` with a warm cache succeeds and makes no network requests.
- [ ] AC-11 R2R-on inputs report app assemblies as `Skipped(AlreadyReadyToRun)` and produce no `Optimise` actions for them.
- [ ] AC-12 With the Acquisition services wired, FD `validate` on a warm cache reports framework references as verified (no `TLR3302`).

## Test Requirements

- xUnit v3 + golden files in `tests/Tailor.IntegrationTests/`, tagged `Category=Integration`, `Category=Matrix` (and `Category=Launch` for smoke runs so CI can tier them); trait `WU=704`.
- Run: `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=704"`.
- `LocalPackageFeedFixture` with an isolated global packages folder seeded from `Build-TestApps.ps1` restores; no network in default runs.
- Record Test Evidence (scenario list, fingerprints, results) in the PR.

## Definition of Done

- All AC ticked by the Verifier; all M7 plan criteria demonstrably met; CI green.
- Any `src` fix has its own unit test and is noted in the PR.

## Agent Notes

- Keep the scenario list data-driven (theory data from `manifest.json`).
- GUI launch must not hang CI: use the smoke contract's timeout and auto-exit.

## Open Questions

- Does the WU-003 smoke contract support auto-exit for WinForms/WPF apps? If not, GUI launch checks need a harness extension.
- Is a mixed-mode (C++/CLI) test app available (architecture §16 marks it optional)? If not, AC-2 mixed-mode coverage uses a checked-in fixture.
