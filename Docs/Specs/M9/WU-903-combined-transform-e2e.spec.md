# WU-903 combined-transform-e2e

| Field | Value |
|---|---|
| ID | WU-903 |
| Title | combined-transform-e2e |
| Milestone | M9 Retargeting & Patching → v0.5.0-preview |
| Status | Not started |
| Depends on | WU-900, WU-901, WU-902, WU-704 |
| Parallel with | WU-1000–WU-1002 |
| Target project(s)/paths | `tests/DotNetRepack.IntegrationTests/` (TS §33 scenario, fixtures under `Fixtures/Ts33/`), `tests/DotNetRepack.RegressionTests/` (snapshots). Production code only for defects found (see Scope) |
| Size | M |

## Goal

Prove that the [TS §33](../../Requirements/Transformation_Specification.md) conceptual example works as one declarative TransformSpec over the WPF plugin test app. `plan`, `apply --dry-run` and `apply` all succeed. The output validates, all output assertions pass, the app launches, and two runs are deterministic. This WU closes the M9 milestone.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [TS §33](../../Requirements/Transformation_Specification.md) | Conceptual example |
| [TS §23.3](../../Requirements/Transformation_Specification.md), [RQ §6](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Combined phase ordering |
| [TS §25](../../Requirements/Transformation_Specification.md), [TS §26](../../Requirements/Transformation_Specification.md) | Output assertions, dry-run and per-file action report |
| [TS §32.6–32.9, §32.12–32.14](../../Requirements/Transformation_Specification.md) | Composable, deterministic, explicit upgrades, preserve by default, plan before execution, projected validation |
| [RQ §5](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §7](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | All categories combined, dry-run, determinism |
| [Architecture §4, §11, §15, §16](../../Architecture/DotNetRepack.architecture.md) | Pipeline, staging, determinism, testing strategy |
| [Plan M9 acceptance criteria](../../Plans/DotNetRepack.plan.md) | Milestone gate |

## Scope

**In**
- TransformSpec fixture `ts33.transform.json` expressing each TS §33 bullet:

| TS §33 intent | TransformSpec element |
|---|---|
| Retarget to `net10.0-windows` | `operations.retarget.tfm: "${targetTfm}"` |
| SC `win-x64` | `operations.deploymentModel {target: selfContained, rid: win-x64}` |
| Latest permitted runtime patch | `operations.patch.runtime.version: latestPatch` |
| Selected non-core libraries within ranges | `operations.patch.libraries[]` (one package, `allow: patch`/`range`) |
| Preserve app and plugin assemblies | Default preservation (no rule) |
| Remove other-platform runtime assets | Rule excluding `rid` ≠ target |
| Keep `en`/`en-*` resources | Rule excluding `not culture [en, en-*]` for `classification: resource` |
| Keep config files of included assemblies | Default association behaviour |
| Exclude XML docs | Rule on `association: xmlDoc` |
| Separate PDBs | `symbols.policy: separate`, zip output |
| R2R app + plugins except excluded | `optimisation.readyToRun.select` with `not name` exclusion |
| Preserve unclassified content | Default catch-all preservation |

- `output.assert`: `tfm net10.0-windows`, `deploymentModel selfContained`, `rid win-x64`, runtime version = pinned, no non-`en` satellites, no `.xml` docs, R2R state for selected assemblies, symbols policy.
- End-to-end runs via the CLI: `analyze` → `validate` → `plan` → `apply --dry-run` → `apply` (twice, different output dirs).
- Fixing small integration defects found in M5–M9 code, each with a regression test. Larger defects become new WUs or bugs.

**Out**
- New transformation features. Template authoring (WU-1003). Performance tuning (WU-1004).

## Deliverables

- `tests/DotNetRepack.IntegrationTests/Fixtures/Ts33/ts33.transform.json` (+ README comment block mapping the TS §33 bullets, if the JSON reader tolerates comments).
- Integration test class `Ts33CombinedTransformTests` and Verify snapshots of the normalised plan, projected AppSpec and output AppSpec.
- Any local feed additions needed (net10 runtime/host/crossgen2 packs, library versions).

## Design Notes

- Input: the net8 FD WPF plugin host from `artifacts/testapps/` (plugin chain + satellites + multi-RID `runtimes/` + a library dependency). If a TS §33 facet is missing from the test app (e.g. a nameable R2R exclusion target, a patchable library, non-`en` satellites), extend the test app source and `Build-TestApps.ps1` in this WU.
- The AppSpec comes from `analyze` and is committed as a fixture after review (not regenerated per run) so that the TransformSpec selectors bind to stable ids.
- Normalise machine paths and timings before snapshotting. Canonical artefacts must not need normalisation.
- Spike reports/ADRs override the architecture where they differ (notably WU-004 R2R determinism fallback).

## Acceptance Criteria

- [ ] AC-1 `ts33.transform.json` validates against the committed TransformSpec schema and contains no per-file enumeration (no `path` selector that names a single file, except the R2R exclusion by assembly `name`).
- [ ] AC-2 `plan` succeeds (exit 0). The plan orders actions in phases Retarget → Patch → DeploymentModel → FilteringLayout → Optimisation → ConfigGeneration → ProjectedValidation, and pins every acquired package (id, version, source, sha512).
- [ ] AC-3 `apply --dry-run` makes no filesystem changes outside `--artifacts` and the NuGet package cache (before/after tree snapshot; with `--offline` the cache is unchanged too).
- [ ] AC-4 `apply` exits 0. The output AppSpec validates against the output tree and all output assertions pass.
- [ ] AC-5 Output facts: runtimeconfig `tfm` = `net10.0-windows` with `includedFrameworks` at the pinned version; no files under `runtimes/<rid>/` for RIDs other than `win-x64` (and its compatible parents); no satellite folders other than `en`/`en-*`; no `.xml` docs; no `.pdb` in the main output; the symbols zip contains the PDBs.
- [ ] AC-6 Every selected app/plugin assembly is R2R per the inspector, and the excluded assembly is not. Skipped/ineligible assemblies have reasons in the plan.
- [ ] AC-7 Only the selected library changed version. Every other deps.json library entry has its input version (test-enforced).
- [ ] AC-8 The output launches: the harness starts the WPF host, loads the plugin chain and exits 0.
- [ ] AC-9 Two `apply` runs into different output directories produce byte-identical trees (hash manifest compare), plans and output AppSpecs. Any R2R exception follows the WU-004 ADR and is listed explicitly.
- [ ] AC-10 All M9 plan acceptance criteria are demonstrably met by this and WU-900–WU-902 tests (checklist in the PR).

## Test Requirements

- xUnit v3 on MTP, Verify. Tests run offline against the local package feed fixture (net10 runtime, WindowsDesktop, host, crossgen2 packs; library versions).
- Mark the class `Category=Integration`, `Category=Matrix`, `Category=Launch` (nightly if CI time requires, plan risk R7) but runnable locally with one command: `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=903"`.
- Record Test Evidence: commands, TRX, output hash manifests of both runs, and launch log.

## Definition of Done

- All AC verified. CI green (including the integration tier where it runs).
- M9 milestone criteria ticked in the plan by the Verifier.
- Defect fixes have their own regression tests. The plan status is updated. Test Evidence is recorded.

## Agent Notes

- Do the dry-run first and review the plan snapshot before running `apply`. Most failures show up as ordering or selector-binding issues.
- If a TS §33 intent cannot be expressed with the current schema, stop and raise it as an Open Question. Do not add ad-hoc schema members here.

## Open Questions

- Which library and version range to use in the fixture (proposed: `Newtonsoft.Json`, `allow: patch`).
- Whether the full combined run is PR-tier or nightly-tier in CI.
