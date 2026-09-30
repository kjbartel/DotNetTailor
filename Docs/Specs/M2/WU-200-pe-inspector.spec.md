# WU-200 pe-inspector

| Field | Value |
|---|---|
| ID | WU-200 |
| Title | pe-inspector |
| Milestone | M2 Binary Inspection |
| Status | Not started |
| Depends on | WU-100 |
| Parallel with | WU-201–WU-203, M1 |
| Target project(s)/paths | `src/Tailor.Inspection/Pe/`, `tests/Tailor.Inspection.Tests/Pe/` (+ `Fixtures/Pe/`), `tests/Tailor.IntegrationTests/Inspection/` |
| Size | M |

## Goal

Classify any file as non-PE, native PE, managed IL-only, or mixed-mode, and report machine, CorFlags, ReadyToRun/composite state, reference-assembly marker and single-file bundle marker — robustly for malformed or hostile input.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Binary inspection for classification | [AS §11.3](../../Requirements/Application_Specification.md), [AS §13.2](../../Requirements/Application_Specification.md), [AS §14](../../Requirements/Application_Specification.md) | [§7.2](../../Architecture/Tailor.architecture.md#72-classification-as-11), [§7.4](../../Architecture/Tailor.architecture.md#74-identities-and-inspection) |
| R2R eligibility inputs | [TS §14.4](../../Requirements/Transformation_Specification.md), [RD §8.2](../../Requirements/R2R_tool_Design.md) | [§9.1](../../Architecture/Tailor.architecture.md#91-readytorun-details) |
| Single-file refusal | — | [§1.2](../../Architecture/Tailor.architecture.md#12-non-goals-v1), [§7.4](../../Architecture/Tailor.architecture.md#74-identities-and-inspection) |
| Untrusted binaries | [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§17](../../Architecture/Tailor.architecture.md#17-security) |

## Scope

**In**
- `PeInspector` producing `PeFacts` from a stream or file.
- Managed / native / mixed detection; `Machine`; `CorFlags` (`ILOnly`, `Requires32Bit`, `Prefers32Bit`, `StrongNameSigned`); PE subsystem.
- ReadyToRun detection (managed native header with `RTR` signature), R2R component-of-composite flag, composite image detection (native PE exporting `RTR_HEADER`).
- `ReferenceAssemblyAttribute` presence (assembly-level custom attribute type name match only).
- Single-file bundle marker detection → `RPK2xxx` refuse diagnostic.

**Out**
- Assembly identity, references, TFM, satellite (WU-201).
- Apphost DLL binding (WU-202).
- Classification decisions (WU-301); R2R eligibility (WU-702).

## Deliverables

Namespace `Tailor.Inspection.Pe`.

| Type | API |
|---|---|
| `interface IPeInspector` / `sealed class PeInspector` | `Result<PeFacts> Inspect(Stream stream, RelativePath location)`; `Result<PeFacts> InspectFile(string fullPath, RelativePath location)`; stateless, thread-safe |
| `sealed record PeFacts` | `PeKind Kind` (`NotPe`, `Native`, `ManagedIlOnly`, `Mixed`), `Machine` (`System.Reflection.PortableExecutable.Machine`), `bool Is64Bit` (PE32+), `Subsystem`, `CorFlags?`, `ReadyToRunInfo? ReadyToRun`, `bool IsCompositeImage`, `bool IsReferenceAssembly`, `bool IsSingleFileBundle`, `bool HasMetadata` |
| `sealed record ReadyToRunInfo` | `MajorVersion`, `MinorVersion`, `Flags` (raw `uint`), `bool IsComponentOfComposite`, `bool IsPartial` |
| `static PeDiagnostics` | `RPK2001`–`RPK2099` |

Diagnostics (minimum):

| Code | Condition | Severity |
|---|---|---|
| `RPK2001` | Malformed/truncated PE headers (`BadImageFormatException`, out-of-range RVA) | Warning; `Kind = NotPe` or best-effort facts |
| `RPK2002` | Malformed CLI metadata in an otherwise valid PE | Warning |
| `RPK2003` | Malformed ReadyToRun header | Warning |
| `RPK2010` | Single-file bundle detected — unsupported input, refuse | Error (structural) |

## Design Notes

- Use only `System.Reflection.PortableExecutable.PEReader` / `System.Reflection.Metadata` ([§17](../../Architecture/Tailor.architecture.md#17-security)); open streams with `PEStreamOptions.Default` (no `PrefetchEntireImage` for large files); wrap every read in bounds-checked accessors; catch `BadImageFormatException`, `InvalidOperationException`, `ArgumentOutOfRangeException` → diagnostic. Never throw to callers for bad input; I/O errors (file not found, access denied) are returned as `RPK2004`.
- Fact rules per [§7.4](../../Architecture/Tailor.architecture.md#74-identities-and-inspection): managed = `CorHeader != null`; mixed = managed and no `ILOnly`; R2R = `CorHeader.ManagedNativeHeaderDirectory` non-empty and signature `0x00525452`; composite component via the R2R `Component` flag; composite image = native PE with export `RTR_HEADER`. Flag values come from the runtime's `readytorun.h`; cite the source in a code comment.
- Reference assembly: scan assembly-level custom attributes for `System.Runtime.CompilerServices.ReferenceAssemblyAttribute` by type name (no blob decoding). This deliberately lives here (not WU-201) so WU-702 can depend on WU-200 alone ([plan](../../Plans/Tailor.plan.md)).
- Bundle detection: apply the bundle-marker algorithm confirmed by spike WU-005 (bundle signature + non-zero header offset in an apphost). Detection must scan with a bounded buffer, not load the whole file into memory more than once.
- Non-PE files (e.g. `.json`) return `Kind = NotPe` without a diagnostic when the `MZ` signature is absent.

## Acceptance Criteria

- [ ] AC-1 Synthetic IL-only assembly (built in-memory with `ManagedPEBuilder`) → `ManagedIlOnly`, `Machine = I386`/`Amd64` as built, `CorFlags` contains `ILOnly`.
- [ ] AC-2 Synthetic assembly with `ILOnly` cleared → `Mixed`.
- [ ] AC-3 Synthetic PE without a CLI header (`PEBuilder` subclass) → `Native`; `Machine = Amd64`.
- [ ] AC-4 A non-PE byte array and an empty stream → `NotPe`, no exception.
- [ ] AC-5 Malformed fixtures — truncated after DOS header, truncated after COFF header, bad `e_lfanew`, CLI header RVA out of range, R2R header pointer out of range, metadata stream truncated — each produce a `RPK200x` diagnostic and no exception (theory test; fuzz loop over 1,000 random truncations/bit-flips of a valid assembly also throws nothing).
- [ ] AC-6 Synthetic assembly with `[assembly: ReferenceAssembly]` → `IsReferenceAssembly = true`; without it → `false`.
- [ ] AC-7 Synthetic apphost-like PE containing the bundle marker with non-zero offset → `IsSingleFileBundle = true` and `RPK2010` error; marker with zero offset → `false`.
- [ ] AC-8 Matrix (`Category=Matrix`): for every PE file listed in `artifacts/testapps/manifest.json`, `PeFacts` (excluding R2R minor version) matches a committed golden file; R2R-on builds report `ReadyToRun != null` for app assemblies and R2R-off builds report `null`; SC builds report framework assemblies as R2R.
- [ ] AC-9 Matrix: the native `e_sqlite3.dll` shipped with `ConsoleApp` variants is `Native`; apphosts are `Native` with subsystem `WindowsGui` for WinForms/WPF and `WindowsCui` for console.
- [ ] AC-10 `PeInspector` is safe for concurrent use (parallel inspection of the same set yields identical results).
- [ ] AC-11 A committed real single-file bundle fixture (`Fixtures/Pe/singlefile-fd-net8.exe`, framework-dependent, < 1 MB, with a `README.md` giving the exact regeneration command) is reported with `IsSingleFileBundle = true` and `RPK2010`; the ordinary matrix apphosts report `false` (M2 criterion).

## Test Requirements

- xUnit v3 + golden files (`Tailor.Testing.Golden`) in `tests/Tailor.Inspection.Tests/`.
- Unit tests in `tests/Tailor.Inspection.Tests/Pe/` use synthetic in-memory PE images built with `System.Reflection.Metadata.Ecma335.MetadataBuilder` + `ManagedPEBuilder`/`PEBuilder`; add a small `SyntheticPe` builder in `tests/Tailor.Inspection.Tests/Infrastructure/` (shared with WU-201/WU-202). Trait `WU=200`.
- Matrix tests (AC-8, AC-9) in `tests/Tailor.IntegrationTests/Inspection/PeInspectionMatrixTests` read `artifacts/testapps/manifest.json` (WU-003); traits `Category=Integration`, `Category=Matrix`, `WU=200`. Skip with an explicit reason locally when the matrix is absent; CI must not skip.
- Malformed-PE and single-file fixtures are owned by this WU ([WU-003](../M0/WU-003-test-app-suite.spec.md) Out of scope).
- Run: `dotnet test --project tests/Tailor.Inspection.Tests --filter-trait "WU=200"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=200"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green (unit + matrix in CI); `dotnet format --verify-no-changes` clean.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- Read the WU-005 spike report/ADR for the bundle marker and any per-major differences before implementing AC-7.
- Golden files hold only stable facts; SDK patch updates may change R2R minor versions.
- `Inspection` references only `Core` ([§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies)).

## Open Questions

- M2 criterion "mixed classified correctly" needs a real mixed-mode binary; the C++/CLI fixture is optional (`-IncludeMixedMode` in WU-003, [§16](../../Architecture/Tailor.architecture.md#16-testing-strategy)). Without it only the synthetic AC-2 covers mixed.
- Committing a binary single-file fixture (AC-11) vs generating it in CI: this spec proposes committing it for determinism and speed.
- Severity of malformed-PE diagnostics (warning vs error) at model level is decided by WU-301; here they are warnings.

## Test Evidence

_To be completed by the implementer._
