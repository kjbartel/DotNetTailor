# WU-900 runtime-patching

| Field | Value |
|---|---|
| ID | WU-900 |
| Title | runtime-patching |
| Milestone | M9 Retargeting & Patching → v0.5.0-preview |
| Status | Not started |
| Depends on | WU-803, WU-702, WU-703 |
| Parallel with | WU-901, WU-902, WU-805 |
| Target project(s)/paths | `src/Tailor.Transforms/` (`Patch.Runtime` handler), `src/Tailor.Inspection/` (R2R version-bubble facts, only if missing), `src/Tailor.Specifications/` (runtime patch options, only if missing), `tests/Tailor.Transforms.Tests/`, `tests/Tailor.IntegrationTests/` |
| Size | L |

## Goal

Implement the `Patch.Runtime` handler. It moves an app to another runtime patch of the **same major** version. SC apps get their framework files replaced by the `RuntimeList.xml` diff. FD apps get an explicit runtimeconfig framework version and, if requested, a `rollForward` policy. R2R output that embeds the framework in its version bubble is invalidated and recompiled. Nothing is upgraded implicitly.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [TS §12.1–12.2, §12.4–12.5](../../Requirements/Transformation_Specification.md) | Runtime patching, explicit/latest/range versions, sources, upgrade safety |
| [TS §23.3](../../Requirements/Transformation_Specification.md), [RQ §6](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Patch phase ordering |
| [TS §29.3](../../Requirements/Transformation_Specification.md), [TS §32.7–32.8](../../Requirements/Transformation_Specification.md) | Pinned resolved versions, determinism, explicit upgrades |
| [RQ §5.2](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Patch runtime versions (8.0.x → 8.0.y), determinism, auditability |
| [CK §9.1](../../Requirements/Read_to_run_Cake.md), [CK §12](../../Requirements/Read_to_run_Cake.md) | SC framework patch replacement (managed + native), diagnostics |
| [Architecture §4, §9, §9.1, §10, §15, §19 item 18](../../Architecture/Tailor.architecture.md) | Phase 4, `Patch.Runtime`, R2R rules, acquisition policies, determinism, `latestPatch` pinning |

## Scope

**In**
- `Patch.Runtime` handler: category `patch.runtime`, phase `Patch`, registered in DI by category.
- Target version resolution: `RuntimeVersionPolicy` `exact` or `latestPatch` (or `range` within the same major), mapped onto a range with the WU-102 `RuntimeVersionPolicy.ToRangeString` and resolved at plan time through Acquisition, then pinned in the plan (version + sha512). No implicit target version. `--offline` uses the highest cached version in range and warns ([Architecture §19 item 18](../../Architecture/Tailor.architecture.md)).
- SC: for each shared framework present (NETCore, WindowsDesktop with `Profile` filtering, AspNetCore), compute the source→target RuntimeList diff and emit actions:
  - `Replace`: same relative path, different `FileVersion`/hash.
  - `Add`: present only in the target list.
  - `Remove`: present only in the source list (catalogued files only).
  - Covers managed and native files, including host components as the WU-006 ADR defines them.
- SC config: contribute `includedFrameworks[].version` (runtimeconfig) and `runtimepack.*` library versions (deps.json) through the WU-801/WU-802 transformers in phase `ConfigGeneration`.
- FD: set `runtimeOptions.framework(s)[].version` to the target version. `rollForward` is written only when the TransformSpec sets it explicitly. All other runtimeconfig members are preserved.
- R2R invalidation: detect app/plugin images whose version bubble includes framework assemblies (`--inputbubble` or composite with the framework, from the input publish). If R2R is selected for them, emit `Optimise` actions for the app/plugin images against the target pack (via WU-702, executed by WU-703); framework assemblies themselves are never recompiled in v1 (architecture §9.1). Otherwise fail planning. Non-bubble R2R images are preserved, with a `Preserve` reason in the plan.
- Guards: cross-major target → error (points to retargeting). Same version → no-op plus info diagnostic. Lower version → error (see Open Questions).
- Diagnostics in `RPK9xxx` (proposed sub-range `RPK90xx`), registered in the Core registry.

**Out**
- Major-version changes and TFM changes (WU-901). FD⇄SC (WU-803/WU-804). Library patching (WU-902).
- Apphost refresh for same-major patches (see Open Questions). Stripping R2R code. Running crossgen2 (WU-703 executes `Optimise` actions).

## Deliverables

- `Patch.Runtime` handler + `Validate` checks + plan contributions with provenance (rule/operation id, handler, phase).
- RuntimeList diff service (or extension of the WU-701 catalogue) returning a sorted, deterministic diff.
- R2R version-bubble facts in `Inspection` (only if WU-200 did not add them): composite component flag, multi-module version bubble flag, manifest assembly list.
- Fixtures: an SC app pinned to an older same-major patch (e.g. `RuntimeFrameworkVersion=8.0.13`) and an R2R input-bubble/composite variant including the framework. Add them to `build/Build-TestApps.ps1` and `manifest.json`.
- Unit, golden-file and integration tests (see Test Requirements).

## Design Notes

- The architecture is the baseline. The WU-006 spike report/ADR (RuntimeList variance, `Profile`, host component sources) and the WU-004 ADR (crossgen2) override it where they differ.
- The RuntimeList catalogue is the only authority for framework ownership. Files that are not in the source list are app-owned and are never touched.
- Keep patch semantics in the handler. The handler contributes config deltas; the WU-801/WU-802 writers produce the files. Do not write JSON directly.
- The TransformSpec shape (`operations.patch.runtime`) is normative from the WU-102 schema. If `rollForward` or `version` members are missing, extend the model and schema using the `schema-change` skill. Regenerate `schemas/` in the same commit.
- R2R validity: an image outside the framework version bubble stays valid across same-major patches. An image whose bubble includes the framework would be rejected or unsafe, so replacing the framework must never leave it unhandled.
- Plan ordering: Acquisition (runtime packs) → Patch (Replace/Add/Remove) → Optimisation (recompile) → ConfigGeneration.

## Acceptance Criteria

- [ ] AC-1 For an SC net8 console fixture patched explicitly from the pinned source patch to a newer 8.0.x in the local feed, the set of `Replace`/`Add`/`Remove` actions equals the RuntimeList diff (test compares sets).
- [ ] AC-2 For an SC WPF fixture, the diff covers `Microsoft.WindowsDesktop.App` filtered by the `WPF` `Profile`. No WinForms-only files are added.
- [ ] AC-3 Every file not listed in the source RuntimeList is copied with an unchanged SHA-256 (test-enforced).
- [ ] AC-4 `latestPatch` resolves to the highest same-major version in the local feed and is pinned in the plan with id, version, source and sha512. Two plan runs are byte-identical.
- [ ] AC-5 `--offline` with only an older patch cached resolves to that version and emits a warning. With nothing cached it fails with exit code 4.
- [ ] AC-6 A target in a different major version (e.g. 10.0.x for a net8 input) fails planning with an `RPK9xxx` error and exit code 1.
- [ ] AC-7 FD patch: the runtimeconfig framework `version` equals the target. `rollForward` is unchanged unless the TransformSpec sets it, and is written exactly when set. The other members are semantically unchanged.
- [ ] AC-8 SC patch: runtimeconfig `includedFrameworks` and deps.json `runtimepack.*` entries carry the target version. A normalised comparison against an SDK publish at the target patch (when that fixture exists) is equal.
- [ ] AC-9 An input-bubble/composite R2R fixture that includes the framework: with R2R selected, the plan contains `Optimise` actions for exactly those images; without R2R selected, planning fails with an `RPK9xxx` error naming the images.
- [ ] AC-10 Non-bubble R2R app assemblies get `Preserve` with a reason and no `Optimise` action.
- [ ] AC-11 No deps.json library other than `runtimepack.*` / framework references changes version, and only runtime packs are acquired (test-enforced).
- [ ] AC-12 `apply` output launches (test-harness smoke run exits 0). The inspector reports the target `FileVersion` for `System.Private.CoreLib.dll`. The output AppSpec validates.
- [ ] AC-13 All new codes are registered with severity and message. The same-version no-op emits an info diagnostic.

## Test Requirements

- xUnit v3 on MTP. Golden files for plans (normalised) and diffs.
- Offline local package feed fixture: a folder feed with two same-major runtime pack versions (NETCore + WindowsDesktop `win-x64`) plus crossgen2, referenced by a test `nuget.config`. It is populated by a build script and CI cache, never committed.
- Integration over `artifacts/testapps/` (pinned-patch SC console, WPF, R2R bubble variant). Launch smoke only in the test harness.
- Record Test Evidence: commands, TRX, feed package list with hashes, and plan hashes of both runs.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=900"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=900"` (integration tests carry `Category=Integration`, `Category=Matrix`, and `Category=Launch` for smoke runs).

## Definition of Done

- All AC ticked by the Verifier. CI green (build, test, format).
- New fixtures are in `Build-TestApps.ps1` and the manifest, and the matrix cache key is updated.
- If this WU changes the architecture (inspector facts, schema members), an ADR is in `Docs/Decisions/` and the architecture is updated in the same PR.
- The plan status is updated. Test Evidence is recorded in the PR.

## Agent Notes

- Read the WU-006 and WU-004 spike reports first. Do not guess host-component package sources.
- Pin the source patch with `RuntimeFrameworkVersion` in the fixture. The default SDK publish always uses the SDK's bundled patch.
- Runtime packs are large. Reuse the CI package cache and keep feed population outside the test run.
- Compare the diff against `RuntimeList.xml` sets, not against directory listings.

## Open Questions

- Should a same-major patch also refresh `apphost.exe` from the target host pack? Proposed default: preserve, with an explicit opt-in.
- Are downgrades (explicit lower patch) allowed? Proposed: error in v1.
- Accepted `rollForward` values and whether `latestPatch` for FD should also imply `rollForward: LatestPatch`. Proposed: never implied.
