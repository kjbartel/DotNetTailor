# WU-701 runtime-pack-catalogue

| Field | Value |
|---|---|
| ID | WU-701 |
| Title | runtime-pack-catalogue |
| Milestone | M7 Acquisition & ReadyToRun (v0.3.0-preview) |
| Status | Not started |
| Depends on | WU-700, WU-006 |
| Parallel with | M3–M6 |
| Target | `src/DotNetRepack.Acquisition/` (`RuntimePacks/`), `tests/DotNetRepack.Acquisition.Tests/` |
| Size | M |

## Goal

Parse `data/RuntimeList.xml` of runtime packs into a deterministic catalogue: per framework, version and RID, the authoritative file set with type, versions, culture and profile, its source path in the pack and its app-relative destination. Provide profile/culture subsetting, ownership lookup, reference-assembly sets for crossgen2 and version diffs.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §13.4](../../Requirements/Transformation_Specification.md#13-deployment-model-transformation) | Add/remove runtime components |
| [TS §12.2](../../Requirements/Transformation_Specification.md#12-patching-specification) | Runtime patch diff (consumed by WU-900) |
| [TS §19.2](../../Requirements/Transformation_Specification.md#19-addition-rules) | Runtime packs as addition sources |
| [TS §20.2](../../Requirements/Transformation_Specification.md#20-removal-rules) | Removal of replaced framework components |
| [CK §5](../../Requirements/Read_to_run_Cake.md#5-deployment-model-transformations) | FD⇄SC needs exact framework file sets |
| [Architecture §10](../../Architecture/DotNetRepack.architecture.md#10-acquisition), [§7.5](../../Architecture/DotNetRepack.architecture.md), [§9](../../Architecture/DotNetRepack.architecture.md#9-transformation-handlers), [§19 item 10](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) | Catalogue is authoritative for framework ownership |

## Scope

**In**
- Packs: `Microsoft.NETCore.App.Runtime.<rid>`, `Microsoft.WindowsDesktop.App.Runtime.<rid>`, `Microsoft.AspNetCore.App.Runtime.<rid>`, acquired via WU-700.
- `RuntimeList.xml` parsing: `File` elements with `Type` (`Managed`, `Native`, `Resources`), `Path`, `AssemblyName`, `AssemblyVersion`, `FileVersion`, `PublicKeyToken`, `Culture`, `Profile`, and unknown attributes preserved/ignored safely.
- Layout mapping: `runtimes/<rid>/lib/netN.0/*` and `runtimes/<rid>/native/*` (incl. `System.Private.CoreLib.dll`, `hostfxr.dll`, `hostpolicy.dll`, `coreclr.dll`) → app-root destination; `Resources` with culture → `<culture>/<file>`.
- Consistency check: each listed path exists in the pack; extra pack files not in the list are reported (info).
- Profile subsetting for WindowsDesktop (`WPF`, `WindowsForms`, both); files without `Profile` always included.
- Culture filtering helper (full set by default; policy applied later by the Resources handler).
- Ownership lookup by destination path (ordinal-ignore-case) and by assembly name.
- Reference set for crossgen2: all managed implementation assemblies incl. CoreLib from `native/`.
- Diff between two versions of the same pack: added, removed, changed (by `AssemblyVersion`/`FileVersion`, and content hash when both packs are present).
- `RuntimeListFrameworkCatalogue : Inspection.Frameworks.IFrameworkCatalogue` (WU-303 contract, architecture §3.2), backed by cached/offline-available catalogues. Registration in the Cli happens in WU-704.

**Out**
- Deciding which frameworks/profiles an app needs (WU-803). Removal decisions (WU-804). Patch planning (WU-900).
- Cli wiring of the catalogue (WU-704).

## Deliverables

- `IRuntimePackCatalogue.GetAsync(FrameworkName, NuGetVersion, Rid, AcquisitionContext, CancellationToken) → Result<RuntimePackCatalogue>`.
- `RuntimePackCatalogue {Framework, Version, Rid, PackProvenance, Files}`; `RuntimePackFile {Type, PackPath, Destination (RelativePath), AssemblyName?, AssemblyVersion?, FileVersion?, PublicKeyToken?, Culture?, Profiles}`.
- `RuntimeListParser` (pure, stream-based, XML DTD processing disabled).
- `Subset(profiles, cultures?)`, `TryGetOwner(RelativePath)`, `ReferenceAssemblies`, `RuntimePackDiff Diff(RuntimePackCatalogue other)`.
- Diagnostics (proposed `RPK71xx`): missing/malformed `RuntimeList.xml`, listed file missing in pack, unknown `Type`, duplicate destination, framework/RID mismatch.

## Design Notes

- Follow the WU-006 spike report (`Docs/Spikes/WU-006-*`) and ADR. **Spike/ADR decisions override this spec where they differ** (notably destination mapping, `Profile` values and patch-to-patch variance).
- XML: `XmlReader` with `DtdProcessing.Prohibit`, `XmlResolver = null` (untrusted input, architecture §17).
- Collections sorted by destination (ordinal-ignore-case) for deterministic output; the catalogue is canonical-JSON serialisable (for plans and `runtime-inventory.json`).
- The catalogue is immutable and cached per `(framework, version, rid)` for the process lifetime.
- `Profile` attribute may list several profiles (separator per spike); a file matches when it has no profile or intersects the selected set.

## Acceptance Criteria

- [ ] AC-1 Parsing the `RuntimeList.xml` of NETCore, WindowsDesktop and AspNetCore packs for one net8 and one net10 version matches Verify snapshots (counts per `Type`, profiles, cultures, sample entries).
- [ ] AC-2 Every listed file resolves to an existing pack file; a fixture with a missing file yields an `RPK71xx` diagnostic.
- [ ] AC-3 Malformed, truncated and DTD-bearing XML fixtures produce diagnostics without exceptions; no external entity is resolved.
- [ ] AC-4 `System.Private.CoreLib.dll`, `hostfxr.dll` and `hostpolicy.dll` are present in the NETCore catalogue with app-root destinations; CoreLib is in `ReferenceAssemblies`.
- [ ] AC-5 The NETCore destination set equals the runtime-owned files of the matrix net8 and net10 SC console app (differences only as allowlisted in the WU-006 report).
- [ ] AC-6 NETCore + WindowsDesktop subset `WPF` equals the runtime-owned files of the matrix SC WPF app; subset `WindowsForms` equals those of the SC WinForms app (allowlist as in AC-5).
- [ ] AC-7 `Subset(WPF)` excludes `WindowsForms`-only files and vice versa; files without `Profile` are in both.
- [ ] AC-8 Resource files map to `<culture>/<name>.resources.dll`.
- [ ] AC-9 `TryGetOwner` is ordinal-ignore-case and returns the owning framework and version.
- [ ] AC-10 `Diff` between two NETCore 8.0 patch versions reports added/removed/changed entries, sorted, snapshot-verified.
- [ ] AC-11 Two catalogue serialisations of the same pack are byte-identical.
- [ ] AC-12 `RuntimeListFrameworkCatalogue` answers WU-303 `TryGetFrameworkAssembly` for `System.Runtime` in NETCore 8.0.x (hit) and an unknown name (miss); with the pack absent and `--offline`, it reports a miss so the Model falls back to `framework-provided (unverified)`.

## Test Requirements

- xUnit v3 + Verify; parser unit tests with small synthetic `RuntimeList.xml` fixtures under `tests/DotNetRepack.Acquisition.Tests/Fixtures/RuntimeList/`.
- Real-pack tests use `LocalPackageFeedFixture` (WU-700) seeded from the global packages folder populated by `Build-TestApps.ps1` (SC publishes restore runtime packs); tag `Category=Matrix`. Network download only under `Category=Network`.
- AC-5/AC-6 compare against `artifacts/testapps/manifest.json`.
- Run: `dotnet test --project tests/DotNetRepack.Acquisition.Tests --filter-trait "WU=701"`.
- Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green.
- `RPK71xx` codes listed for WU-1001; any allowlist differences documented in the test with a link to the WU-006 report.

## Agent Notes

- Do not hard-code RIDs or TFM folder names; derive from `RuntimeList.xml` paths and the RID knowledge base (WU-203).
- Keep `Acquisition` free of AppSpec semantics (architecture §3.1).

## Open Questions

- **Resolved** — exposing the catalogue to the Model: `IFrameworkCatalogue` lives in Inspection (WU-303), implemented here, wired by the Cli (WU-704); Model falls back to `framework-provided (unverified)` when none is wired (architecture §3.2).
- Should the catalogue include `DropFromSingleFile`-type attributes for future single-file support, or ignore them?
