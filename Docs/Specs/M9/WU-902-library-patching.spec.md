# WU-902 library-patching

| Field | Value |
|---|---|
| ID | WU-902 |
| Title | library-patching |
| Milestone | M9 Retargeting & Patching → v0.5.0-preview |
| Status | Not started |
| Depends on | WU-700, WU-802, WU-503, WU-305 |
| Parallel with | WU-603, M7, WU-803–WU-805, WU-900, WU-901 |
| Target project(s)/paths | `src/Tailor.Transforms/` (`Patch.Library` handler, asset selection), `src/Tailor.Specifications/` (only if members are missing), `tests/Tailor.Transforms.Tests/`, `tests/Tailor.IntegrationTests/`, `tests/TestApps/` |
| Size | L |

## Goal

Implement the `Patch.Library` handler. It updates **explicitly selected** non-core packages to a version allowed by a version policy (`exact`, `patch`, `minor`, `range`). It maps app assemblies to packages through deps.json, picks the best `lib/<tfm>` asset for the target TFM, replaces assets and associated files, and updates deps.json. It fails if the new version would need an unrequested transitive upgrade.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [TS §12.3–12.5](../../Requirements/Transformation_Specification.md) | Library selection, package sources, upgrade limits, no unrequested upgrades |
| [TS §19.4](../../Requirements/Transformation_Specification.md), [TS §29](../../Requirements/Transformation_Specification.md) | Provenance, sources, no credentials, reproducibility |
| [TS §24.2](../../Requirements/Transformation_Specification.md) | "Package update unavailable" policy |
| [TS §32.8](../../Requirements/Transformation_Specification.md) | Explicit upgrades invariant |
| [RQ §5.2](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Patch selected non-core libraries (explicit opt-in) |
| [CK §9.3](../../Requirements/Read_to_run_Cake.md) | Library updates via NuGet, no silent upgrades |
| [Architecture §8, §9, §10, §17](../../Architecture/Tailor.architecture.md) | Explicit-upgrade check, `Patch.Library`, acquisition/provenance, secrets |

## Scope

**In**
- `Patch.Library` handler: category `patch.library`, phase `Patch`.
- Mapping: deps.json `libraries` entries with `type: package` → the runtime/native/resource assets under the target → matching EAM files. Assemblies without a package entry cannot be selected (error).
- Selection: `operations.patch.libraries[]` entries by package id (exact, case-insensitive). No entry = no change. A selection that matches nothing follows the "selector matches nothing" policy ([TS §24.2](../../Requirements/Transformation_Specification.md)). Framework and `runtimepack.*` packages cannot be selected (error; use WU-900/WU-901).
- Version policy per entry (`LibraryVersionPolicy`, WU-102): `exact` (version), `patch` (same major.minor), `minor` (same major), `range` (NuGet range syntax). `LibraryVersionPolicy.ToRangeString` (WU-102) maps each onto a NuGet range for the WU-700 generic resolver (architecture §10), which picks the highest allowed stable version (prerelease only if the range includes one). Resolve at plan time and pin (id, version, source, sha512).
- Asset selection: NuGet.Frameworks nearest-framework rules for the target TFM (post-retarget TFM if WU-901 runs) for `lib/`, `runtimes/<rid>/lib|native/` (RID from the target state) and satellite `lib/<tfm>/<culture>/`.
- Actions: `Replace` existing asset files at their current relative paths. `Add` assets that are new in the package version. `Remove` assets no longer in the package version, subject to dependency safety ([TS §20.3](../../Requirements/Transformation_Specification.md)). Associated files: replace `.xml`/`.pdb` if the package has them, otherwise remove the stale one with an info diagnostic.
- deps.json update (via WU-802): library `version`, `sha512`, `path`, `hashPath`, asset `assemblyVersion`/`fileVersion`, and dependency ranges of the updated library.
- Dependency closure check: for each updated package, every nuspec dependency for the target TFM must be satisfied by the version already in deps.json, or by another selected update. Otherwise planning fails, listing the package and the required version. No transitive upgrade is ever added implicitly.
- "Update unavailable" (nothing newer within policy) → policy-driven (`error|warning|skip`, default `warning` + unchanged).
- Diagnostics in `RPK9xxx` (proposed sub-range `RPK92xx`).

**Out**
- Implicit or transitive upgrades. Adding brand-new packages. Framework/runtime packs. Assembly binding redirects (not used by .NET Core).
- Pattern/selector-based package selection (see Open Questions).

## Deliverables

- `Patch.Library` handler, asset selector, and closure checker.
- A package provenance section in the plan (existing plan schema acquisition records).
- A test app (or an extension of an existing one) referencing `Newtonsoft.Json` at an older version plus one package with a transitive dependency. A local feed fixture with several versions, including one that needs a newer transitive dependency.

## Design Notes

- The architecture is the baseline. The WU-007 spike/ADR (NuGet.Protocol usage, source mapping) overrides it where they differ.
- Library policies (`exact`, `patch`, `minor`, `range`) and runtime version policies (`matchSource`, `latestPatch`, `exact`, `range`) are separate types over one Acquisition range resolver ([Architecture §10](../../Architecture/Tailor.architecture.md)). Acquisition stays policy-free.
- Read the nuspec and file list from the downloaded package (`PackageArchiveReader`). Do not guess asset layout.
- Package sources come from `nuget.config` names/URLs only. Credentials never appear in the plan, logs or artefacts.
- An input without deps.json cannot use library patching. This is a validation error with a clear remedy.

## Acceptance Criteria

- [ ] AC-1 With `Newtonsoft.Json` selected and `allow: patch`, the plan updates it to the highest patch of the same major.minor in the local feed, pinned with id, version, source and sha512.
- [ ] AC-2 `minor`, `range` and `exact` each resolve the expected version on the feed fixture (table-driven unit tests, including prerelease exclusion).
- [ ] AC-3 The chosen assets match NuGet nearest-framework selection for the target TFM (unit tests over a package with `net462`, `netstandard2.0`, `net6.0` and `net8.0` folders).
- [ ] AC-4 After `apply`, the deps.json library entry and asset versions match the new package. The replaced DLL's `AssemblyVersion`/`FileVersion` match the package asset. The output AppSpec validates and the app launches (harness).
- [ ] AC-5 Associated `.xml`/`.pdb` files are replaced when the package ships them and removed with an info diagnostic when it does not.
- [ ] AC-6 A version that requires a newer transitive dependency that is not selected fails planning with exit code 1, naming both packages and the required range. Selecting the transitive package too makes planning succeed.
- [ ] AC-7 No deps.json library other than the selected ones changes version (test compares all entries before and after).
- [ ] AC-8 Selecting a framework/runtimepack package, a package id not in deps.json, or an app without deps.json produces distinct errors.
- [ ] AC-9 "Update unavailable" follows the policy: `warning` by default (unchanged), `error` fails, `skip` is silent. The plan records the outcome.
- [ ] AC-10 Two plan runs are byte-identical. No credentials appear in any artefact (test scans artefacts for the feed's credential value).

## Test Requirements

- xUnit v3 on MTP. Golden files for plans and deps.json diffs.
- Offline local package feed fixture built by a script from checked-in `.nuspec` + generated minimal assemblies, or copied from the CI package cache. Use an authenticated-feed mock only if WU-700 provides one.
- Integration over the library test app in `artifacts/testapps/`. Launch smoke only in the harness.
- Record Test Evidence: commands, TRX, feed package hashes and plan hashes.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=902"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=902"` (integration tests carry `Category=Integration`, `Category=Matrix`, and `Category=Launch` for smoke runs).

## Definition of Done

- All AC verified. CI green.
- Test app and feed fixture are reproducible from the repo scripts.
- ADR/architecture updated if the TransformSpec schema or Acquisition contracts change.
- The plan status is updated. Test Evidence is recorded.

## Agent Notes

- WU-503 (planner) and WU-305 (EAM identities) are dependencies in the plan.
- Use NuGet.Versioning/NuGet.Frameworks types for ranges and nearest-framework. Do not hand-roll comparisons.
- When WU-901 runs in the same plan, the target TFM comes from `ResolveTargetState`, not from the input.

## Open Questions

- **Resolved** — plan dependency gap: WU-503 and WU-305 added to the plan.
- Should selection support id globs (`Contoso.*`) or selector-based assembly groups ([TS §12.3](../../Requirements/Transformation_Specification.md))? Proposed: exact ids in v1.
- Default for "update unavailable": `warning` (proposed) or `error`.
