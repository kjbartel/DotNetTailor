# WU-800 apphost-service

| Field | Value |
|---|---|
| ID | WU-800 |
| Title | apphost-service |
| Milestone | M8 Deployment Model Conversion (v0.4.0-preview) |
| Status | Not started |
| Depends on | WU-005, WU-700 |
| Parallel with | M3–M7 |
| Target | `src/Tailor.Platform.Abstractions/` (`IApphostService`), `src/Tailor.Platform.Windows/Apphost/`, `src/Tailor.Transforms/DeploymentModel/ApphostTemplateResolver.cs`, `tests/Tailor.Platform.Windows.Tests/` |
| Size | L |

## Goal

Create and rebind Windows apphosts without the SDK: patch the app-DLL placeholder, set the subsystem, copy Win32 resources from the original host, refuse single-file bundles and warn about invalidated Authenticode signatures. Windows-only implementation behind `IPlatformServices`.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §13](../../Requirements/Transformation_Specification.md#13-deployment-model-transformation) | Platform assets for deployment conversion |
| [TS §19](../../Requirements/Transformation_Specification.md#19-addition-rules) | Host pack as addition source; provenance |
| [CK §5](../../Requirements/Read_to_run_Cake.md#5-deployment-model-transformations) | FD⇄SC combinations |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Deployment model changes |
| [Architecture §9.2](../../Architecture/Tailor.architecture.md#92-apphost), [§12](../../Architecture/Tailor.architecture.md#12-platform-abstraction), [§1.2](../../Architecture/Tailor.architecture.md#12-non-goals-v1), [§19 item 12](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) | Custom patcher, bundle refusal, no re-signing |

## Scope

**In**
- `IApphostService` operations: `Create` (from template), `Rebind` (existing host → new DLL path), `IsBundle`, `GetSubsystem`, `HasAuthenticodeSignature`. No binding-read operation: consumers use the WU-202 `ApphostBindingReader` (single owner); `WindowsApphostService` uses it internally to verify its output.
- Placeholder patching: ASCII hex of SHA-256("foobar") replaced by the UTF-8 relative DLL path, zero-padded; max length per WU-005.
- .NET 9+ second placeholder (DOTNET_ROOT / app-relative search options): detected and left at template default unless explicitly set.
- Subsystem: `WINDOWS_GUI` for GUI entry points (`WinExe`), `WINDOWS_CUI` otherwise; source = original host, else AppSpec entry-point `subsystem`.
- Resource copy from the original apphost: `RT_ICON`, `RT_GROUP_ICON`, `RT_VERSION`, `RT_MANIFEST`.
- Bundle detection → refusal diagnostic.
- Authenticode: signed original → warning; output carries no stale certificate table.
- `ApphostTemplateResolver` (Transforms): acquires `Microsoft.NETCore.App.Host.<rid>` via WU-700 and returns `runtimes/<rid>/native/apphost.exe` + provenance.

**Out**
- Re-signing. Non-Windows hosts. Deciding when to create/rebind (WU-803/804). Single-file bundle extraction.

## Deliverables

- Contracts in `Platform.Abstractions`: `IApphostService`, `ApphostCreateOptions {AppDllRelativePath, Subsystem, ResourceSource?}`, `PeSubsystem`. No `ApphostBinding` type here (Platform.Abstractions references Core only; the binding type lives in Inspection, WU-202).
- `WindowsApphostService` (`[SupportedOSPlatform("windows")]`), `ApphostPlaceholderPatcher`, `PeSubsystemWriter`, `Win32ResourceCopier` (`LibraryImport` of `BeginUpdateResource`/`UpdateResource`/`EndUpdateResource` or managed writer per spike).
- `ApphostTemplateResolver` in Transforms.
- Diagnostics (proposed `RPK80xx`): placeholder not found, path too long, bundle refused, signature invalidated (warning), resource copy failed, template not found in host pack.

## Design Notes

- Follow the WU-005 spike report (`Docs/Spikes/WU-005-*`) and ADR; **they override this spec where they differ** (placeholder layout per major, second-placeholder handling, resource API, checksum/security-directory handling).
- Platform.Windows must not reference Acquisition (architecture §3.1): the service takes file paths; template acquisition lives in Transforms.
- The same `apphost.exe` serves FD and SC; SC is decided by `hostfxr.dll` beside it and `includedFrameworks` (architecture §9.2).
- Write to a temp file in staging then move; retry `EndUpdateResource`/rename on sharing violations (AV locks, risk R9).
- Output must be deterministic for identical inputs (no timestamps written; recompute PE checksum deterministically or leave per spike).
- Binding reads go through the WU-202 `ApphostBindingReader` (architecture §3.2); this WU only creates and patches. Do not implement a second reader.
- Microsoft.NET.HostModel is not used (not a supported public package).

## Acceptance Criteria

- [ ] AC-1 `Create` from the net8 and net10 host-pack templates yields a host whose binding (read by the WU-202 `ApphostBindingReader`) equals the requested relative DLL path.
- [ ] AC-2 For GUI entry points the output subsystem is `WINDOWS_GUI`; for console it is `WINDOWS_CUI`.
- [ ] AC-3 Icon, group icon, version and manifest resources in the output are byte-equal to those of the original WinForms/WPF matrix apphost.
- [ ] AC-4 `Rebind` of a matrix apphost to a different DLL name changes only the placeholder region (byte diff limited to that region, plus checksum if the spike requires it).
- [ ] AC-5 A non-apphost executable yields "placeholder not found"; a path longer than the limit yields "path too long"; both without writing output.
- [ ] AC-6 A single-file bundle fixture is refused with an `RPK80xx` error.
- [ ] AC-7 A fixture with a populated security directory (signed original) produces the Authenticode warning; the output has no certificate table.
- [ ] AC-8 Two `Create` calls with identical inputs produce byte-identical outputs.
- [ ] AC-9 A net10 host created from the template for a matrix FD console app launches it (smoke, harness only).
- [ ] AC-10 `ApphostTemplateResolver` returns the template path and provenance `{id, version, source, sha512}`; offline cache miss is an acquisition error.

## Test Requirements

- xUnit v3 + golden files in `tests/Tailor.Platform.Windows.Tests/` (Windows-only; tests skip with explicit reason on non-Windows) and `tests/Tailor.Transforms.Tests/` for the resolver.
- Signed fixture: synthetic security-directory blob is sufficient; real signing optional.
- Host packs via `LocalPackageFeedFixture` (offline); `Category=Network` only for extra versions. Matrix hosts under `Category=Matrix`; launch under `Category=Launch`.
- Run: `dotnet test --project tests/Tailor.Platform.Windows.Tests --filter-trait "WU=800"`; `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=800"`.
- Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green.
- `RPK80xx` codes listed for WU-1001; architecture §9.2/§19 item 12 updated with the WU-005 outcome if changed.

## Agent Notes

- No Windows types in `Platform.Abstractions` signatures.
- Bound all PE reads (untrusted input); malformed PE → diagnostic, never exception.

## Open Questions

- When an FD input has no apphost (`UseAppHost=false`) and SC needs one, should resources be copied from the managed assembly's Win32 resources (SDK behaviour)?
- Should the .NET 9+ search-location placeholder ever be set by the tool (e.g. app-relative runtime for FD)?
- Authenticode re-signing remains open (architecture §20, awaiting user; provisional default: warn only in v1).
