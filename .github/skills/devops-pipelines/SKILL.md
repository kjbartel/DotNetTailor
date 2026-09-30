---
name: devops-pipelines
description: "Use when adding or changing .NET Tailor GitHub Actions workflows (CI, nightly, release), Dependabot, caching, pack or publish steps."
---

# DevOps Pipelines

The contracts live in the specs: [WU-002 CI](../../../Docs/Specs/M0/WU-002-ci-pipeline.spec.md), [WU-003 test apps](../../../Docs/Specs/M0/WU-003-test-app-suite.spec.md), [WU-1002 release](../../../Docs/Specs/M10/WU-1002-packaging-and-release-workflow.spec.md), and the [release guide](../../../Docs/Guides/releasing.md). Implement them from those; do not re-derive.

## Rules

- Workflows live in `.github/workflows/`. Use `windows-latest`, `actions/setup-dotnet` with `global-json-file: global.json`, and the solution `DotNetTailor.slnx`.
- Pin every `uses:` to a 40-character SHA followed by `# vX.Y.Z`. Set top-level `permissions: contents: read` and widen only per job. Do not use `pull_request_target`.
- Store no secrets. Publishing uses NuGet trusted publishing (OIDC) and never falls back to an API key.
- Gates match `AGENTS.md`: build with `-warnaserror`, run `dotnet test --solution` in MTP mode with TRX upload (`if: always()`), and run `dotnet format --verify-no-changes`. Fail when `DOTNET_REPACK_UPDATE_GOLDEN` is set.
- Network and Launch tests run only in the nightly workflow. PR CI makes no network calls from tests.
- Cache keys hash `global.json`, `Directory.Packages.props`, `**/*.csproj` and `**/Directory.Build.props`. The test-app key comes from `build/Build-TestApps.ps1 -PrintCacheKey`.

## Validate locally

- Run the same `dotnet` commands the workflow runs, from the repo root.
- Check YAML syntax with `actionlint` if it is installed. Otherwise parse the file and review the diff by hand.
- State which behaviour could not be exercised locally, such as triggers, OIDC or caches. Those items are verified on the first PR run, and CI must be green before `Done`.
- Ask before changing triggers, permissions, environments or publishing steps.
