# WU-405: regression-harness

| Field | Value |
|---|---|
| ID | WU-405 |
| Title | regression-harness |
| Milestone | M4 Analysis & Validation |
| Status | Not started |
| Depends on | WU-404, WU-003, WU-002 |
| Parallel with | WU-406, M5, WU-1002 |
| Target project(s)/paths | `tests/Tailor.RegressionTests/`, `.github/workflows/` (regression job only) |
| Size | M |
| Branch / PR | `wu/405-regression-harness` / `WU-405: regression-harness` |

## Goal

Provide a regression harness that runs `analyse` + `validate` over the full test-app matrix against golden files, and runs edited-spec/edited-tree scenarios that must produce expected diagnostics and exit codes, wired into CI.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [AS §2.3](../../Requirements/Application_Specification.md#23-user-refinement), [AS §20.4](../../Requirements/Application_Specification.md#204-manual-modification) | User edits must be revalidated and detected |
| [AS §3.6](../../Requirements/Application_Specification.md#36-determinism), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Determinism, testability |
| Architecture [§16](../../Architecture/Tailor.architecture.md#16-testing-strategy) | Regression tests over the matrix with golden files |
| Plan M4 criteria 1, 3 | AC-2, AC-4 |

## Scope

**In**: matrix enumeration from `manifest.json`, golden files for all analyse/validate outputs, determinism check, scenario runner (spec edits + tree edits), CI job.

**Out**: new engine behaviour; any failing scenario is fixed in the owning WU, not here.

## Deliverables

| Item | Detail |
|---|---|
| `tests/Tailor.RegressionTests/MatrixFixture` | Reads `artifacts/testapps/manifest.json`; copies each entry to a temp dir; exposes the non-deterministic file list for scrubbing |
| `AnalyseValidateRegressionTests` | Per entry: `analyse --spec-out <tmp> --artefacts <tmp>` → `validate`; golden files of draft AppSpec, all artefacts, `capabilities.json`, `validation-report.json` (`.golden.json`) and console output (`.golden.txt`) |
| `DeterminismTests` | Two runs per entry; byte comparison of every output |
| `Scenarios/*.scenario.json` + `ScenarioRunner` | `{ app, specEdits: [{op, path, value}] (JSON Patch subset add/replace/remove), treeEdits: [{op: add\|remove\|copy, path, from?}], expect: { codes: [], exitCode } }` applied to a golden draft and a temp tree copy |
| CI job | `.github/workflows/` job running `Tailor.RegressionTests` on `windows-latest` after restoring the matrix cache; TRX upload |

**Required scenarios**

| Scenario | Edit | Expected |
|---|---|---|
| wrong-plugin-boundary | plugins definition role → `content`, drop its references | `TLR3301`, exit 1 |
| wrong-role | set a folder holding non-satellite managed assemblies (e.g. a plugin folder) to role `resources` | `TLR4307`, exit 1 |
| missing-catch-all | `classifications.catchAll: null` | `TLR3102`, exit 1 |
| bad-reference-path | root reference → `folder:doesNotExist` | `TLR3305`, exit 1 |
| missing-required-association | mark `symbols` association required; remove one `.pdb` from tree | `TLR3201`, exit 1 |
| removed-file | remove an app assembly referenced by the entry assembly | `TLR3301`, exit 1 |
| added-file | copy a plugin assembly into a second plugin folder with `duplicates: error` | `TLR3303`, exit 1 |
| cyclic-plugins | cyclic plugin test app with its draft | `TLR3401`, exit 1 |
| strict-warnings | FD app (unverified framework refs `TLR3302` while the catalogue is empty) with `--strict` | exit 3 |

## Design Notes

- Never write into `artifacts/testapps`; always copy to temp.
- Scrub only: tool version in `generator`, hashes of files listed as non-deterministic in the WU-003 manifest and values derived from them (`treeFingerprint`, `specHash` where affected). Scrubbers are listed in one place.
- Golden files live under `tests/Tailor.RegressionTests/Golden/AnalyseValidateRegressionTests/<app>/<variant>/<output>.golden.json` (`name` = `<app>/<variant>/<output>`).
- Scenario expectations assert code presence and exit code; message text is compared against separate golden files.

## Acceptance Criteria

- [ ] AC-1 The harness enumerates every matrix entry from `manifest.json`; a missing entry fails the run (no silent skip in CI).
- [ ] AC-2 `analyse` → `validate` exits 0 with zero errors for every matrix entry except variants flagged `expectedInvalid` in the WU-003 manifest, which produce exactly their listed codes (the cyclic plugin variant, also covered by the `cyclic-plugins` scenario).
- [ ] AC-3 Golden files exist for every entry and all outputs listed in Deliverables; the suite passes against them.
- [ ] AC-4 All nine required scenarios produce their expected codes and exit codes.
- [ ] AC-5 Determinism: two runs per entry are byte-identical for all outputs, with no scrubbing applied.
- [ ] AC-6 The input copy's fingerprint (excluding sidecars) is unchanged after each run.
- [ ] AC-7 The CI workflow runs the regression job on pull requests and fails the build on golden-file or scenario mismatch; TRX results are uploaded.
- [ ] AC-8 Adding a scenario requires only a new `*.scenario.json` file (demonstrated by the nine data-driven scenarios).

## Test Requirements

- xUnit v3 + golden files (`Tailor.Testing.Golden`); traits `Category=Integration`, `Category=Matrix`, `WU=405`.
- Run: `dotnet test --project tests/Tailor.RegressionTests --filter-trait "WU=405"`.
- Requires `build/Build-TestApps.ps1` output; locally, skip with an explicit reason when absent; CI fails when absent.
- Record Test Evidence (matrix manifest hash, entry count, scenario count, pass/fail) in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI regression job green on `main`; M4 criteria 1 and 3 ticked when WU-404 is also Done; changes limited to target paths (plus the plan status row).

## Agent Notes

- Reuse `TreeFingerprint` and CLI in-process invocation helpers from WU-404 tests rather than duplicating them (move shared helpers into a small test-support source folder if needed).
- If CI time is excessive (risk R7), gate the full matrix to nightly and keep one app × all variants on PRs; record the decision in the PR.

## Open Questions

- **Resolved** — WU-002 dependency: added to the plan.
- Plan M4 criterion 3 lists "added file"; with a catch-all an added file is valid unless it breaks a rule. The `added-file` scenario above uses a duplicate-candidate interpretation; confirm.
- Scenario `missing-catch-all` requires the WU-101 schema to allow an explicit null catch-all (see WU-301).
