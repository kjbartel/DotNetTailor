# WU-801 runtimeconfig-transformer

| Field | Value |
|---|---|
| ID | WU-801 |
| Title | runtimeconfig-transformer |
| Milestone | M8 Deployment Model Conversion (v0.4.0-preview) |
| Status | Not started |
| Depends on | WU-202, WU-003 |
| Parallel with | M3–M7 |
| Target | `src/Tailor.Transforms/Configuration/RuntimeConfig/`, `tests/Tailor.Transforms.Tests/Configuration/` |
| Size | S |

## Goal

Pure, deterministic transformation of `*.runtimeconfig.json` between FD and SC shapes (`framework`/`frameworks` ⇄ `includedFrameworks`), with framework version and `tfm` updates, preserving `configProperties` and unknown members.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §13.4](../../Requirements/Transformation_Specification.md#13-deployment-model-transformation) | Update configuration |
| [TS §11.4](../../Requirements/Transformation_Specification.md), [TS §12.2](../../Requirements/Transformation_Specification.md#12-patching-specification) | Reused by retarget (WU-901) and FD runtime patch (WU-900) |
| [CK §5](../../Requirements/Read_to_run_Cake.md#5-deployment-model-transformations), [CK §6.1](../../Requirements/Read_to_run_Cake.md#6-target-framework-and-runtime-requirements) | FD⇄SC; update `.runtimeconfig.json` |
| [RQ §5.3](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Deployment model changes |
| [Architecture §9](../../Architecture/Tailor.architecture.md#9-transformation-handlers), [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline) | DeploymentModel handler; ConfigGeneration phase |

## Scope

**In**
- `RuntimeConfigTransformer.Transform(RuntimeConfigDocument, RuntimeConfigTarget) → Result<RuntimeConfigDocument>` and a writer.
- FD→SC: `framework`/`frameworks` → `includedFrameworks` with exact runtime pack versions supplied by the caller.
- SC→FD: `includedFrameworks` → `framework` (single) or `frameworks` (multiple) with caller-supplied versions.
- FD→FD / SC→SC: version and `tfm` updates only.
- `rollForward` handling per the table below; `configProperties`, `additionalProbingPaths` and unknown members preserved.
- Semantic JSON comparer for tests (normalised: property order, framework order by name).

**Out**
- Choosing target versions or frameworks (callers: WU-803/804/900/901). Plan action emission (ConfigGeneration handler, WU-803). deps.json (WU-802).

## Deliverables

- `RuntimeConfigTarget {DeploymentModel, Tfm?, Frameworks[{Name, Version}], RollForward?}`.
- `RuntimeConfigTransformer`, `RuntimeConfigWriter` (UTF-8 no BOM, 2-space indent; shape per SDK).
- `JsonSemanticComparer` test utility (shared with WU-802/805; place in the existing test-support project if one exists).
- Diagnostics (proposed `TLR81xx`): both `framework(s)` and `includedFrameworks` present, missing target version, unknown framework name, invalid JSON.

## Design Notes

- Follow the WU-005 spike report (`Docs/Spikes/WU-005-*`) and ADR; **they override this spec where they differ**, especially SDK output shape (`framework` vs `frameworks`, ordering, `rollForward` in SC).
- Build on the WU-202 reader model; if it is lossy for unknown members, extend it (keep a `JsonObject` for passthrough) rather than writing a second reader.

| Member | FD→SC | SC→FD | Same model |
|---|---|---|---|
| `tfm` | Preserve (or target) | Preserve (or target) | Preserve (or target) |
| `framework(s)` | Removed | Created from target | Versions updated |
| `includedFrameworks` | Created, exact versions | Removed | Versions updated (SC) |
| `rollForward` | Per SDK SC output (spike) | Preserve if present in input metadata, else absent | Preserve |
| `configProperties` | Preserve | Preserve | Preserve |
| Unknown members | Preserve | Preserve | Preserve |

## Acceptance Criteria

- [ ] AC-1 FD→SC on the matrix FD runtimeconfig of console, WinForms and WPF (net8, net10), using the SC counterpart's framework versions, is semantically equal to the SC counterpart's runtimeconfig.
- [ ] AC-2 SC→FD on the matrix SC runtimeconfig, using the FD counterpart's framework versions, is semantically equal to the FD counterpart's runtimeconfig.
- [ ] AC-3 `configProperties` and unknown members (fixture with custom properties) survive both directions unchanged.
- [ ] AC-4 FD output never contains `includedFrameworks`; SC output never contains `framework`/`frameworks`.
- [ ] AC-5 FD→FD with a new version changes only framework versions.
- [ ] AC-6 Invalid inputs (both shapes present, malformed JSON, missing version) yield `TLR81xx` diagnostics without exceptions.
- [ ] AC-7 Output for identical inputs is byte-identical; UTF-8 without BOM.
- [ ] AC-8 `JsonSemanticComparer` has its own tests (order-insensitive objects, framework arrays keyed by name, number/string distinctions kept).

## Test Requirements

- xUnit v3 + golden files in `tests/Tailor.Transforms.Tests/Configuration/`.
- Matrix comparisons (`Category=Matrix`) read `artifacts/testapps`; unit fixtures are small checked-in JSON files.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=801"`.
- No network. Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green; `TLR81xx` codes listed for WU-1001.

## Agent Notes

- Keep the transformer free of Planning/Acquisition calls so WU-900/901 can reuse it.

## Open Questions

- **Resolved** — default FD framework version on SC→FD: none. The TransformSpec must state an explicit version or runtime version policy (`matchSource` = the SC included version); a missing value is a validation error in the calling handler (architecture §10, §19 item 33). This transformer always receives explicit versions.
- **Resolved** — WU-003 matrix dependency: added to the plan.
