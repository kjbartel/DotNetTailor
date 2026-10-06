# WU-600 output-safety-and-staging

| Field | Value |
|---|---|
| ID | WU-600 |
| Title | output-safety-and-staging |
| Milestone | M6 Execution Engine (v0.2.0-preview) |
| Status | Ready |
| Depends on | WU-100 |
| Parallel with | M2–M5 |
| Target project(s)/paths | `src/Tailor.Platform.Abstractions/Paths/`, `src/Tailor.Platform.Windows/Paths/`, `src/Tailor.Execution/Safety/`, `src/Tailor.Execution/Staging/`, `tests/Tailor.Execution.Tests/{Safety,Staging}/`, `tests/Tailor.Platform.Windows.Tests/Paths/` |
| Size | M |
| Branch / PR | `wu/600-output-safety-and-staging` / `WU-600: output-safety-and-staging` |

## Goal

Guarantee safe output handling. Before any write, reject input/output (and symbols output) path combinations that are equal, nested or aliased through symlinks or junctions, and reject a non-empty output. Write only into a sibling staging directory, commit by atomic rename, and on failure or cancellation remove every trace. No partial output remains.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [RQ §9](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §4.3](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Input ≠ output, never in place |
| [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Safe failure, no partial output corruption |
| [TS §24.4](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies), [TS §32](../../Requirements/Transformation_Specification.md#32-global-invariants) item 10 | Input/output identity and unsafe path traversal are unconditional errors |
| [TS §2.4](../../Requirements/Transformation_Specification.md#2-lifecycle) | Execution produces a new directory |
| Architecture [§11](../../Architecture/Tailor.architecture.md#11-execution-and-safety), [§12](../../Architecture/Tailor.architecture.md#12-platform-abstraction), [§17](../../Architecture/Tailor.architecture.md#17-security), [§20](../../Architecture/Tailor.architecture.md#20-open-questions) (`--force`) | Canonicalisation, staging, atomic rename, long paths, `IPathCanonicaliser` |
| Plan risk R9 | Rename retry with backoff, long paths |
| Plan M6 criteria 2, 3 | AC-1–AC-10 |

## Scope

**In**
- `IPathCanonicaliser` (Platform.Abstractions) and its Windows implementation: final paths with symlinks and junctions resolved (`GetFinalPathNameByHandle`), for existing and not-yet-existing paths.
- `OutputSafetyGuard`: pre-flight checks over input, output, symbols output and the artefacts directory.
- `StagingSession`: creates `<outputParent>/<outputName>.staging-<random>` (and the symbols staging sibling when needed), commits by rename, and rolls back on failure, dispose or cancellation.
- Stale staging detection.
- Long-path (`> 260` chars) support.

**Out**
- Writing content (WU-601). Validation of the staged tree (WU-602). CLI and Ctrl+C wiring (WU-603 hooks `Console.CancelKeyPress` to the token consumed here). `--force` (open question; not implemented).

## Deliverables

| Type | API / responsibility |
|---|---|
| `Platform.Abstractions.Paths.IPathCanonicaliser` | `Result<CanonicalPath> Canonicalise(string path)`. For a non-existent path: canonicalise the nearest existing ancestor and append the remaining segments. `CanonicalPath { Value, VolumeId, IsReparsePointTraversed }` |
| `Platform.Windows.Paths.WindowsPathCanonicaliser` | `\\?\`-prefixed long-path aware; resolves junctions, symlinks and `subst` drives; case-insensitive compare |
| `Execution.Safety.OutputSafetyGuard.Check(OutputSafetyRequest)` → `Result` | Request: `InputRoot`, `OutputRoot`, `SymbolsOutput?`, `ArtifactsDirectory?`, `InputSpecPath?` |
| `Execution.Staging.StagingSession` (`IAsyncDisposable`) | `static CreateAsync(StagingRequest, CancellationToken)`, `PrimaryRoot`, `SymbolsRoot?`, `CommitAsync(CancellationToken)`, `RollbackAsync()`, `State` (`Open`, `Committed`, `RolledBack`) |
| `Execution.Staging.IFileSystemOps` | Thin, fakeable seam over `Directory.Move`, delete and create, used for fault injection |
| `ExecutionSafetyDiagnostics` | `TLR6001`–`TLR6099` (all path checks are structural) |

**Checks** (all run before any write; paths compared after canonicalisation with `PathPolicy.Windows`)

| Code | Condition |
|---|---|
| `TLR6001` | Output equals input |
| `TLR6002` | Output is nested inside input, or input inside output |
| `TLR6003` | Output exists and is not an empty directory (hidden and system files count), or output is a file |
| `TLR6004` | Symbols output equals, or is nested in/around, input or output |
| `TLR6005` | Output parent does not exist or is not writable |
| `TLR6006` | Output parent is on a different volume from the staging location (cannot happen with a sibling unless a mount point is used; defensive) |
| `TLR6007` | Canonicalisation failed (broken reparse point, access denied) |
| `TLR6008` | Stale `<output>.staging-*` sibling found (warning; never deleted automatically) |
| `TLR6009` | Commit failed after retries (execution failure → exit 5) |
| `TLR6010` | Execution cancelled; staging removed |

## Design Notes

- **Staging name.** `<outputName>.staging-<8 hex>` from a CSPRNG, created with `CreateDirectory` and failing if it exists. The random suffix never appears in canonical artefacts.
- **Commit.** If the output directory exists (it is empty by the check), delete it, then `Directory.Move(staging, output)`. Retry on `IOException`/`UnauthorizedAccessException` sharing violations (AV/indexer locks, R9) with backoff 50, 100, 200, 400, 800 ms. Symbols output is committed **first** and removed again if the primary commit fails, so that either both or neither exist.
- **Rollback.** Recursive delete with the same retry policy, and with read-only attributes cleared. `DisposeAsync` rolls back unless `Committed`. Cancellation (`OperationCanceledException`) triggers rollback before rethrow.
- **TOCTOU.** Checks are repeated immediately before commit, because output emptiness or aliasing may change during execution.
- Input is never opened for write. The guard verifies nothing is written under input by asserting that staging is not under input.
- No Windows types leak outside `Platform.Windows` (architecture §12).

## Acceptance Criteria

- [ ] AC-1 Output = input yields `TLR6001` (also with different casing, a trailing separator, or a `subst` drive alias).
- [ ] AC-2 Output under input, and input under output, each yield `TLR6002`.
- [ ] AC-3 Output reached through a **junction** or **directory symlink** that resolves into (or equal to) the input yields `TLR6001`/`TLR6002` (real reparse points created in the test; symlink tests skip with a reason when the privilege is missing, junction tests always run).
- [ ] AC-4 A non-empty output (including one containing only a hidden file) yields `TLR6003`. An empty existing directory and an absent path pass.
- [ ] AC-5 A symbols output inside the output or input yields `TLR6004`.
- [ ] AC-6 A failed check leaves the filesystem untouched: no staging directory, no output (before/after snapshot of the parent).
- [ ] AC-7 A successful `CommitAsync` leaves the output with the staged content and no `*.staging-*` sibling. The symbols output is present when requested.
- [ ] AC-8 A fault injected via `IFileSystemOps` during commit (primary move fails after the symbols move) leaves neither output nor symbols output nor staging.
- [ ] AC-9 Cancelling the token during a long write (fake writer) triggers rollback: no staging and no output remain; `TLR6010` is reported.
- [ ] AC-10 Disposing an uncommitted session removes staging, even when files inside are read-only.
- [ ] AC-11 Sharing-violation faults on the first two move attempts succeed on the third attempt. Persistent faults yield `TLR6009` after the documented retries.
- [ ] AC-12 Staging, commit and rollback work for an output path longer than 300 characters with nested files over 260 characters.
- [ ] AC-13 A pre-existing `<output>.staging-abcd1234` sibling yields `TLR6008` and is left untouched.

## Test Requirements

- Unit: `tests/Tailor.Execution.Tests/{Safety,Staging}/` with a fake canonicaliser and `IFileSystemOps` fault injection. Trait `WU=600`.
- Platform: `tests/Tailor.Platform.Windows.Tests/Paths/`, with real temp directories, junctions (`mklink /J` equivalent via API), symlinks and long paths. Traits `WU=600` (no category: runs in the default suite on the Windows host).
- Run: `dotnet test --project tests/Tailor.Execution.Tests --filter-trait "WU=600"`, `dotnet test --project tests/Tailor.Platform.Windows.Tests --filter-trait "WU=600"`.
- Record Test Evidence below and in the PR, including whether the symlink tests ran or skipped.

## Definition of Done

- Zero warnings, tests green, format clean. All ACs ticked by the Verifier. Plan status `Done`. `TLR60xx` codes listed for WU-1001.

## Agent Notes

- Execution references Platform.Abstractions only. Platform.Windows is wired by the Cli (architecture §3.1).
- Create junctions in tests with `DeviceIoControl(FSCTL_SET_REPARSE_POINT)` or `cmd /c mklink /J` started with `ArgumentList` (tests only; never a shell string).
- This WU can start right after WU-100. It has no M2–M5 dependency.

## Open Questions

- **Resolved** — cancellation exit code: 130 (output rolled back); path-safety violations `TLR6001`–`TLR6004` are structural, exit 1 (architecture §13).
- `--force` for a non-empty output (architecture §20) is not implemented. Confirm that v1 keeps "absent or empty".
- Should the artefacts directory also be forbidden inside the output (it would end up in the committed tree)? Proposed: yes, add to `TLR6004`'s family if confirmed.
- Two renames (symbols, then primary) are not atomic as a pair. The compensation above is best-effort if the process is killed between them.

## Test Evidence

_To be completed by the implementer._
