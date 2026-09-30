# WU-202 runtime-config-readers

| Field | Value |
|---|---|
| ID | WU-202 |
| Title | runtime-config-readers |
| Milestone | M2 Binary Inspection |
| Status | Not started |
| Depends on | WU-100, WU-005 |
| Parallel with | WU-200, WU-201, WU-203, M1 |
| Target project(s)/paths | `src/Tailor.Inspection/Runtime/`, `src/Tailor.Inspection/Apphost/`, `tests/Tailor.Inspection.Tests/Runtime/`, `tests/Tailor.Inspection.Tests/Apphost/`, `tests/Tailor.IntegrationTests/Inspection/` |
| Size | M |

## Goal

Read the runtime-facing configuration of an app: `*.runtimeconfig.json` (framework references vs included frameworks, roll-forward, properties), `*.deps.json` via Microsoft.Extensions.DependencyModel, and the apphost binding (bound DLL path, subsystem).

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Execution model | [AS §7.2](../../Requirements/Application_Specification.md), [AS §9.2](../../Requirements/Application_Specification.md), [AS §12.4](../../Requirements/Application_Specification.md) | [§7.4](../../Architecture/Tailor.architecture.md#74-identities-and-inspection) (Apphost binding), [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies) (Inspection owns readers) |
| FD/SC facts for later transforms | [TS §13](../../Requirements/Transformation_Specification.md), [TS §11.4](../../Requirements/Transformation_Specification.md), [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§9](../../Architecture/Tailor.architecture.md#9-transformation-handlers), [§9.2](../../Architecture/Tailor.architecture.md#92-apphost) |
| Platform isolation | [RQ §11](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§12](../../Architecture/Tailor.architecture.md#12-platform-abstraction) |
| Untrusted input | [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§17](../../Architecture/Tailor.architecture.md#17-security) |

## Scope

**In**
- `RuntimeConfigReader` → `RuntimeConfigFacts` (+ the parsed `JsonObject` retained for WU-801).
- `DepsJsonReader` → `DepsJsonFacts` wrapping `DependencyContext` (runtime target, RID, libraries, runtime/native assets per RID, resource assemblies, `runtimepack.*` libraries).
- `ApphostBindingReader` → `ApphostBinding` (bound relative DLL path, subsystem, PE machine).

**Out**
- Writing/transforming runtimeconfig or deps.json (WU-801, WU-802).
- Apphost patching, resource copying, `IApphostService` (WU-800, `Platform.Windows`).
- Deployment-model inference from combined facts (WU-400).
- Single-file bundle detection (WU-200).

## Deliverables

| Namespace | Type | API |
|---|---|---|
| `…Inspection.Runtime` | `sealed record RuntimeConfigFacts` | `Tfm?`, `Frameworks` (`FrameworkReference(Name, Version)` list; `framework` singular normalised into the list), `IncludedFrameworks` (same shape), `IsSelfContained` (`IncludedFrameworks.Count > 0`), `RollForward?` (string, known values `Minor`, `Major`, `LatestPatch`, `LatestMinor`, `LatestMajor`, `Disable`), `RollForwardOnNoCandidateFx?`, `ApplyPatches?`, `ConfigProperties` (sorted `IReadOnlyDictionary<string, JsonNode?>`), `Raw` (`JsonObject`) |
| | `RuntimeConfigReader` | `Result<RuntimeConfigFacts> Read(Stream, RelativePath)` |
| | `sealed record DepsJsonFacts` | `RuntimeTargetName`, `RuntimeTargetRid?`, `Libraries` (sorted `DepsLibrary(Name, Version, Type, Serviceable, IsRuntimePack)`), `RuntimeAssets`/`NativeAssets` per `(Library, Rid?, Path)`, `ResourceAssets` per `(Library, Culture, Path)`, `Context` (`DependencyContext`) |
| | `DepsJsonReader` | `Result<DepsJsonFacts> Read(Stream, RelativePath)` using `DependencyContextJsonReader` |
| `…Inspection.Apphost` | `sealed record ApphostBinding` | `BoundAssemblyPath` (`RelativePath`, relative to the apphost's folder), `Subsystem` (`Console`/`Gui`/`Other`), `Machine`, `BindingState` (`Bound`, `Unbound` (template placeholder present), `NotFound`) |
| | `ApphostBindingReader` | `Result<ApphostBinding> Read(Stream, RelativePath apphostLocation)` |
| | `RuntimeDiagnostics` | `RPK2200`–`RPK2299` |

Diagnostics (minimum):

| Code | Condition | Severity |
|---|---|---|
| `RPK2201` | runtimeconfig.json malformed JSON / missing `runtimeOptions` | Error |
| `RPK2202` | runtimeconfig has both `framework` and `frameworks` | Warning (merged) |
| `RPK2203` | runtimeconfig has both `frameworks` and `includedFrameworks` | Warning |
| `RPK2210` | deps.json malformed (reader exception wrapped) | Error |
| `RPK2220` | Apphost binding not found (not an apphost) | Warning |
| `RPK2221` | Apphost is an unbound template (placeholder present) | Warning |
| `RPK2222` | Bound path invalid (absolute, escaping, invalid chars) | Error (structural) |

## Design Notes

- runtimeconfig: parse with `CanonicalJson.ReaderOptions` into `JsonObject`; runtimeconfig has no public reader API. Ignore `*.runtimeconfig.dev.json` (caller responsibility; document it). Keep `Raw` so WU-801 can edit while preserving unknown members.
- deps.json: `DependencyContextJsonReader.Read(Stream)` (Microsoft.Extensions.DependencyModel via CPM). `IsRuntimePack` = library type `runtimepack` or name prefix `runtimepack.`. RID from `runtimeTarget.name` suffix after `/`.
- Apphost binding: the SDK replaces the SHA-256(`foobar`) UTF-8 placeholder with the DLL path in a fixed 1024-byte buffer ([§9.2](../../Architecture/Tailor.architecture.md#92-apphost)). After binding, the placeholder is gone, so the reader must locate the path with the algorithm confirmed by spike WU-005. Implement it behind `ApphostBindingReader` only. Validate the extracted path with `RelativePath.TryParse` (security, [§17](../../Architecture/Tailor.architecture.md#17-security)).
- Subsystem: from `PEHeaders.PEHeader.Subsystem` (`WindowsGui` → `Gui`, `WindowsCui` → `Console`).
- Location per [§3.2](../../Architecture/Tailor.architecture.md#32-cross-layer-contracts): this reader is the **single owner** of apphost binding reads (pure byte/PE parsing, no Win32 APIs). `IApphostService` (WU-800) has no read operation; `Platform.Windows` reuses this reader internally. No `Platform.*` reference from `Inspection`.
- All readers are stateless and never throw for bad input.

## Acceptance Criteria

- [ ] AC-1 FD runtimeconfig (`framework` singular) → one `Frameworks` entry, `IsSelfContained = false`, `Tfm` read.
- [ ] AC-2 FD WindowsDesktop runtimeconfig (`frameworks` with `Microsoft.NETCore.App` + `Microsoft.WindowsDesktop.App`) → two entries in file order.
- [ ] AC-3 SC runtimeconfig (`includedFrameworks`) → `IsSelfContained = true`, entries read.
- [ ] AC-4 `rollForward`, `applyPatches` and `configProperties` (bool, string, number) are read; properties sorted ordinally.
- [ ] AC-5 Comments/trailing commas tolerated; malformed JSON → `RPK2201`; both `framework` and `frameworks` → `RPK2202`.
- [ ] AC-6 deps.json fixtures (FD, SC with `runtimepack.*`, with `runtimes/win-x64/native` and `runtimes/linux-x64/lib` assets, with resource assemblies) → `RuntimeTargetName`, `RuntimeTargetRid`, libraries, per-RID assets and cultures match committed golden files; malformed → `RPK2210`.
- [ ] AC-7 Synthetic apphost-like PE with an embedded bound path `Viewer.dll` → `Bound`, path `Viewer.dll`; with the placeholder still present → `Unbound` + `RPK2221`; bound path `..\x.dll` → `RPK2222`.
- [ ] AC-8 Matrix (`Category=Matrix`): for net8 and net10 × FD and SC apphosts in the matrix, `BoundAssemblyPath` equals `<AppName>.dll` and subsystem matches the app type (M2 criterion).
- [ ] AC-9 Matrix: every `*.runtimeconfig.json` and `*.deps.json` in the matrix reads without error; `IsSelfContained` matches the matrix manifest's FD/SC flag.
- [ ] AC-10 Readers never throw on truncated/garbage input (fuzz loop over each reader, 1,000 iterations).

## Test Requirements

- xUnit v3 + golden files (`Tailor.Testing.Golden`) in `tests/Tailor.Inspection.Tests/`.
- Unit fixtures: small hand-written runtimeconfig/deps.json files under `Fixtures/Runtime/` modelled on SDK output; synthetic apphost PE via `SyntheticPe` helper. Trait `WU=202`.
- Matrix tests (AC-8, AC-9) in `tests/Tailor.IntegrationTests/Inspection/RuntimeConfigMatrixTests` over `artifacts/testapps/manifest.json`; traits `Category=Integration`, `Category=Matrix`, `WU=202`; skip with reason locally when absent, CI must not skip.
- Run: `dotnet test --project tests/Tailor.Inspection.Tests --filter-trait "WU=202"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=202"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green (unit + matrix in CI); `dotnet format --verify-no-changes` clean.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- Read the WU-005 spike report/ADR first (WU-005 is a dependency); AC-7/AC-8 depend on its findings.
- Do not copy code from Microsoft.NET.HostModel ([§9.2](../../Architecture/Tailor.architecture.md#92-apphost)).

## Open Questions

- **Resolved** — WU-005 dependency: added to the plan.
- **Resolved** — duplicate `ApphostBinding` types: this spec's `Inspection.Apphost.ApphostBinding` is the only one; `IApphostService` exposes no binding read (architecture §3.2).
- Whether `ApphostBinding` should report the .NET 9+ `DOTNET_ROOT` search options ([§9.2](../../Architecture/Tailor.architecture.md#92-apphost)); proposed: add when WU-800 needs it.

## Test Evidence

_To be completed by the implementer._
