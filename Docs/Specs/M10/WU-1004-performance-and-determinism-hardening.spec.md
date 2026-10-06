# WU-1004 performance-and-determinism-hardening

| Field | Value |
|---|---|
| ID | WU-1004 |
| Title | performance-and-determinism-hardening |
| Milestone | M10 Configuration, Hardening & Release → v1.0.0 |
| Status | Ready |
| Depends on | WU-903 |
| Parallel with | WU-1003 |
| Target project(s)/paths | `tests/Tailor.PerformanceTests/` (new), `tests/Fixtures/Synthetic/` (generator), `tests/Tailor.Inspection.Tests/` + `tests/Tailor.Specifications.Tests/` (fuzz), `.github/workflows/determinism.yml`, `src/Tailor.Inspection/`, `src/Tailor.Model/` (parallelism, memory fixes), `Docs/Decisions/` (budget ADR) |
| Size | L |

## Goal

Set and enforce a performance budget on a large synthetic app (thousands of files). Make inspection parallel while keeping the merge deterministic. Bound memory. Prove determinism across machines in CI. Harden the PE, JSON and XML readers against malformed input with fuzz tests.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Determinism, safe failure, testability |
| [TS §3.6](../../Requirements/Transformation_Specification.md), [TS §32.7](../../Requirements/Transformation_Specification.md) | Deterministic plans |
| [CK §4.3](../../Requirements/Read_to_run_Cake.md) | Output equivalence for identical inputs (same tooling versions) |
| [Architecture §15, §16, §17](../../Architecture/Tailor.architecture.md) | Determinism rules, test strategy, untrusted binary parsing |
| [Plan risks R1, R7, R12; M10 criteria](../../Plans/Tailor.plan.md) | crossgen2 determinism, CI time, large trees, perf budget + matrix determinism |

## Scope

**In**
- Synthetic app generator (deterministic from a seed): emits a tree of ≥ 10,000 files with ~2,000 minimal managed assemblies (built with `System.Reflection.Metadata` `MetadataBuilder`/`ManagedPEBuilder`, real references between them), native stubs, 20+ satellite cultures, multi-RID `runtimes/`, plugin folders (≥ 50 plugins, one-way chain), config/content files, and a matching AppSpec. The generator does not use the SDK.
- Performance budget (proposed; calibrate, then fix in an ADR) on GitHub `windows-latest`:

| Verb (synthetic tree, warm cache, no R2R) | Wall time | Peak working set |
|---|---|---|
| `analyse` | ≤ 60 s | ≤ 1.5 GB |
| `validate` | ≤ 30 s | ≤ 1.0 GB |
| `plan` (filtering + symbols + resources) | ≤ 30 s | ≤ 1.0 GB |
| `apply` (same TransformSpec) | ≤ 90 s | ≤ 1.0 GB |

- Parallelism: parallel file hashing and PE inspection with a bounded degree (default `Environment.ProcessorCount`, overridable by a hidden/advanced option or config key), merged into sorted results. Results for parallelism 1 and N are byte-identical.
- Memory: streaming SHA-256, `PEReader` over file streams (no whole-file buffers for large files), no retained file contents in the EAM.
- Cross-machine determinism workflow (`determinism.yml`, nightly + manual): two jobs on separate runners with different working-directory roots, `TEMP`, user culture (`en-US` vs `de-DE`) and time zone. Each runs the full matrix scenarios (analyse, validate, plan, apply for the M5–M9 scenarios) and uploads SHA-256 manifests of canonical artefacts and output trees. A third job compares the manifests and fails on any difference outside the documented exclusion list.
- Fuzz/robustness (seeded mutational fuzzing in xUnit; fixed-seed short run on PR, longer run nightly):
  - PE: bit flips, truncation, corrupted headers/directories/metadata tables and R2R header over a corpus of matrix binaries.
  - JSON: AppSpec, TransformSpec, plan, tool config, runtimeconfig, deps.json. Covers truncation, deep nesting (beyond `MaxDepth`), huge strings/numbers, duplicate properties, invalid UTF-8.
  - XML: `RuntimeList.xml` and nuspec, with DTD processing prohibited (XXE test).
  - Invariant: only diagnostics, no unhandled exception, each input completes within a timeout, bounded memory.

**Out**
- Linux runners. R2R compilation speed (crossgen2 time is excluded from the budget and reported separately). Continuous benchmarking dashboards.

## Deliverables

- Generator + perf tests with budget assertions + a timing report artefact in CI.
- Parallel inspection/hashing changes with determinism tests.
- `determinism.yml` + manifest compare tool/script + exclusion list (shared with the WU-003 manifest exclusions).
- Fuzz tests + seed corpus + any reader fixes (each with a regression input).
- ADR `ADR-NNNN-performance-budget.md` with measured baseline and final budget.

## Design Notes

- [Architecture §15](../../Architecture/Tailor.architecture.md) guarantees determinism for equal inputs **and equal environment**. [CK §4.3](../../Requirements/Read_to_run_Cake.md) does not require equivalence across tooling versions. The cross-machine job therefore pins the SDK (`global.json`), package versions (plan pinning) and the runner image. Differences in path, culture, TEMP and time zone must not change results.
- crossgen2 output follows the WU-004 ADR (plan risk R1). If byte determinism is not guaranteed, compare R2R files semantically as the ADR defines and list them in the exclusion file with the reason.
- Parallel stages must not write to shared collections in completion order. Collect, then sort ordinal-ignore-case before any output.

## Acceptance Criteria

- [ ] AC-1 The generator produces the same tree (same file-hash manifest) for the same seed, with ≥ 10,000 files and the listed composition.
- [ ] AC-2 The budget ADR records the measured baseline and final budget. Perf tests assert wall time and peak working set against it on `windows-latest` (performance project, runs nightly and on demand).
- [ ] AC-3 `analyse`, `validate`, `plan` and `apply` on the synthetic tree meet the budget in three consecutive CI runs.
- [ ] AC-4 Artefacts and outputs for degree of parallelism 1 and N are byte-identical (test).
- [ ] AC-5 `determinism.yml` runs two jobs on separate runners with different path roots, culture and time zone. Manifest comparison passes for the full matrix. The exclusion list is committed and every entry has a reason.
- [ ] AC-6 An injected nondeterminism (e.g. unsorted enumeration behind a test switch) makes the determinism comparison fail (negative test of the checker).
- [ ] AC-7 PE fuzzing (≥ 10,000 mutated inputs on the nightly seed set) produces no unhandled exceptions or timeouts. Every failure found has a committed regression input.
- [ ] AC-8 JSON and XML fuzzing covers every reader listed in Scope with the same invariant. An XXE payload in `RuntimeList.xml` is not resolved.
- [ ] AC-9 Streaming hashing is used: hashing a 1 GB synthetic file raises peak working set by < 100 MB (test).

## Test Requirements

- xUnit v3 on MTP. Perf tests live in the separate project `tests/Tailor.PerformanceTests/`, which the nightly workflow runs explicitly, so PR runs stay fast (plan risk R7); no extra `Category` value is introduced. The PR tier runs a fixed-seed fuzz smoke (≤ 60 s); the nightly run raises the iteration count via an environment variable.
- Run: `dotnet test --project tests/Tailor.PerformanceTests --filter-trait "WU=1004"`; fuzz: `dotnet test --project tests/Tailor.Inspection.Tests --filter-trait "WU=1004"` and `dotnet test --project tests/Tailor.Specifications.Tests --filter-trait "WU=1004"`.
- Synthetic trees are generated into a temp/`artifacts/` folder, never committed. Fuzz regression inputs are small and committed.
- Record Test Evidence: perf timing report, determinism manifests from both runners, fuzz run summary (seed, iterations, failures).

## Definition of Done

- All AC verified. CI green (PR tier). Nightly perf, determinism and fuzz workflows green at least once.
- The M10 "performance budget … determinism check" criterion is ticked by the Verifier.
- ADR committed, and architecture §15/§16 updated if the determinism scope changes.
- The plan status is updated. Test Evidence is recorded.

## Agent Notes

- Measure before optimising. Commit the baseline numbers in the ADR first.
- GitHub-hosted runners vary in speed. Budgets need headroom (≥ 30% over the measured median).
- Do not change canonical artefact formats to hit budgets.

## Open Questions

- Final budget numbers (the proposed table must be confirmed after the baseline).
- Should the degree-of-parallelism option be public (CLI/config) or internal only?
- Do cross-machine checks also need different runner images (e.g. `windows-2022` vs `windows-2025`), given that equivalence across tooling versions is not required?
