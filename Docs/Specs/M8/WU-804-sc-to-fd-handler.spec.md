# WU-804 sc-to-fd-handler

| Field | Value |
|---|---|
| ID | WU-804 |
| Title | sc-to-fd-handler |
| Milestone | M8 Deployment Model Conversion (v0.4.0-preview) |
| Status | Not started |
| Depends on | WU-801, WU-802, WU-503, WU-803 |
| Parallel with | WU-603, M7 |
| Target | `src/Tailor.Transforms/DeploymentModel/SelfContainedToFrameworkDependent.cs`, `tests/Tailor.Transforms.Tests/DeploymentModel/` |
| Size | M |

## Goal

Plan self-contained → framework-dependent conversion: remove only files owned by the included frameworks' RuntimeList catalogue, keep app-local overrides and app-owned natives with diagnostics, enforce dependency safety, and update runtimeconfig/deps.json.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §13](../../Requirements/Transformation_Specification.md#13-deployment-model-transformation) | SC→FD, derived removals |
| [TS §20](../../Requirements/Transformation_Specification.md#20-removal-rules) | Semantic removal, dependency safety |
| [TS §24.4](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies), [TS §29.3](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials) | Incompatible dependencies are errors; pinned catalogue identity |
| [CK §5](../../Requirements/Read_to_run_Cake.md#5-deployment-model-transformations) | SC→FD |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Removal of shared runtimes |
| [Architecture §9](../../Architecture/Tailor.architecture.md#9-transformation-handlers), [§10](../../Architecture/Tailor.architecture.md#10-acquisition), [§19 item 10](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) | RuntimeList is authoritative for ownership |

## Scope

**In**
- `SelfContainedToFrameworkDependent : IDeploymentModelConversion` registered with the WU-803 dispatcher.
- Catalogue: acquire runtime packs matching the input runtimeconfig `includedFrameworks` exact versions (WU-700/701).
- Removal rule: a file is removed only if (a) its path is in the catalogue, (b) deps.json attributes it to a `runtimepack` library (or no package library claims it), and (c) its `AssemblyVersion`/`FileVersion` match the catalogue entry. Otherwise it is kept with a diagnostic (app-local override).
- Removes `hostfxr.dll`, `hostpolicy.dll` and framework satellites under culture folders.
- Keeps the original apphost unmodified (no Authenticode invalidation).
- Dependency safety: every retained managed assembly's framework references resolve against the target shared frameworks; a reference to a removed file not provided by a target framework is an error.
- Composite R2R input that embeds framework code → error.
- `ModifyConfig` inputs for WU-801 (`frameworks` with target versions) and WU-802 (remove `runtimepack.*`).
- Projected AppSpec: `execution.deploymentModel = frameworkDependent`, framework contexts updated.

**Out**
- Handler dispatch and ConfigGeneration handler (WU-803). Runtime patching (WU-900). E2E launch matrix (WU-805).

## Deliverables

- `SelfContainedToFrameworkDependent`, `FrameworkOwnershipEvaluator`; DI registration.
- Diagnostics (proposed `RPK84xx`): catalogue unavailable, app-local override kept, dependency-unsafe removal, composite framework image, `includedFrameworks` missing.

## Design Notes

- Follow WU-005 and WU-006 spike reports and ADRs; **they override this spec where they differ**.
- Use the WU-803 seams (`DeploymentModelHandler`, `IDeploymentModelConversion`, `ConfigGenerationHandler`); WU-803 is a dependency.
- Without a catalogue no file is removed: `--offline` cache miss is an acquisition error (exit 4), never a heuristic fallback.
- Target FD framework versions: from the TransformSpec runtime version or policy (`matchSource` = the input `includedFrameworks` version). No implicit default; a missing value is a `Validate` error (architecture §10).
- `--inputbubble` (non-composite) outputs are not reliably detectable; see Open Questions.

## Acceptance Criteria

- [ ] AC-1 For matrix SC console, WinForms and WPF (net8, net10), every `Remove` action targets a catalogue-owned path (test enumerates all `Remove` actions).
- [ ] AC-2 The retained file set equals the matrix FD counterpart's file set (documented allowlist only).
- [ ] AC-3 An app-owned native DLL fixture (name not in the catalogue) is preserved.
- [ ] AC-4 An app-local override fixture (catalogue name, different version, package-attributed in deps.json) is preserved with an `RPK84xx` diagnostic.
- [ ] AC-5 The apphost is not modified (no action touches it).
- [ ] AC-6 A retained assembly referencing a framework not in the target set (e.g. WindowsDesktop removed while the target lists only NETCore) fails planning with a dependency-safety error.
- [ ] AC-7 A composite R2R SC input fails planning with an `RPK84xx` error.
- [ ] AC-8 `--offline` without the matching runtime pack in cache fails with an acquisition error and plans no removals.
- [ ] AC-9 The projected AppSpec validates against `deploymentModel: frameworkDependent`.
- [ ] AC-10 Two plans are byte-identical.
- [ ] AC-11 An SC→FD TransformSpec without a runtime version or policy fails validation; with `matchSource` the FD runtimeconfig carries the input `includedFrameworks` versions.

## Test Requirements

- xUnit v3 + golden files in `tests/Tailor.Transforms.Tests/DeploymentModel/`.
- Matrix inputs under `Category=Matrix`; fixtures derived from SC outputs with added native DLL / override assembly; packs via `LocalPackageFeedFixture`; no network.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=804"`.
- Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green; `RPK84xx` codes listed for WU-1001.

## Agent Notes

- Prefer keeping a file over removing it when evidence is incomplete; always explain via a diagnostic.

## Open Questions

- How to detect `--inputbubble` R2R app assemblies compiled against the bundled framework (invalid after SC→FD)? Warn only, or require a TransformSpec acknowledgement?
- **Resolved** — WU-803/WU-804 shared seams: WU-804 now depends on WU-803, which owns the dispatcher and ConfigGeneration handler.
