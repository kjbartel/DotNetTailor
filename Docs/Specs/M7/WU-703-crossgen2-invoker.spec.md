# WU-703 crossgen2-invoker

| Field | Value |
|---|---|
| ID | WU-703 |
| Title | crossgen2-invoker |
| Milestone | M7 Acquisition & ReadyToRun (v0.3.0-preview) |
| Status | Ready |
| Depends on | WU-702, WU-004 |
| Parallel with | M6, M8 |
| Target | `src/Tailor.Execution/ReadyToRun/`, `tests/Tailor.Execution.Tests/` |
| Size | L |

## Goal

Execute `Optimise` actions: locate crossgen2 in the pinned package, verify it, generate deterministic `.rsp` files, run crossgen2 safely, apply the failure policy and verify the output is an R2R image.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §14.5](../../Requirements/Transformation_Specification.md#14-optimisation-specification) | Fail / warn-preserve / skip |
| [TS §24](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies), [TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials) | Policies; tool identity in reports |
| [RD §8](../../Requirements/R2R_tool_Design.md) | Compilation units executed as planned |
| [CK §6.2](../../Requirements/Read_to_run_Cake.md#6-target-framework-and-runtime-requirements) | crossgen2, `win-x64` |
| [RQ §5.4](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Semantics unchanged |
| [Architecture §9.1](../../Architecture/Tailor.architecture.md#91-readytorun-details), [§11](../../Architecture/Tailor.architecture.md#11-execution-and-safety), [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§17](../../Architecture/Tailor.architecture.md#17-security) | Invocation rules, staging, determinism, process security |

## Scope

**In**
- `Optimise` action executor registered with the WU-601 executor.
- Crossgen2 locator in the package `tools/` folder: detect `crossgen2.exe` (single-file, .NET 8+) vs `crossgen2.dll`; never assume.
- Pre-flight checks: package hash matches plan provenance; crossgen2 major equals unit target runtime major.
- `.rsp` generation: inputs, `-o`, `-r` per reference, `--targetos:windows`, `--targetarch:x64`, `-O`, optional `--pdb`, `-m:StandardOptimizationData.mibc` + `--embed-pgo-data` when enabled, `--composite`/`--inputbubble` (SC only), `--parallelism`, other flags per WU-004 baseline.
- Process execution via `ProcessStartInfo.ArgumentList` (`@<rsp>` only), `UseShellExecute = false`, captured stdout/stderr, timeout, cancellation (kill process tree).
- Failure policy application; post-condition: output has R2R header (WU-200 inspector).
- Bounded parallel execution of independent units with deterministic result merge.
- Execution report entries: unit id, crossgen2 `{id, version, sha512}`, exit code, diagnostics, duration (non-canonical).

**Out**
- Eligibility and unit construction (WU-702). E2E over the matrix (WU-704). Composite framework recompilation beyond what WU-702 plans.

## Deliverables

- `Crossgen2Locator`, `Crossgen2ResponseFileWriter`, `Crossgen2Invoker`, `OptimiseActionExecutor`.
- `IProcessRunner` abstraction (testable; default implementation uses `ArgumentList`).
- Consumes `Core.Packages.IPackageLocator` (WU-100 contract; implemented by Acquisition in WU-700, wired by the Cli in WU-704).
- Diagnostics (proposed `TLR73xx`): crossgen2 not found, ambiguous layout, version mismatch, package hash mismatch, non-zero exit, timeout, output not R2R.

## Design Notes

- Follow the WU-004 spike report (`Docs/Spikes/WU-004-*`) and ADR; they **override this spec where they differ** (exact argument baseline from SDK publishes, mibc location, `.dll` launch strategy, environment variables, determinism result).
- `.rsp` content: one argument per line, references sorted ordinal-ignore-case, paths quoted; identical units produce identical `.rsp` modulo the staging root. `.rsp` files are written under `<artifacts>/r2r/` (non-canonical, diagnostic aid).
- Input is the staged IL file; output is written to a temp path in staging, verified, then moved to the destination. On `warning`/`skip` policy the IL file is kept at the destination.
- `fail` policy → executor failure → rollback via WU-601/600, exit code 5.
- If only `crossgen2.dll` exists and the spike does not define a launch strategy, fail with `TLR73xx` (no implicit `dotnet` from PATH).
- Determinism: `--parallelism` must not change output bytes (verify); if WU-004 found non-determinism, follow the ADR fallback (semantic comparison).
- No shell, no string-concatenated command lines, no credentials in the environment passed to crossgen2.

## Acceptance Criteria

- [ ] AC-1 Locator finds `crossgen2.exe` in a synthetic .NET 8+ layout and `crossgen2.dll` in a dll-only layout; both-or-neither layouts yield the documented result or an `TLR73xx` diagnostic.
- [ ] AC-2 A crossgen2 package whose major differs from the unit's target runtime major is rejected before any process starts.
- [ ] AC-3 A package whose `.nupkg` SHA-512 differs from the plan provenance is rejected before any process starts.
- [ ] AC-4 `.rsp` files for an FD unit and an SC composite unit match committed golden files (staging root normalised).
- [ ] AC-5 The process is started with `UseShellExecute = false`, the crossgen2 path as `FileName` and `ArgumentList == ["@<rsp>"]` (fake `IProcessRunner`).
- [ ] AC-6 Non-zero exit under `error` fails the action; under `warning` the IL file is kept and a warning is emitted; under `skip` the IL file is kept with an info diagnostic.
- [ ] AC-7 Timeout and cancellation terminate the crossgen2 process tree and leave no temp output.
- [ ] AC-8 Real crossgen2 (net8 and net10 packages from the local feed) compiles a matrix IL assembly; WU-200 reports the output as R2R.
- [ ] AC-9 Compiling the same unit twice (fresh staging) produces byte-identical output (or the WU-004 ADR fallback comparison passes).
- [ ] AC-10 The execution report lists crossgen2 id, version and sha512 for each unit.

## Test Requirements

- xUnit v3 + golden files in `tests/Tailor.Execution.Tests/`.
- Unit tests use a fake `IProcessRunner` and synthetic package layouts.
- Real crossgen2 tests (`Category=Matrix`) use `LocalPackageFeedFixture` seeded from the global packages folder populated by `Build-TestApps.ps1` R2R publishes; no network. `Category=Network` only for fetching packages not in the seed.
- Run: `dotnet test --project tests/Tailor.Execution.Tests --filter-trait "WU=703"`.
- Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green.
- `TLR73xx` codes listed for WU-1001; architecture §9.1 updated if the spike changed the argument set.

## Agent Notes

- Do not reference `Tailor.Acquisition` from `Tailor.Execution` (architecture §3.1); use the `IPackageLocator` seam.
- Keep argument construction in one place for WU-900 R2R invalidation reuse.

## Open Questions

- **Resolved** — `IPackageLocator` seam: defined in Core (WU-100), implemented in Acquisition (WU-700), wired by the Cli (architecture §3.2).
- Should `.rsp` files be kept in `--artefacts` after success, or only on failure?
