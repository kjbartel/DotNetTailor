# WU-601 action-executor-and-symbols-output

| Field | Value |
|---|---|
| ID | WU-601 |
| Title | action-executor-and-symbols-output |
| Milestone | M6 Execution Engine (v0.2.0-preview) |
| Status | Ready |
| Depends on | WU-600, WU-503 |
| Parallel with | WU-504, WU-505, WU-507 |
| Target project(s)/paths | `src/Tailor.Execution/{Executor,Actions,Symbols,Reports}/`, `tests/Tailor.Execution.Tests/{Executor,Actions,Symbols,Reports}/` |
| Size | M |
| Branch / PR | `wu/601-action-executor-and-symbols-output` / `WU-601: action-executor-and-symbols-output` |

## Goal

Execute a WU-503 plan into a WU-600 staging session. Every action kind is dispatched through a registry of executors. Files are copied as streams while being hashed and verified against the plan. Config modifications go through named hooks. The symbols output is written as a directory or as a byte-deterministic zip, and a deterministic execution report is produced. The executor never changes the plan.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §2.4](../../Requirements/Transformation_Specification.md#2-lifecycle), [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) item 12 | Execute a resolved plan only |
| [TS §16.2](../../Requirements/Transformation_Specification.md#16-debug-symbol-policy) | Separate symbols output |
| [TS §26.4](../../Requirements/Transformation_Specification.md#26-dry-run-and-planning-behaviour), [TS §31](../../Requirements/Transformation_Specification.md#31-relationship-to-transformation-plans-and-logs) | Action kinds; execution report content |
| [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Separate report artefact; deterministic, auditable, safe failure |
| [CK §8.3](../../Requirements/Read_to_run_Cake.md) | Symbols zip packaging |
| Architecture [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies) (Execution must not change the plan), [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§9](../../Architecture/Tailor.architecture.md#9-transformation-handlers) (Symbols row), [§11](../../Architecture/Tailor.architecture.md#11-execution-and-safety), [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§17](../../Architecture/Tailor.architecture.md#17-security) | Executor, zip rules, determinism, zip path safety |
| Plan M6 criteria 1, 3, 4 | AC-6–AC-10 |

## Scope

**In**
- `PlanExecutor`: executes actions in `Order`, honouring `dependsOn`. Bounded parallelism for independent actions with deterministic report merge.
- Executors for `Preserve`, `Copy`, `Move`, `Remove`, `Add` (input/user/generated and package sources via `Core.Packages.IPackageLocator`, WU-100), `Replace`, `ExtractSymbols`, `ModifyConfig` (via `IConfigModifier` hooks).
- `Optimise` extension point only: without a registered executor → error. WU-703 registers its executor.
- Symbols output writers: directory and deterministic zip.
- `execution-report.json` writer.

**Out**
- Staging, commit and rollback mechanics (WU-600). Post-execution validation (WU-602). Concrete config modifiers (WU-801/802). crossgen2 (WU-703). CLI (WU-603).

## Deliverables

Namespace `Tailor.Execution`.

| Type | API / responsibility |
|---|---|
| `Executor.PlanExecutor.ExecuteAsync(ExecutablePlan, ExecutionContext, CancellationToken)` → `ExecutionResult` | `ExecutablePlan` wraps WU-503 `PlanResult` (read-only). `ExecutionContext {InputRoot, StagingSession, IPackageLocator?, UserFileRoot?, SymbolsFormat, MaxParallelism}`. Result: `Succeeded`, `ActionResults[]`, `Diagnostics` |
| `Actions.IActionExecutor` | `ActionKind Kind`, `ValueTask<ActionResult> ExecuteAsync(PlanAction, ActionExecutionContext, CancellationToken)`. Registered through DI, keyed by kind; a duplicate kind → startup error |
| `Actions.StreamingCopier` | `CopyAsync(Stream source, string destination, ContentHash? expected, CancellationToken)` → `(ContentHash actual, long size)`. Single pass: read → write + `IncrementalHash` SHA-256. Mismatch → delete destination, error |
| `IConfigModifier` (consumed, not defined here) | Lives in `Tailor.Planning.Actions` (architecture §3.2) so Transforms can implement it without referencing Execution: `string Name`, `ValueTask<byte[]> ModifyAsync(ReadOnlyMemory<byte> original, JsonObject parameters, CancellationToken)`. Selected by `PlanAction.Parameters.modifier`. If WU-503 has not added it, add it to Planning in this WU |
| `Symbols.SymbolsDirectoryWriter` / `Symbols.DeterministicZipWriter` | Write `ExtractSymbols` outputs into `StagingSession.SymbolsRoot` (directory) or `<SymbolsRoot>.zip` (zip) |
| `Reports.ExecutionReportWriter` | `execution-report.json`: `kind: ExecutionReport`, `schemaVersion`, `planHash`, `actions[] {id, kind, status (succeeded\|failed\|skipped), destination, sha256, size, diagnostics[]}` sorted by `order`, `tools[]`, `timings {…}` (the only non-canonical section, separable) |
| `ExecutionDiagnostics` | `TLR6101`–`TLR6199` |

**Diagnostics**

| Code | Condition |
|---|---|
| `TLR6101` | Source hash differs from the plan's `ExpectedHash`/`InputFile.Hash` (input changed since planning) |
| `TLR6102` | No executor registered for the action kind (e.g. `Optimise` before WU-703) |
| `TLR6103` | No `IConfigModifier` registered for the named modifier |
| `TLR6104` | Package source not locatable, or package hash differs from provenance |
| `TLR6105` | Destination already exists in staging (plan/collision invariant violated) |
| `TLR6106` | Zip entry name invalid after `RelativePath` validation (defensive) |
| `TLR6107` | An action failed (wraps the inner diagnostic). Execution stops and the result is `Succeeded = false` |

## Design Notes

- **Order.** Actions run in `Order`. Actions without mutual `dependsOn` in the same phase may run concurrently (`MaxParallelism`, default `Environment.ProcessorCount`). The report is merged by `Order`, so its content never depends on scheduling.
- `Preserve`/`Copy`/`Move` all read from the input tree and write to staging. The input tree is never modified (`Move` means "moved in the output layout"). `Remove` is a no-op unless a previous action created the destination in staging.
- `ExtractSymbols` writes only to the symbols root, never to the primary root.
- **Deterministic zip.** Entries are sorted with `PathPolicy.Windows` comparer, use `/` separators, `LastWriteTime` = 1980-01-01T00:00:00 (DOS epoch), `CompressionLevel.Optimal`, no directory entries, no extra fields, and fixed external attributes. Entry names come from `RelativePath` (no `..`, no rooted names). Bytes are identical for equal inputs on the same runtime version.
- Fail fast. The first failed action cancels outstanding actions. The executor returns a failure and never deletes staging itself: rollback belongs to the caller (WU-600/602/603).
- Execution never reads the plan file from disk. It receives the in-memory `PlanResult` (replay is an open question in the plan).

## Acceptance Criteria

- [ ] AC-1 Each of `Preserve`, `Copy`, `Move`, `Remove`, `Add` (input/user/generated/package via fake locator), `Replace`, `ExtractSymbols`, `ModifyConfig` has a unit test asserting staged content and the report entry.
- [ ] AC-2 `StreamingCopier` computes the same SHA-256 as `ContentHasher` for 0 B, 1 B, 1 MiB and 100 MiB (sparse temp) files, and reads each source exactly once (counting stream).
- [ ] AC-3 A source file modified after planning yields `TLR6101`, the destination is deleted and execution stops.
- [ ] AC-4 An `Optimise` action without a registered executor yields `TLR6102`. A fake `IActionExecutor` for `Optimise` registered through DI is invoked (extension-point test).
- [ ] AC-5 A `ModifyConfig` action with a fake `IConfigModifier` writes the modifier's output. An unknown modifier name yields `TLR6103`.
- [ ] AC-6 Symbols `directory` output mirrors the PDBs' relative paths under the symbols root. The primary root contains no extracted PDB.
- [ ] AC-7 The symbols zip is byte-identical across two executions into fresh staging sessions (M6 criterion 4). Entry order, timestamps and attributes match the Design Notes (inspected with `ZipArchive`).
- [ ] AC-8 Running with `MaxParallelism` 1 and 8 yields byte-identical staged trees and `execution-report.json` (excluding the `timings` section).
- [ ] AC-9 A fault injected in the Nth action stops execution, reports `TLR6107`, and returns `Succeeded = false`. Staging is left for the caller to roll back (asserted via WU-600 session state).
- [ ] AC-10 Executing the WU-504 filtering, symbols and resources plans for a matrix ConsoleApp copy produces a staged tree whose file list and hashes equal `PlanResult.ProjectedState` (primary and symbols roots).
- [ ] AC-11 No executor opens any input file with write access (a fake input tree that throws on write-open runs the suite green).

## Test Requirements

- Unit: `tests/Tailor.Execution.Tests/{Executor,Actions,Symbols,Reports}/`, with WU-600 `StagingSession` over temp directories, fake locators and fake modifiers. Trait `WU=601`.
- Integration (AC-10): matrix copies with WU-504 scenario plans. Traits `Category=Integration`, `Category=Matrix`, `WU=601`.
- Run: `dotnet test --project tests/Tailor.Execution.Tests --filter-trait "WU=601"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=601"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `TLR61xx` codes listed for WU-1001.

## Agent Notes

- Execution may reference Planning, Validation and Platform.Abstractions, but not Acquisition or Transforms (architecture §3.1). `IPackageLocator` is the Core contract from WU-100 (architecture §3.2); do not define a second one.
- If WU-504 is not merged, build plans for AC-1–AC-9 with the WU-503 `PlanBuilder` directly.

## Open Questions

- Default symbols output location when neither `symbols.output.path` nor `--symbols-output` is given (see WU-504). Proposed: `<output>.symbols` / `<output>.symbols.zip`.
- Deflate output may differ across .NET runtime versions. Should the zip use `CompressionLevel.NoCompression` for cross-version byte stability? Proposed: `Optimal`, with determinism asserted only for the same runtime version.
- Should `execution-report.json` put timings in a separate file (`execution-timings.json`) so the report is fully canonical? Architecture §5 says "deterministic except timings".

## Test Evidence

_To be completed by the implementer._
