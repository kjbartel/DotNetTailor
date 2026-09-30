# WU-201 managed-metadata-reader

| Field | Value |
|---|---|
| ID | WU-201 |
| Title | managed-metadata-reader |
| Milestone | M2 Binary Inspection |
| Status | Not started |
| Depends on | WU-100 |
| Parallel with | WU-200, WU-202, WU-203, M1 |
| Target project(s)/paths | `src/DotNetRepack.Inspection/Metadata/`, `tests/DotNetRepack.Inspection.Tests/Metadata/`, `tests/DotNetRepack.IntegrationTests/Inspection/` |
| Size | M |

## Goal

Read managed assembly facts from ECMA-335 metadata: identity (name, version, culture, public key token), assembly references, `TargetFrameworkAttribute`, satellite detection, and a TypeRef/MemberRef scan API for later compatibility analysis.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Assembly model | [AS §13.2](../../Requirements/Application_Specification.md), [AS §13.3](../../Requirements/Application_Specification.md) | [§7.4](../../Architecture/DotNetRepack.architecture.md#74-identities-and-inspection) |
| Identity | [RD §5.2](../../Requirements/R2R_tool_Design.md) | [§7.5](../../Architecture/DotNetRepack.architecture.md#75-reference-resolution-as-15-rd-5) |
| Dependency discovery | [AS §15.3](../../Requirements/Application_Specification.md) | [§7.5](../../Architecture/DotNetRepack.architecture.md#75-reference-resolution-as-15-rd-5) |
| Satellites | [AS §17.2](../../Requirements/Application_Specification.md) | [§7.4](../../Architecture/DotNetRepack.architecture.md#74-identities-and-inspection) |
| Compat scan (consumer WU-901) | [TS §11.5](../../Requirements/Transformation_Specification.md), [RQ §5.1](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§9](../../Architecture/DotNetRepack.architecture.md#9-transformation-handlers) (Retarget row) |
| Untrusted input | [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§17](../../Architecture/DotNetRepack.architecture.md#17-security) |

## Scope

**In**
- `AssemblyIdentity` value type with deterministic equality/ordering and display form.
- `AssemblyMetadataReader` → `AssemblyFacts`: identity, references, TFM, satellite flag, module MVID.
- Custom attribute blob decoding for `TargetFrameworkAttribute` (ctor string + `FrameworkDisplayName` named arg).
- `ExternalReferenceScanner`: enumerate TypeRefs and MemberRefs grouped by resolution-scope assembly.

**Out**
- PE classification, R2R, reference-assembly marker (WU-200).
- Reference resolution against folders (WU-303); compatibility rules (WU-901).

## Deliverables

Namespace `DotNetRepack.Inspection.Metadata`.

| Type | API |
|---|---|
| `sealed record AssemblyIdentity` | `Name`, `Version`, `Culture` (`""` = neutral), `PublicKeyToken` (8 bytes or empty, hex lowercase in `ToString`), `IsRetargetable`, `ContentType`; `ToDisplayName()` (`Name, Version=…, Culture=neutral, PublicKeyToken=…`); equality: name ordinal-ignore-case, culture ordinal-ignore-case, version, token bytes; `IComparable` total order |
| `sealed record AssemblyFacts` | `Identity`, `References` (`IReadOnlyList<AssemblyIdentity>`, sorted), `TargetFramework?` (`TargetFrameworkInfo(string FrameworkName, string? DisplayName)` + `Tfm?` short form, e.g. `net8.0`, via mapping `.NETCoreApp,Version=v8.0` → `net8.0`), `IsSatellite`, `Mvid` |
| `interface IAssemblyMetadataReader` / `AssemblyMetadataReader` | `Result<AssemblyFacts> Read(Stream, RelativePath location)`; `ReadFile(string fullPath, RelativePath location)`; returns `RPK2101` for non-assemblies (no manifest) |
| `static PublicKeyTokens` | `Compute(ReadOnlySpan<byte> publicKey)` (SHA-1, last 8 bytes reversed) |
| `ExternalReferenceScanner` | `Result<ExternalReferences> Scan(Stream, RelativePath)` → per referenced assembly: sorted `TypeReference(Namespace, Name, Enclosing?)` and `MemberReference(TypeReference Parent, string Name, MemberKind Kind, string SignatureDisplay)` |
| `static MetadataDiagnostics` | `RPK2100`–`RPK2199` |

Diagnostics (minimum): `RPK2101` PE has no assembly manifest (netmodule/native); `RPK2102` malformed metadata table/heap; `RPK2103` undecodable `TargetFrameworkAttribute` blob (warning, TFM unknown); `RPK2104` unsupported/unknown TFM identifier (warning, keep raw `FrameworkName`).

## Design Notes

- Use `PEReader.GetMetadataReader()`; decode attributes with `CustomAttribute.DecodeValue` and a minimal `ICustomAttributeTypeProvider<T>` (strings/primitives only).
- Public key token: if `AssemblyDefinition.PublicKey` is a full key, compute token; `AssemblyReference.PublicKeyOrToken` may already be a token (`AssemblyFlags.PublicKey` distinguishes).
- Satellite rule ([§7.4](../../Architecture/DotNetRepack.architecture.md#74-identities-and-inspection)): non-empty culture **and** name ends with `.resources` (ordinal-ignore-case). Culture validity is not checked here (WU-203 does that at model level).
- TFM short-form mapping: `.NETCoreApp,Version=vX.Y` → `netX.Y`; `.NETStandard,Version=vX.Y` → `netstandardX.Y`; `.NETFramework,Version=vX.Y[.Z]` → `netXY[Z]`; platform suffix is not derivable from the attribute (`TargetPlatformAttribute` optional read → `-windows`). Unknown identifiers keep the raw value (`RPK2104`).
- Reference lists and scans are sorted deterministically (identity order; then namespace, name, member name, signature).
- Signature display for MemberRefs uses a `ISignatureTypeProvider<string, …>` producing a stable, culture-invariant string.
- Exceptions from malformed metadata are caught and converted to `RPK2102`; never thrown ([§17](../../Architecture/DotNetRepack.architecture.md#17-security)).

## Acceptance Criteria

- [ ] AC-1 Synthetic assembly `Contoso.Lib, Version=1.2.3.4, Culture=neutral` with a known public key → identity fields and computed token match expected values; display name matches exactly.
- [ ] AC-2 Synthetic assembly referencing `System.Runtime 8.0.0.0` (token `b03f5f7f11d50a3a`) and `Contoso.Other` → both references with correct version, culture, token; order deterministic.
- [ ] AC-3 `[assembly: TargetFramework(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]` → `FrameworkName`, `DisplayName`, `Tfm = net8.0`; with `TargetPlatform("Windows7.0")` → `net8.0-windows`; `.NETStandard,Version=v2.0` → `netstandard2.0`; `.NETFramework,Version=v4.7.2` → `net472`.
- [ ] AC-4 Malformed TFM attribute blob → `RPK2103` warning, `TargetFramework = null`, identity still returned.
- [ ] AC-5 Synthetic `Contoso.Lib.resources` with culture `de` → `IsSatellite = true`; neutral `Contoso.Lib.resources` → `false`; `Contoso.Lib` with culture `de` → `false`.
- [ ] AC-6 Native PE / non-PE input → `RPK2101`/`RPK2102`, no exception; truncation/bit-flip fuzz loop (1,000 iterations) over a valid assembly throws nothing.
- [ ] AC-7 `ExternalReferenceScanner` on a synthetic assembly calling `System.Console.WriteLine(string)` and referencing type `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` lists both under their resolution-scope assembly with stable signature strings (golden file).
- [ ] AC-8 `AssemblyIdentity` equality treats `contoso.lib`/`Contoso.Lib` as equal and differs on token/version/culture; ordering is total and stable (property test).
- [ ] AC-9 Matrix (`Category=Matrix`): identity, references and TFM of every managed file in every matrix app match a committed golden file; satellite assemblies in the satellite test app are flagged; net8 and net10 app assemblies report `net8.0*`/`net10.0*` TFMs.

## Test Requirements

- xUnit v3 + golden files (`DotNetRepack.Testing.Golden`) in `tests/DotNetRepack.Inspection.Tests/`.
- Unit tests use synthetic assemblies built with `MetadataBuilder`/`ManagedPEBuilder` via the shared `SyntheticPe` helper (coordinate with WU-200; whichever lands first creates it). Trait `WU=201`.
- Matrix test (AC-9) in `tests/DotNetRepack.IntegrationTests/Inspection/MetadataMatrixTests` over `artifacts/testapps/manifest.json`; traits `Category=Integration`, `Category=Matrix`, `WU=201`; skip with reason locally when absent, CI must not skip.
- Run: `dotnet test --project tests/DotNetRepack.Inspection.Tests --filter-trait "WU=201"`; `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=201"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green (unit + matrix in CI); `dotnet format --verify-no-changes` clean.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- Do not load assemblies with `Assembly.Load*` or `MetadataLoadContext`; `System.Reflection.Metadata` only.
- Keep `ExternalReferenceScanner` allocation-light; WU-901 will run it over whole apps.
- Golden-file scrubbing: MVIDs differ between builds — exclude `Mvid` from matrix golden files.

## Open Questions

- Whether `AssemblyIdentity` belongs in `Inspection` (this spec) or `Core` for use by Planning/Acquisition without an Inspection reference. The dependency graph lets Model/Acquisition see Inspection, so `Inspection` is sufficient for now.

## Test Evidence

_To be completed by the implementer._
