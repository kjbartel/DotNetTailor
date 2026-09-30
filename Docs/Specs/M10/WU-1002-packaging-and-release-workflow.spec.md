# WU-1002 packaging-and-release-workflow

| Field | Value |
|---|---|
| ID | WU-1002 |
| Title | packaging-and-release-workflow |
| Milestone | M10 Configuration, Hardening & Release → v1.0.0 (**Scheduled: immediately after WU-404; enables v0.1.0-preview**, plan risk R11 resolved) |
| Status | Not started |
| Depends on | WU-002, WU-404 |
| Parallel with | WU-405, WU-406, M5–M9 |
| Target project(s)/paths | `src/Tailor.Cli/Tailor.Cli.csproj` (tool packaging), `Directory.Build.props` / `Directory.Packages.props` (MinVer, SourceLink, deterministic CI build), `.github/workflows/release.yml`, `.github/workflows/ci.yml` (pack + install smoke), `Docs/Guides/releasing.md`, `tests/Tailor.IntegrationTests/` (tool install smoke) |
| Size | M |

## Goal

Make `dotnet-tailor` a releasable `dotnet tool`: SemVer from git tags, a tag-triggered GitHub release workflow that publishes to nuget.org with trusted publishing (OIDC, no API key), plus an SBOM and build-provenance attestations. This WU unblocks the v0.1.0-preview release at M4. It must not wait for M10.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md) | C# CLI, NuGet `dotnet tool`, SemVer, CI/CD suitability |
| [RQ §11](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Tool targets a stable LTS runtime |
| [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Auditability, determinism |
| [Architecture §1, §3, §17](../../Architecture/Tailor.architecture.md) | Package id/command, `PackAsTool`, deterministic build, secrets |
| [Plan: Release Points, risk R11, M4 + M10 criteria](../../Plans/Tailor.plan.md) | Preview releases, `dotnet pack`/install, v1.0.0 via release workflow |

## Scope

**In**
- Tool packaging: `PackAsTool=true`, `ToolCommandName=dotnet-tailor`, `PackageId=Tailor.Tool` (placeholders per [Architecture §1](../../Architecture/Tailor.architecture.md)), `TargetFramework=net10.0`, package readme, `PackageLicenseExpression` (placeholder until licence decided), repository metadata, SourceLink, `ContinuousIntegrationBuild=true` in CI, symbols package (`.snupkg`).
- Versioning: MinVer from tags `v<semver>` (e.g. `v0.1.0-preview.1`, `v1.0.0`). Untagged builds get a height-based prerelease. `dotnet-tailor --version` shows the package version.
- CI (`ci.yml`): pack on every PR, install the package from a local folder feed with `dotnet tool install --tool-path` and run `--version` and `--help`.
- Release workflow (`release.yml`), triggered by `v*` tag push:
  - Build, test, pack (same commands as CI).
  - SBOM (SPDX or CycloneDX) for the package, attached to the GitHub release.
  - `actions/attest-build-provenance` for `.nupkg`/`.snupkg` (and SBOM attestation).
  - Publish to nuget.org through trusted publishing (OIDC token exchange via the `NuGet/login` action), in a protected `release` environment with required reviewers.
  - Create the GitHub release with artefacts. The release is marked prerelease when the version has a prerelease label.
- Hardening: least-privilege `permissions` per job (`id-token: write`, `attestations: write`, `contents: write` only where needed), third-party actions pinned by commit SHA, no long-lived secrets.
- `Docs/Guides/releasing.md`: tag conventions, one-time nuget.org trusted-publishing policy setup, how to verify attestations (`gh attestation verify`).

**Out**
- Author-signing the package with a code-signing certificate (nuget.org repository signing applies). Authenticode signing of the tool. Actual publication of v1.0.0 (a release action at M10, not part of this WU).

## Deliverables

- Packaging properties, MinVer config, CI pack/install job, `release.yml`, `releasing.md`, and a tool-install smoke test.

## Design Notes

- The M4 pack/install criterion is split: WU-404 owns the local `dotnet pack` + install smoke; this WU owns publishing v0.1.0-preview through the release workflow. If WU-404 already set `PackAsTool`, keep its settings and add only what is missing.
- Deterministic packages: `Deterministic=true` is already set ([Architecture §3](../../Architecture/Tailor.architecture.md)). Add `ContinuousIntegrationBuild` and deterministic path maps so that two packs of the same commit produce the same DLLs.
- Trusted publishing needs a policy created on nuget.org by the package owner (repo, workflow file, environment). The workflow must fail clearly when the OIDC login fails and must not fall back to an API key.
- Prerelease tags must follow SemVer 2 so that NuGet orders `-preview.N` correctly.

## Acceptance Criteria

- [ ] AC-1 `dotnet pack` produces `Tailor.Tool.<version>.nupkg` and `.snupkg`. The nuspec has `packageType DotnetTool`, readme, repository URL/commit and licence metadata.
- [ ] AC-2 On a commit tagged `v0.1.0-preview.1`, the package version is exactly `0.1.0-preview.1`. On an untagged commit it is a prerelease higher than the last tag (MinVer), and `dotnet-tailor --version` prints it.
- [ ] AC-3 CI installs the packed tool from a local folder feed with `dotnet tool install --tool-path` and runs `--version` and `--help` successfully on `windows-latest`.
- [ ] AC-4 `release.yml` runs only on `v*` tags, uses the `release` environment, has least-privilege permissions per job, and pins every third-party action by full commit SHA (checked by a CI lint step or `zizmor`).
- [ ] AC-5 The release workflow publishes through OIDC trusted publishing. No NuGet API key secret is referenced anywhere in `.github/`.
- [ ] AC-6 The GitHub release includes the `.nupkg`, `.snupkg` and SBOM. `gh attestation verify` succeeds for the `.nupkg` (demonstrated with a preview tag or on a fork/test feed).
- [ ] AC-7 Prerelease versions create GitHub prereleases. Stable versions create normal releases.
- [ ] AC-8 `Docs/Guides/releasing.md` documents tag format, trusted-publishing setup and attestation verification.

## Test Requirements

- xUnit v3 integration test for tool install + `--version` (runs the packed package from a temp tool path; skipped with a clear reason if the package was not built). Traits `Category=Integration`, `WU=1002`.
- Run: `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=1002"`.
- Workflow validation: `actionlint` (and optionally `zizmor`) in CI.
- A dry run of the release workflow on a test tag against a test feed or with the publish step disabled through `workflow_dispatch` input.
- Record Test Evidence: workflow run links, package hash and attestation verification output.

## Definition of Done

- All AC verified. CI green. At least one successful release-workflow dry run is recorded.
- The plan status is updated. Test Evidence is recorded.

## Agent Notes

- Schedule this immediately after WU-404 (R11). The v0.1.0-preview release depends on it.
- Never add a NuGet API key as a fallback. Ask the repo owner to create the trusted-publishing policy.
- Get the current action versions and their SHAs from the upstream repos at implementation time. Do not copy SHAs from memory.

## Open Questions

- The licence is undecided. Publishing to nuget.org is blocked until the licence expression is set.
- The final package id/command name is undecided. Reserve the id on nuget.org before the first preview.
- SBOM format (SPDX via Microsoft `sbom-tool` vs CycloneDX).
- **Resolved** — milestone placement: the WU keeps its M10 ID and file; the plan marks it "Scheduled: immediately after WU-404 (enables v0.1.0-preview)".
