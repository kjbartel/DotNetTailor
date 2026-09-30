# WU-002 ci-pipeline

| Field | Value |
|---|---|
| ID | WU-002 |
| Title | ci-pipeline |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap) |
| Status | Not started |
| Depends on | WU-000 |
| Parallel with | WU-001, WU-003, WU-006, WU-007, WU-100 |
| Target paths | `.github/workflows/ci.yml`, `.github/dependabot.yml` |
| Size | S |
| Branch | `wu/002-ci-pipeline` |

## Goal

Run build, test and format verification on every push and PR on `windows-latest`, publish test results, and cache NuGet packages and the test-app matrix. Keep dependencies and actions current with Dependabot.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/DotNetRepack.architecture.md#1-summary) | §1 Hosting (GitHub Actions), Tests (MTP) | Platform |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#16-testing-strategy) | §16 Testing Strategy | Matrix cache key (TestApps source hash + SDK version) |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#17-security) | §17 Security | Supply chain, no secrets |
| [Plan](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap) | M0 criteria 1 and 3; [risk R7](../../Plans/DotNetRepack.plan.md#risks-register) | CI green, matrix cache |
| [RQ](../../Requirements/Repackage_tool_Requirements_v1.1.md) | §12 Non-Functional | Determinism, reproducibility |

## Scope

**In**: `ci.yml` (build/test/format job, test-apps job), `nightly.yml` (scheduled Network/Launch/performance tiers), `dependabot.yml`.

**Out**: release/pack/publish workflows (WU-1002), branch protection settings (repo admin action, list in PR), code signing.

## Deliverables

`.github/workflows/ci.yml`:

| Element | Requirement |
|---|---|
| Triggers | `push` to `main`, `pull_request`, `workflow_dispatch` |
| Permissions | Top-level `permissions: contents: read`; job-level additions only if needed (`checks: write` only if a test reporter action is used) |
| Concurrency | `group: ci-${{ github.ref }}`, `cancel-in-progress: true` for PRs |
| Env | `DOTNET_NOLOGO=1`, `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `NUGET_PACKAGES=${{ github.workspace }}/.nuget/packages` |
| Job `build-test` | `runs-on: windows-latest`; checkout; `actions/setup-dotnet` with `global-json-file: global.json` **and** `dotnet-version: 8.0.x` (runtime for net8 test apps); NuGet cache (`actions/cache`, key `nuget-${{ runner.os }}-${{ hashFiles('global.json','Directory.Packages.props','**/*.csproj','**/Directory.Build.props') }}`); `dotnet restore DotNetRepack.slnx`; `dotnet build DotNetRepack.slnx -c Release --no-restore -warnaserror`; a guard step that fails when `DOTNET_REPACK_UPDATE_GOLDEN` is set (golden files must never be rewritten in CI, architecture §16); `dotnet test --solution DotNetRepack.slnx -c Release --no-build --report-xunit-trx --results-directory TestResults`; `dotnet format DotNetRepack.slnx --verify-no-changes --no-restore`; `actions/upload-artifact` of `TestResults/**` with `if: always()` |
| Job `test-apps` | `runs-on: windows-latest`; guarded with `if: hashFiles('build/Build-TestApps.ps1') != ''` (script arrives in WU-003); setup-dotnet as above; cache `artifacts/testapps` keyed by the key printed by `build/Build-TestApps.ps1 -PrintCacheKey` (contract defined in [WU-003](WU-003-test-app-suite.spec.md)); run the script only on cache miss; upload `artifacts/testapps/manifest.json` as an artifact |
| Pinning | Every `uses:` pinned to a full 40-char commit SHA with a trailing `# vX.Y.Z` comment |

`.github/workflows/nightly.yml`: `schedule` (daily) + `workflow_dispatch`; same setup and pinning rules as `ci.yml`; `needs`-style reuse of the test-apps cache; steps run `dotnet test --solution DotNetRepack.slnx -c Release --filter-trait "Category=Network"` with `DOTNET_REPACK_TEST_NETWORK=1`, then `--filter-trait "Category=Launch"`, then `DotNetRepack.PerformanceTests` when that project exists. Each step passes `--ignore-exit-code 8` so tiers with zero tests (before M7/M10) do not fail. Later WUs (WU-700, WU-704, WU-1004) only add tests, not workflow steps.

`.github/dependabot.yml`: `version: 2`; ecosystems `nuget` (directory `/`) and `github-actions` (directory `/`); weekly; grouped updates (`xunit*` test group, `NuGet.*` group, all actions in one group); `open-pull-requests-limit: 5`.

## Design Notes

- The test-apps job exists before WU-003 finishes. It skips cleanly when the script is absent. WU-003 must implement `-PrintCacheKey`; do not duplicate the hash logic in YAML. If `hashFiles` in `if:` is evaluated before checkout, compute the guard in a step output after checkout instead.
- MTP TRX: use xUnit v3's built-in `--report-xunit-trx`. If WU-000 chose `Microsoft.Testing.Extensions.TrxReport`, use `--report-trx` instead. The flag must match what WU-000 configured.
- `-warnaserror` duplicates `TreatWarningsAsErrors` on purpose: it also covers MSBuild/NuGet warnings.
- Test-app job does not gate `build-test` (independent jobs). Integration tests that need the matrix come later and will declare `needs: test-apps`.
- Do not use `pull_request_target`. No secrets are required.

## Acceptance Criteria

- [ ] AC-1 `.github/workflows/ci.yml` and `.github/dependabot.yml` exist and parse as YAML; `actionlint` (if available locally or via a pinned action) reports no errors.
- [ ] AC-2 Every `uses:` line matches `@[0-9a-f]{40}\s+#\s*v\d` (`Select-String -Path .github/workflows/*.yml -Pattern 'uses:' | Where-Object { $_ -notmatch '@[0-9a-f]{40}\s+#\s*v\d' }` returns nothing).
- [ ] AC-3 The PR's CI run is green: `build-test` runs restore, build (`-warnaserror`), test (MTP) and format verify on `windows-latest`.
- [ ] AC-4 The run has an uploaded artifact containing at least one `*.trx` file.
- [ ] AC-5 A second run on the same commit (re-run) logs a NuGet cache hit.
- [ ] AC-6 Before WU-003 merges, `test-apps` is skipped without failing the workflow. After WU-003 merges, a re-run with unchanged `tests/TestApps/**` reports a cache hit for `artifacts/testapps` and does not run the script (M0 criterion 3; may be verified in WU-003's PR).
- [ ] AC-7 Workflow-level `permissions` is `contents: read`; no `secrets.` references exist.
- [ ] AC-10 `nightly.yml` exists, is triggered by `schedule` and `workflow_dispatch`, and a manual dispatch run is green with zero Network/Launch tests present.
- [ ] AC-8 `dependabot.yml` declares `nuget` and `github-actions` ecosystems.
- [ ] AC-9 Introducing a format violation on a scratch branch makes `build-test` fail at the format step (demonstrated once, link in PR).
- [ ] AC-11 `build-test` runs the `DOTNET_REPACK_UPDATE_GOLDEN` guard before `dotnet test`; the variable is not set anywhere in `.github/workflows/*.yml` (`Select-String -Path .github/workflows/*.yml -Pattern 'DOTNET_REPACK_UPDATE_GOLDEN\s*[:=]'` returns nothing).

## Test Requirements

- No new code tests. Evidence is the CI run links (AC-3..AC-6, AC-9) recorded in the PR.
- Record a Test Evidence block for the local `dotnet test --solution DotNetRepack.slnx -c Release` equivalent.

## Definition of Done

- All AC ticked by the Verifier; CI green on `main` after merge (M0 criterion 1).
- Solution still builds with zero warnings; format clean.
- Plan status updated. List recommended branch-protection settings (required check `build-test`) in the PR for the repo admin.

## Agent Notes

- Load: this spec, [WU-000 spec](WU-000-repository-scaffold.spec.md) (commands, TRX choice), [WU-003 spec](WU-003-test-app-suite.spec.md) (cache key contract).
- Resolve action SHAs from the actions' release tags (e.g. `git ls-remote https://github.com/actions/checkout refs/tags/v4.*`). Do not guess SHAs.
- Do not modify build props, projects or the plan beyond the status row.

## Open Questions

- Should the full test-app matrix run on every PR or only nightly/on `tests/TestApps/**` changes (risk R7)? Default here: every run, served from cache.
- Test result reporting UI (e.g. a test-reporter action needing `checks: write`) — not included by default.
