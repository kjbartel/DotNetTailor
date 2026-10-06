# WU-700 nuget-acquisition-service

| Field | Value |
|---|---|
| ID | WU-700 |
| Title | nuget-acquisition-service |
| Milestone | M7 Acquisition & ReadyToRun (v0.3.0-preview) |
| Status | Ready |
| Depends on | WU-007, WU-100 |
| Parallel with | M2–M6 |
| Target | `src/Tailor.Acquisition/` (`Packages/`), `tests/Tailor.Acquisition.Tests/` |
| Size | L |

## Goal

Provide a deterministic, credential-safe NuGet acquisition service that resolves a package request to a pinned version at plan time, downloads it into the global packages folder, verifies its SHA-512 and returns provenance `{id, version, source, sha512}`. All later WUs (runtime packs, crossgen2, host pack, library patching) consume packages only through this service.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §29](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials) | Named sources/URLs, credentials never in specs, resolved identity recorded |
| [TS §12.4](../../Requirements/Transformation_Specification.md#12-patching-specification) | Allowed sources, version policies |
| [TS §19.4](../../Requirements/Transformation_Specification.md#19-addition-rules) | Provenance of added artefacts |
| [TS §3.6](../../Requirements/Transformation_Specification.md) | Determinism; pinned external inputs |
| [RQ §5](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Patching/deployment/optimisation need external packages |
| [Architecture §10](../../Architecture/Tailor.architecture.md#10-acquisition), [§13](../../Architecture/Tailor.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§17](../../Architecture/Tailor.architecture.md#17-security), [§19 item 18](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) | Service design, exit code 4, security, offline `latestPatch` |

## Scope

**In**
- `nuget.config` hierarchy discovery from a working directory (NuGet.Configuration `Settings.LoadDefaultSettings`), enabled sources, package source mapping, `packageSourceCredentials` — exactly as NuGet does.
- Optional source restriction from the TransformSpec (source name or URL) intersected with / added to configured sources.
- Generic version-range resolution: NuGet `VersionRange` → highest stable version in range (architecture §10). Runtime version policies and library version policies are mapped onto ranges by their callers.
- Plan-time resolution → `ResolvedPackage` with exact pinned version; execution-time acquisition never re-resolves.
- Download/extract into the global packages folder (shared with the SDK layout: `<id-lower>/<version-lower>/` + `.nupkg.metadata`).
- SHA-512 verification of cached and downloaded packages; provenance record.
- `--offline`: cache-only resolution; miss → error (exit code 4); highest cached version in range is picked, with a warning when the range is open-ended (e.g. mapped from `latestPatch`).
- Credentials from the standard NuGet configuration hierarchy only (`packageSourceCredentials` and credential providers), non-interactive. Never from the TransformSpec, CLI or tool config.
- Tool-level default versions (options object) for crossgen2, runtime packs and host pack, used only when a request carries no range.
- Package-id helpers: `Microsoft.NETCore.App.Crossgen2.<hostRid>`, `<Framework>.Runtime.<rid>`, `Microsoft.NETCore.App.Host.<rid>` (RID passed in; never hard-coded).
- `NuGetPackageLocator : Core.Packages.IPackageLocator` (WU-100 contract, architecture §3.2): maps `{id, version, sha512}` to the verified package root in the global packages folder.

**Out**
- RuntimeList parsing (WU-701), plan wiring of acquisition actions (consumers: WU-702/703/803/804/902).
- Config file / env var binding of default versions (WU-1000).
- Mapping of runtime version policies (`matchSource`, `latestPatch`, `exact`, `range`) and library policies (`exact`, `patch`, `minor`, `range`) onto ranges (WU-102 policy types; callers WU-803/804/900/901/902).
- Package signing verification beyond hash (open question).

## Deliverables

- `IPackageAcquisitionService` with `ResolveAsync(PackageRequest, AcquisitionContext, CancellationToken) → Result<ResolvedPackage>` and `AcquireAsync(ResolvedPackage, AcquisitionContext, CancellationToken) → Result<AcquiredPackage>`.
- Records: `PackageRequest {Id, VersionRange?, Sources?}` (NuGet `VersionRange`; exact = `[v]`), `ResolvedPackage {Id, Version, Source}`, `AcquiredPackage {Provenance, RootPath}`, `PackageProvenance {Id, Version, Source, Sha512}` (canonical-JSON serialisable). No policy enum: policies are mapped onto ranges by callers.
- `AcquisitionOptions {Offline, WorkingDirectory, DefaultVersions {Crossgen2, RuntimePack, HostPack}}` bound through the options pattern.
- `KnownPackageIds` helper; `NuGetPackageLocator`.
- NuGet `ILogger` adapter to `Microsoft.Extensions.Logging` with secret redaction.
- Diagnostics (proposed range `TLR70xx`): package not found, version not satisfiable, hash mismatch, offline cache miss, source unreachable, source not allowed by mapping, credential failure.
- Tests + `LocalPackageFeedFixture` (reused by WU-701/703/800/803/804).

## Design Notes

- Follow the WU-007 spike report (`Docs/Spikes/WU-007-*`) and its ADR (`Docs/Decisions/ADR-*`). **Where the spike or ADR differs from this spec, the spike/ADR decision wins**; record the deviation in the PR.
- Resolution: gather versions from all eligible sources (after source mapping), exclude prerelease unless the range explicitly names a prerelease; pick the highest stable version inside the `VersionRange`.
- Source selection for download: first source (in `nuget.config` order) that has the pinned version and is allowed by mapping. Record the source as its configured name plus URL with user-info removed.
- Cache: use `GlobalPackagesFolderUtility` / `PackageExtractor` so the SDK and tool share the cache and NuGet's file locks. Hash = base64 SHA-512 of the `.nupkg`; must equal `.nupkg.metadata` `contentHash`. A mismatch is an error; the package is never used.
- Credentials: honour the `nuget.config` hierarchy exactly as NuGet does — `packageSourceCredentials` (via `PackageSourceProvider`) and credential providers through `DefaultCredentialServiceUtility.SetupDefaultCredentialService(logger, nonInteractive: true)`. No credential input via TransformSpec, CLI or tool config. Never log headers, tokens or user-info.
- Determinism: outputs are sorted; `ResolvedPackage` is the only thing recorded in the plan. The same `ResolvedPackage` always yields the same `AcquiredPackage.Provenance`.
- Planning is side-effect-free except declared acquisition (architecture §4); `--offline` disables all HTTP.
- Offline behaviour follows architecture §19 item 18 (highest cached version in range + warning).

## Acceptance Criteria

- [ ] AC-1 Range `[v]` resolves to exactly `v`; a missing version yields an `TLR70xx` error naming id, range and searched sources.
- [ ] AC-2 With a local feed holding `8.0.1`, `8.0.3`, `8.0.4-preview.1`, `8.1.0`, the range `[8.0.0, 8.1.0)` (the mapping of runtime `latestPatch` for 8.0) resolves to `8.0.3`.
- [ ] AC-3 `range` `[8.0.1, 8.1.0)` resolves to the highest stable version in range (`8.0.3` in the AC-2 feed).
- [ ] AC-4 `AcquireAsync` for a `ResolvedPackage` does not re-resolve: adding `8.0.5` to the feed after resolution still yields `8.0.3`.
- [ ] AC-5 Provenance contains id, version, source and sha512; sha512 equals the base64 SHA-512 of the `.nupkg` and the `.nupkg.metadata` `contentHash`.
- [ ] AC-6 A cached package whose `.nupkg` or metadata hash was tampered with is rejected with an `TLR70xx` error and not returned.
- [ ] AC-7 A `nuget.config` in a parent of the working directory (with `<clear/>` + local feed) is honoured; sources from the user-level config are not used.
- [ ] AC-8 With package source mapping, a package mapped to feed B is resolved and downloaded from feed B even when feed A (listed first) contains the same id/version; provenance source = B.
- [ ] AC-9 `--offline` with a warm cache succeeds with zero HTTP requests (asserted with an unreachable source or a counting `HttpMessageHandler`).
- [ ] AC-10 `--offline` with an empty cache yields an error diagnostic classified as environment/acquisition failure (maps to exit code 4 per WU-105 mapping).
- [ ] AC-11 Offline resolution of an open range selects the highest cached version in range and emits a warning.
- [ ] AC-12 A source URL containing user-info or a token never appears in logs, diagnostics or provenance (captured-logger test).
- [ ] AC-13 A request without a version uses `AcquisitionOptions.DefaultVersions`; when no default is configured the result is an error.
- [ ] AC-14 Two resolutions of the same requests produce byte-identical canonical JSON of the provenance list.
- [ ] AC-15 The default test run makes no network calls; network tests are gated by `Category=Network`.
- [ ] AC-16 A local authenticated HTTP feed stub whose credentials are defined only in `nuget.config` `packageSourceCredentials` is resolved and downloaded successfully; the credential value never appears in logs, diagnostics or provenance.
- [ ] AC-17 `NuGetPackageLocator.GetPackageRoot` returns the extracted root for an acquired package and an error when the package is missing or its sha512 differs.

## Test Requirements

- xUnit v3 (MTP) in `tests/Tailor.Acquisition.Tests/`; golden files for canonical provenance JSON.
- `LocalPackageFeedFixture`: temp folder feed, temp global packages folder and temp `nuget.config`; never touches the user's cache. Synthetic packages built with `NuGet.Packaging.PackageBuilder`; optional seeding of real packs from the global packages folder populated by `build/Build-TestApps.ps1`.
- `Category=Network` tests (nightly only; skipped unless `DOTNET_TAILOR_TEST_NETWORK=1`): resolve and acquire `Microsoft.NETCore.App.Host.win-x64` `8.0.*` from nuget.org.
- Run: `dotnet test --project tests/Tailor.Acquisition.Tests --filter-trait "WU=700"`.
- Record Test Evidence (test-evidence skill) in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green (build, test, format) with `TreatWarningsAsErrors`.
- New `TLR70xx` codes listed in the PR description for WU-1001.
- Deviations from this spec backed by the WU-007 ADR or a new ADR; architecture §10 updated if behaviour changed.

## Agent Notes

- Do not add credential options anywhere. Do not use `Microsoft.NET.HostModel` or SDK MSBuild tasks.
- Keep the service free of AppSpec/TransformSpec types; callers map TransformSpec sources to `PackageRequest.Sources`.
- Use CPM (`Directory.Packages.props`) for `NuGet.Protocol`, `NuGet.Configuration`, `NuGet.Packaging`, `NuGet.Credentials`.

## Open Questions

- **Resolved** — `packageSourceCredentials`: honoured, with the whole `nuget.config` hierarchy exactly as NuGet does (architecture §10, §19 item 29).
- Should NuGet package signature verification (repository/author signatures) be required in addition to SHA-512?
- **Resolved** — default versions for tool packages: they follow the resolved target runtime version (architecture §10); `DefaultVersions` apply only to requests without a range.
