# Releasing .NET Tailor

This guide describes the intended release process for the `dotnet-tailor` tool. It does not publish packages or create GitHub releases.

## Version and Tags

Release tags use the form `v<semver>` and must contain a valid SemVer 2 version, for example:

```text
v0.1.0-preview.1
v1.0.0
```

The tag version must equal the package version emitted by the build. Preview versions include a prerelease label such as `-preview.1`; GitHub prereleases must be used for those tags. Stable tags create normal GitHub releases.

Before the first release, confirm that the repository's versioning configuration derives the package version from these tags and that an untagged build receives a deterministic prerelease version. Confirm the result with:

```powershell
dotnet pack src/Tailor.Cli/Tailor.Cli.csproj -c Release -o artifacts/pkg
dotnet tool install --tool-path artifacts/tool --add-source artifacts/pkg dotnet-tailor
artifacts/tool/dotnet-tailor.exe --version
```

## Package Metadata

The package is the `dotnet-tailor` .NET tool. Before publishing, inspect the `.nupkg` and `.snupkg` and verify:

- the tool command is `dotnet-tailor`;
- the package contains the repository readme;
- the licence expression is `Apache-2.0`;
- the repository URL is `https://github.com/kjbartel/DotNetTailor`;
- the repository commit is present in the package metadata; and
- the package contains no credentials, machine-specific paths or unintended files.

The package must be built from the tagged commit. Keep the `.nupkg`, `.snupkg`, SBOM and their hashes together as release evidence.

## NuGet Trusted Publishing

Publishing is intended to use NuGet trusted publishing through GitHub Actions OIDC. No NuGet API key is required or permitted.

One time, the package owner must:

1. Reserve and verify the `dotnet-tailor` package identity on nuget.org.
2. Create a NuGet trusted-publishing policy for package `dotnet-tailor`.
3. Restrict the policy to owner `kjbartel`, repository `DotNetTailor`, the release workflow file and the `release` environment.
4. Configure the GitHub `release` environment with required reviewers.
5. Grant only the workflow permissions needed for the job: `contents: write` for the GitHub release, `id-token: write` for OIDC exchange and `attestations: write` for build provenance.

The release workflow must fail when OIDC login or the NuGet trusted-publishing exchange fails. It must not fall back to `NUGET_API_KEY`, another secret, or a credential embedded in a command line.

## Attestations

The release should attach the `.nupkg`, `.snupkg` and SBOM to the GitHub release, and attest the package artefacts and SBOM with build provenance. After a release, verify an artefact against the repository and workflow identity:

```powershell
gh attestation verify dotnet-tailor.<version>.nupkg --repo kjbartel/DotNetTailor
```

Use the exact downloaded filename. Record the verification output, package hash and workflow run URL with the release evidence.

## GitHub URL Redirects

The canonical repository URL is `https://github.com/kjbartel/DotNetTailor`. GitHub may redirect an old repository URL after a rename, but redirects are not a migration strategy. Raw-content URLs, badges, package metadata, webhook targets, automation tokens, cached links and external integrations may not follow redirects consistently. Update repository-owned references to the canonical URL and verify external integrations separately.

## Current Readiness

The repository currently has no release workflow. Workflow creation remains blocked until the required third-party action versions and full commit SHAs can be verified from their authoritative upstream release tags. Do not create a workflow with guessed pins, publish from a local machine, or add a NuGet API key as a workaround.
