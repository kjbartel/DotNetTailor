# WU-802 depsjson-transformer

| Field | Value |
|---|---|
| ID | WU-802 |
| Title | depsjson-transformer |
| Milestone | M8 Deployment Model Conversion (v0.4.0-preview) |
| Status | Not started |
| Depends on | WU-202, WU-701, WU-003, WU-604 |
| Parallel with | WU-603, M7, WU-800, WU-801 |
| Target | `src/Tailor.Transforms/Configuration/DepsJson/`, `tests/Tailor.Transforms.Tests/Configuration/` |
| Size | M |

## Goal

Pure, deterministic transformation of `*.deps.json` between FD and SC: set the `runtimeTarget` RID suffix, add or remove `runtimepack.*` libraries built from the runtime-pack catalogue, and preserve project/package/reference libraries exactly.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §13.4](../../Requirements/Transformation_Specification.md#13-deployment-model-transformation) | Update configuration |
| [TS §12.3](../../Requirements/Transformation_Specification.md#12-patching-specification), [TS §12.5](../../Requirements/Transformation_Specification.md#12-patching-specification) | Reused by library patching (WU-902); no unrequested changes |
| [CK §5](../../Requirements/Read_to_run_Cake.md#5-deployment-model-transformations) | FD⇄SC |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Deployment model changes |
| [Architecture §9](../../Architecture/Tailor.architecture.md#9-transformation-handlers), [§10](../../Architecture/Tailor.architecture.md#10-acquisition) | `runtimeTarget` `/win-x64`, `runtimepack.*` libraries, catalogue |

## Scope

**In**
- Read/write with Microsoft.Extensions.DependencyModel v10, building on the WU-604 `DepsJsonWriter` and asset pruner (extend, do not fork); semantic round-trip guarantee.
- FD→SC: `runtimeTarget.name` `.NETCoreApp,Version=vN.0` → `.NETCoreApp,Version=vN.0/<rid>`; add `runtimepack.<Framework>.Runtime.<rid>/<version>` libraries (`type: runtimepack`) with `runtime` (assemblyVersion, fileVersion) and `native` (fileVersion) assets from the WU-701 catalogue (profile-subset).
- SC→FD: remove `runtimepack.*` libraries and their target entries; `runtimeTarget` RID suffix per target (see Open Questions).
- Exclusion of catalogue files that lost an app-local conflict (caller supplies the set).
- Preserve all `project`, `package`, `reference` libraries (sha512, path, hashPath, serviceable, dependencies, runtimeTargets), `compilationOptions`.
- Semantic comparer extension for deps.json (reuse `JsonSemanticComparer` from WU-801).

**Out**
- Choosing versions/frameworks/profiles (WU-803/804). Flattening RID-specific package assets (the tool preserves them; SDK-equivalence tests normalise instead, WU-805). Library upgrades (WU-902). Asset pruning after filtering (WU-604).

## Deliverables

- `DepsJsonTarget {DeploymentModel, Rid?, RuntimePacks[RuntimePackCatalogue subset], ExcludedPackFiles[]}`.
- `DepsJsonTransformer.Transform(DependencyContext, DepsJsonTarget) → Result<DependencyContext>`, `DepsJsonWriter`.
- Diagnostics (proposed `TLR82xx`): malformed deps.json, unknown `runtimeTarget`, existing `runtimepack` library conflicts with requested one, package library would be modified.

## Design Notes

- Follow the WU-005 spike report and ADR (and WU-006 for catalogue data); **they override this spec where they differ** (exact SDK shape of `runtimepack` entries, resources in runtime packs, RID suffix rules).
- Round-trip is semantic, not byte-level; unknown/lost members found during the round-trip test must be documented and handled (passthrough via `JsonNode` if the DependencyModel writer drops them).
- Output ordering follows SDK conventions where known; otherwise sorted ordinal for determinism.

## Acceptance Criteria

- [ ] AC-1 Read → write → read over every matrix deps.json is semantically lossless (documented exceptions only).
- [ ] AC-2 FD→SC on matrix FD console, WinForms and WPF (net8, net10) deps.json is semantically equal to the SC counterpart's deps.json after the WU-805 normalisation (RID-specific package asset flattening, property ordering). Until WU-805 lands, compare apps without RID-specific package assets.
- [ ] AC-3 SC→FD on matrix SC deps.json is semantically equal to the FD counterpart's deps.json (same normalisation rule).
- [ ] AC-4 `runtimepack.*` library asset lists equal the WU-701 catalogue subset used (managed → `runtime`, native → `native`) with matching versions.
- [ ] AC-5 Project, package and reference libraries are byte-semantically unchanged in both directions (test compares each library object).
- [ ] AC-6 A file listed in `ExcludedPackFiles` does not appear in any `runtimepack` asset list.
- [ ] AC-7 Malformed input yields `TLR82xx` diagnostics without exceptions.
- [ ] AC-8 Identical inputs produce byte-identical output.

## Test Requirements

- xUnit v3 + golden files in `tests/Tailor.Transforms.Tests/Configuration/`.
- Matrix comparisons under `Category=Matrix`; runtime packs via `LocalPackageFeedFixture`; no network.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=802"`.
- Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green; `TLR82xx` codes listed for WU-1001.

## Agent Notes

- Do not touch package library entries; any need to do so belongs to WU-902.

## Open Questions

- **Resolved** — RID-specific asset flattening vs SDK SC output: the tool preserves assets; SDK-equivalence comparisons use the normalisation defined in WU-805 (architecture §19 item 35).
- SC→FD `runtimeTarget`: portable (no RID) or RID-specific when the TransformSpec names a `rid`?
- **Resolved** — WU-003 matrix dependency: added to the plan (with WU-604).
