# WU-803 fd-to-sc-handler

| Field | Value |
|---|---|
| ID | WU-803 |
| Title | fd-to-sc-handler |
| Milestone | M8 Deployment Model Conversion (v0.4.0-preview) |
| Status | Not started |
| Depends on | WU-800, WU-801, WU-802, WU-503 |
| Parallel with | WU-603, M7 |
| Target | `src/DotNetRepack.Transforms/DeploymentModel/`, `src/DotNetRepack.Transforms/Configuration/ConfigGenerationHandler.cs`, `tests/DotNetRepack.Transforms.Tests/DeploymentModel/` |
| Size | L |

## Goal

Plan framework-dependent → self-contained conversion for `win-x64`: add runtime pack files (profile-subset for WindowsDesktop), `hostfxr`/`hostpolicy`, a host-pack apphost with original resources, and runtimeconfig/deps.json updates, all with package provenance. Also own the `deploymentModel` handler dispatch and the ConfigGeneration phase handler.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §13](../../Requirements/Transformation_Specification.md#13-deployment-model-transformation) | FD→SC, same-model, target RID, derived actions |
| [TS §19](../../Requirements/Transformation_Specification.md#19-addition-rules) | Runtime packs as sources, deterministic destination, provenance |
| [TS §22.4](../../Requirements/Transformation_Specification.md#22-rule-precedence-and-conflict-resolution), [TS §23](../../Requirements/Transformation_Specification.md#23-transformation-dependencies-and-ordering), [TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials) | Collisions, ordering, pinned versions |
| [CK §5](../../Requirements/Read_to_run_Cake.md#5-deployment-model-transformations) | FD→SC, FD→FD |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Inclusion of shared runtimes |
| [Architecture §9](../../Architecture/DotNetRepack.architecture.md#9-transformation-handlers), [§9.2](../../Architecture/DotNetRepack.architecture.md#92-apphost), [§4](../../Architecture/DotNetRepack.architecture.md#4-processing-pipeline), [§8](../../Architecture/DotNetRepack.architecture.md#8-selectors-precedence-and-actions) | DeploymentModel handler, phases 5 and 8, action model |

## Scope

**In**
- `DeploymentModelHandler : ITransformationHandler` (`Category = "deploymentModel"`, `Phase = DeploymentModel`) dispatching to `IDeploymentModelConversion` strategies by `(source, target)`; FD→FD and SC→SC produce no deployment-model actions.
- `FrameworkDependentToSelfContained` strategy:
  - Target RID from TransformSpec (v1: `win-x64` only, else error).
  - Target runtime version per framework: from `operations.deploymentModel.runtimeVersion` or `operations.patch.runtime.version` (`RuntimeVersionPolicy`: `matchSource`, `latestPatch`, `exact`, `range`), mapped onto a range with the WU-102 `ToRangeString` and resolved/pinned via WU-700. No implicit default: a missing value is a `Validate` error (architecture §10).
  - Frameworks: NETCore always; WindowsDesktop/AspNetCore when referenced by the input runtimeconfig; WindowsDesktop profiles (`WPF`, `WindowsForms`) derived from EAM framework references of app and plugin assemblies.
  - `Add` actions for every catalogue file (WU-701 subset) with source `{id, version, sha512, path}`.
  - App-local conflicts (same destination as a catalogue file): keep the higher `AssemblyVersion`, then `FileVersion`; ties → runtime pack; diagnostic per conflict; losing pack files passed to WU-802 as excluded.
  - Apphost: `Replace` with a host-pack template of the target runtime version via WU-800, binding + subsystem + resources from the original host; if no original host, create one from the AppSpec entry point.
- `ConfigGenerationHandler` (phase 8): emits `ModifyConfig` actions for runtimeconfig (WU-801) and deps.json (WU-802) from the resolved target state of earlier phases.
- Projected AppSpec updates: `execution.deploymentModel = selfContained`, framework contexts, classification of added files (`platformManaged`/`platformNative`/`resource`).

**Out**
- SC→FD strategy (WU-804). Runtime patching (WU-900). Retarget (WU-901). Removing other-RID assets (filtering rules). Launch-level E2E matrix (WU-805).

## Deliverables

- `DeploymentModelHandler`, `IDeploymentModelConversion`, `FrameworkDependentToSelfContained`, `WindowsDesktopProfileResolver`, `FrameworkFileConflictResolver`, `ConfigGenerationHandler`; DI registration.
- Diagnostics (proposed `RPK83xx`): unsupported RID, framework not resolvable, profile undeterminable (fallback all profiles + warning), app-local override kept, bundle input refused (via WU-800), missing apphost and no entry point, missing runtime version or policy.

## Design Notes

- Follow WU-005 (apphost/deployment) and WU-006 (runtime packs) spike reports and ADRs; **they override this spec where they differ** (file set, profile rules, conflict rule, config shapes).
- Shared seams owned here: `DeploymentModelHandler` dispatch, `IDeploymentModelConversion` and `ConfigGenerationHandler`. WU-804 depends on this WU and only registers its strategy.
- Added files land in the app root (catalogue destinations). Filtering/resources rules (phase 6) run after and may remove cultures; the Resources handler must see the added satellites.
- Existing non-composite R2R app assemblies stay valid (no invalidation).
- Order within the phase is planner-derived and recorded; all actions carry handler provenance.

## Acceptance Criteria

- [ ] AC-1 FD→SC plan for matrix console (net8, net10) adds exactly the NETCore catalogue destination set; each `Add` has package provenance `{id, version, sha512, path}`.
- [ ] AC-2 WPF adds NETCore + WindowsDesktop subset `WPF`; WinForms adds subset `WindowsForms`; the resulting runtime file sets equal the SC counterparts in the matrix (WU-701 allowlist applies).
- [ ] AC-3 `hostfxr.dll` and `hostpolicy.dll` are added in the app root.
- [ ] AC-4 The apphost is replaced by the host-pack template of the target runtime version with the original binding, subsystem and resources (verified via WU-800 readers).
- [ ] AC-5 `ModifyConfig` actions for runtimeconfig and deps.json are in phase 8 and their projected content equals WU-801/802 output for the resolved target.
- [ ] AC-6 An app-local override fixture (higher-version framework assembly shipped by the app) keeps the app file, emits a diagnostic, and removes the file from the runtimepack asset list.
- [ ] AC-7 A target RID other than `win-x64` is rejected during validation.
- [ ] AC-8 FD→FD and SC→SC plans contain no deployment-model actions.
- [ ] AC-9 `--offline` with missing runtime/host packs fails planning with an acquisition error.
- [ ] AC-10 The projected AppSpec validates against output assertions `deploymentModel: selfContained`, `rid: win-x64`.
- [ ] AC-11 Two plans are byte-identical.
- [ ] AC-12 `apply` of the net10 console FD→SC plan produces an output that launches (smoke, harness only).
- [ ] AC-13 An FD→SC TransformSpec without `runtimeVersion` and without `patch.runtime` fails validation with the `RPK83xx` missing-runtime-version error; `matchSource`, `latestPatch`, `exact` and `range` each resolve to the expected pinned version on the local feed.

## Test Requirements

- xUnit v3 + golden files in `tests/DotNetRepack.Transforms.Tests/DeploymentModel/`; plan golden files per scenario.
- Matrix inputs under `Category=Matrix`, launch under `Category=Launch`; packs via `LocalPackageFeedFixture`; no network.
- Run: `dotnet test --project tests/DotNetRepack.Transforms.Tests --filter-trait "WU=803"`.
- Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green; `RPK83xx` codes listed for WU-1001.

## Agent Notes

- Handlers only contribute actions; never write files.
- Do not reference Platform.Windows directly; use `IPlatformServices`.

## Open Questions

- **Resolved** — default runtime version for FD→SC: none; an explicit version or policy is required, else validation error (architecture §19 item 33).
- WindowsDesktop profile when references are dynamic (reflection-loaded plugins): include all profiles by default or require TransformSpec input?
- **Resolved** — ConfigGeneration handler owner: this WU; WU-804 depends on it.
