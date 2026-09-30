# WU-805 deployment-model-e2e

| Field | Value |
|---|---|
| ID | WU-805 |
| Title | deployment-model-e2e |
| Milestone | M8 Deployment Model Conversion (v0.4.0-preview) |
| Status | Not started |
| Depends on | WU-803, WU-804, WU-603 |
| Parallel with | WU-704, WU-900–WU-902 |
| Target | `tests/DotNetRepack.IntegrationTests/DeploymentModel/`, `tests/DotNetRepack.IntegrationTests/Fixtures/deployment/*.transform.json`; fixes in `src/DotNetRepack.*` only where E2E exposes defects |
| Size | M |

## Goal

Prove M8 end to end through the CLI: FD⇄SC for console, WinForms and WPF (net8, net10) produce outputs that launch, validate against the output AppSpec, and are semantically equivalent to SDK-published counterparts.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §13](../../Requirements/Transformation_Specification.md#13-deployment-model-transformation), [TS §25](../../Requirements/Transformation_Specification.md#25-output-state-requirements) | All four combinations; output assertions |
| [TS §19](../../Requirements/Transformation_Specification.md#19-addition-rules), [TS §20](../../Requirements/Transformation_Specification.md#20-removal-rules) | Provenance; safe removals |
| [CK §5](../../Requirements/Read_to_run_Cake.md#5-deployment-model-transformations) | FD⇄SC combinations |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Deployment model changes |
| [Plan M8 criteria](../../Plans/DotNetRepack.plan.md#m8-deployment-model-conversion--v040-preview), [Architecture §16](../../Architecture/DotNetRepack.architecture.md#16-testing-strategy) | Milestone gate; harness-only launch |

## Scope

**In**
- Scenarios: {console, WinForms, WPF} × {net8.0, net10.0} × {FD→SC, SC→FD}, plus FD→FD and SC→SC (no deployment actions) and FD→SC→FD round trip.
- Checks: launch, output AppSpec validation, semantic runtimeconfig/deps.json equality, file-set equality against matrix counterparts, apphost resources/subsystem, determinism.
- Signed-input fixture through the CLI (Authenticode warning).
- App-owned native DLL preservation on SC→FD (native test app).

**Out**
- New features; retarget/patch combinations (WU-903); R2R combination (WU-704/903).

## Deliverables

- TransformSpec fixtures (`to-sc.transform.json`, `to-fd.transform.json`, same-model variants), each stating an explicit runtime version or policy (e.g. `matchSource`), plus one negative fixture without it (validation error, exit 1).
- Integration tests + Verify snapshots; normalised comparers reused from WU-801/802.
- Defect fixes in owning `src` projects with focused unit tests.

## Design Notes

- Follow WU-005/WU-006 spike reports and ADRs; **they override this spec where they differ** (e.g. allowlisted differences to SDK output).
- Use the WU-405 regression harness and WU-603 CLI helpers.
- SC launch must prove the app-local runtime is used (test-app runtime-location output per the WU-003 smoke contract); FD launch requires the net8/net10 shared runtimes on the CI image.
- Comparison normalisation (normative for M8 SDK-equivalence, architecture §19 item 35), implemented once in `tests/DotNetRepack.IntegrationTests/Support/SdkEquivalenceNormaliser` and documented in its header:
  - JSON: semantic equality; object property order ignored; `frameworks`/`includedFrameworks` keyed by name; deps.json libraries and targets keyed by `name/version`.
  - RID-specific package assets: the tool output's `runtimeTargets` for the target RID (and its compatible parents) are flattened into `runtime`/`native` and other-RID entries dropped before comparing with the SDK RID-specific (SC) publish; the reverse applies for portable FD comparisons.
  - File sets by relative path; RID-specific asset files compared after the same flattening.
  - Allowlist lives in one file with a reason per entry.

## Acceptance Criteria

- [ ] AC-1 All 12 FD⇄SC scenarios produce outputs that launch via the smoke contract.
- [ ] AC-2 SC outputs run on the app-local runtime (asserted via the smoke contract's runtime location).
- [ ] AC-3 Every output AppSpec validates and output assertions (`deploymentModel`, `rid`) pass.
- [ ] AC-4 Output runtimeconfig and deps.json are semantically equal to the matrix counterpart (FD→SC vs SC publish, SC→FD vs FD publish) under the documented `SdkEquivalenceNormaliser`, which has its own unit tests (flattening, ordering).
- [ ] AC-5 Output file sets equal the matrix counterpart's file sets, apart from allowlisted entries.
- [ ] AC-6 WinForms/WPF outputs keep icon, version resources and `WINDOWS_GUI` subsystem of the input apphost.
- [ ] AC-7 A signed-input fixture produces the Authenticode warning through `apply` (and exit code 3 under `--strict`).
- [ ] AC-8 SC→FD removes only RuntimeList-catalogued files; the app-owned native DLL is preserved.
- [ ] AC-9 FD→SC→FD round trip yields runtimeconfig/deps.json semantically equal to the original FD input and the same file set.
- [ ] AC-10 FD→FD and SC→SC runs contain no deployment-model actions and their outputs launch.
- [ ] AC-11 Two runs per scenario produce identical output tree fingerprints and byte-identical plans.

## Test Requirements

- xUnit v3 + Verify in `tests/DotNetRepack.IntegrationTests/`, tagged `Category=Integration`, `Category=Matrix` and `Category=Launch`; trait `WU=805`.
- Run: `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=805"`.
- `LocalPackageFeedFixture` with isolated cache seeded from `Build-TestApps.ps1`; no network in default runs.
- Record Test Evidence (scenario matrix, fingerprints, allowlist) in the PR.

## Definition of Done

- All AC ticked by the Verifier; all M8 plan criteria demonstrably met; CI green.

## Agent Notes

- Data-drive scenarios from `manifest.json`; keep GUI launches bounded by the smoke-contract timeout.

## Open Questions

- Does the WU-003 smoke contract report the runtime location and support GUI auto-exit? If not, AC-1/AC-2 need a harness extension.
- Does the matrix contain an SC build of the native-DLL test app (needed for AC-8)?
- Where does the signed-input fixture come from (plan M8 criterion has no owner)? WU-800 provides a synthetic one; confirm it is enough for E2E.
