# WU-702 r2r-eligibility-planner

| Field | Value |
|---|---|
| ID | WU-702 |
| Title | r2r-eligibility-planner |
| Milestone | M7 Acquisition & ReadyToRun (v0.3.0-preview) |
| Status | Not started |
| Depends on | WU-503, WU-200, WU-701 |
| Parallel with | WU-504, WU-505, M6 |
| Target | `src/Tailor.Transforms/Optimisation/ReadyToRun/`, `tests/Tailor.Transforms.Tests/` |
| Size | M |

## Goal

Implement the `optimisation.readyToRun` handler's planning half: determine R2R eligibility for every managed assembly in the projected output, explain each decision, build compilation units with fully resolved references, declare the crossgen2 acquisition and emit `Optimise` actions. Planning only; no process execution.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §14](../../Requirements/Transformation_Specification.md#14-optimisation-specification) | R2R scope (§14.3), eligibility at planning (§14.4), failure policy (§14.5) |
| [TS §8](../../Requirements/Transformation_Specification.md#8-selectors), [TS §24](../../Requirements/Transformation_Specification.md#24-validation-and-failure-policies), [TS §25.2](../../Requirements/Transformation_Specification.md#25-output-state-requirements) | Selectors, policies, required optimisation state |
| [RD §8](../../Requirements/R2R_tool_Design.md) | Eligibility states, compilation units, no plugin co-compilation |
| [CK §6.2](../../Requirements/Read_to_run_Cake.md#6-target-framework-and-runtime-requirements) | crossgen2, `win-x64` (R2R optional per architecture §19 item 14) |
| [RQ §5.4](../../Requirements/Repackage_tool_Requirements_v1.1.md#5-supported-transformation-categories) | Optimisation must not change semantics |
| [Architecture §8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions), [§9.1](../../Architecture/Tailor.architecture.md#91-readytorun-details), [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline) | Handler contract, phase 7, R2R rules |

## Scope

**In**
- `ReadyToRunHandler : ITransformationHandler` (`Category = "optimisation.readyToRun"`, `Phase = Optimisation`): `Validate` + `ContributeAsync`.
- Selection via the selector engine over the **projected** state after phases 3–6 (retarget, patch, deployment model, filtering).
- Eligibility per managed assembly (table below) with reason codes.
- Compilation units: target assembly, resolved references, reference roots, target TFM, target RID, options.
- Crossgen2 acquisition request `Microsoft.NETCore.App.Crossgen2.<hostRid>` whose major equals the target runtime major; host RID from `IPlatformKnowledge`.
- `AcquisitionPlannerAdapter : Planning.IAcquisitionPlanner` over WU-700 (architecture §3.2); registered in the Cli by WU-704.
- Optimisation failure policy recorded on each unit.
- Projected AppSpec: planned assemblies marked R2R so output assertions (TS §25.2) and post-execution validation can check them.
- Eligibility report section in the plan covering every managed assembly.

**Out**
- crossgen2 invocation and failure handling at execution (WU-703). Invalidating R2R after runtime patch (WU-900). Stripping existing R2R code (future). PGO/CPU-specific options beyond pass-through flags.

## Eligibility Model

| State | Reason code | Condition |
|---|---|---|
| Ineligible | `MixedMode` | No `ILOnly` flag |
| Ineligible | `ReferenceAssembly` | `ReferenceAssemblyAttribute` present |
| Ineligible | `SatelliteResource` | Satellite resource assembly |
| Ineligible | `CompositeComponent` | Component of a composite R2R image |
| Ineligible | `ArchitectureMismatch` | Not usable on x64 (e.g. `Requires32Bit`, non-AMD64 machine for non-AnyCPU) |
| Skipped | `AlreadyReadyToRun` | App/plugin assembly already has an R2R header |
| Skipped | `FrameworkAlreadyReadyToRun` | Runtime-pack framework assembly (already R2R). Never recompiled in v1, even when a selector names it explicitly (warning `TLR72xx`, then skipped) |
| Skipped | `NotSelected` | Excluded by selector or not matched |
| Planned | — | Eligible and selected |

Every managed assembly in the projected output gets exactly one entry. Non-managed files are not listed.

## Deliverables

- `ReadyToRunHandler`, `R2REligibilityEvaluator`, `R2RCompilationUnitBuilder`.
- Plan model additions: `R2REligibilityEntry {Path, State, Reason?, Detail?}`, `R2RCompilationUnit {Id, TargetAssembly, Output, References[], ReferenceRoots[], TargetTfm, TargetRid, Options, Crossgen2 {Id, Version}, FailurePolicy}`, `R2ROptions {Optimise, Pdb, Mibc, EmbedPgoData, Composite, InputBubble}` (schema regenerated per the `schema-change` skill).
- DI registration of the handler by category.
- Diagnostics (proposed `TLR72xx`): composite/inputbubble requested for FD, selector matches nothing (policy-driven), unresolved reference in unit, crossgen2 version unavailable, explicit selection of an already-R2R assembly.

## Design Notes

- Follow the WU-004 spike report (`Docs/Spikes/WU-004-*`) and ADR; they **override this spec where they differ** (option set, mibc location, determinism caveats).
- References: EAM reference closure of the assembly's folder context (ordered roots, architecture §7.5) **plus all managed implementation assemblies of the target runtime pack(s)** from WU-701 — for FD apps too. Resolve against projected (post-retarget/patch) versions, never the input versions.
- Plugins: one unit per assembly; a plugin unit may reference upstream plugins (plugin graph, WU-304) but never downstream ones; no unit contains more than one input assembly unless `composite` is requested (SC only).
- `--composite` and `--inputbubble` are allowed only when the target deployment model is SC (architecture §9.1); otherwise `Validate` returns an error. They never include framework assemblies in v1.
- Failure policy mapping (TS §14.5, §24.3): `error` → fail apply; `warning` → preserve IL + warning; `skip`/`preserve` → preserve IL + info. Default from `defaults.policies.optimisationFailure`.
- Crossgen2 version: the tool-internal `matchTarget` rule (range `[target runtime version]`, architecture §10), unless a tool default overrides it; resolved and pinned via WU-700; acquisition declared in phase 2 through the planner's acquisition mechanism.
- Machine-dependent settings (`--parallelism`, absolute paths) are not in the plan; WU-703 supplies them.
- Planning stays side-effect-free apart from the declared acquisition.

## Acceptance Criteria

- [ ] AC-1 On eligibility fixtures (IL-only, mixed-mode, reference assembly, satellite, composite component, x86-only, already-R2R app assembly, runtime-pack assembly, unselected), each assembly gets the state and reason in the Eligibility Model table (golden file).
- [ ] AC-2 For every matrix app, every managed assembly in the projected output has exactly one eligibility entry (coverage test).
- [ ] AC-3 Each planned unit's references include the implementation assemblies (incl. `System.Private.CoreLib.dll`) of the **target** runtime pack version, for FD and SC targets.
- [ ] AC-4 For the plugin test app, a plugin unit references its upstream plugins and the host, never a downstream plugin, and contains a single input assembly.
- [ ] AC-5 `composite` or `inputBubble` with an FD target fails validation with an `TLR72xx` error.
- [ ] AC-6 The crossgen2 acquisition request uses `Microsoft.NETCore.App.Crossgen2.<hostRid>` with major = target runtime major (net8 → 8.x, net10 → 10.x), pinned in the plan.
- [ ] AC-7 An R2R selector that matches nothing applies the configured policy (error/warning/skip).
- [ ] AC-8 Every unit records the effective failure policy.
- [ ] AC-9 The projected AppSpec marks planned assemblies as R2R; an output assertion requiring R2R on a skipped assembly fails planning.
- [ ] AC-10 Planning performs no filesystem writes outside the package cache/`--artefacts` (before/after snapshot test).
- [ ] AC-11 Two plans for the same inputs are byte-identical.
- [ ] AC-12 A selector that explicitly names a runtime-pack framework assembly yields a warning and `Skipped(FrameworkAlreadyReadyToRun)`; no `Optimise` action targets a framework assembly.

## Test Requirements

- xUnit v3 + golden files in `tests/Tailor.Transforms.Tests/`; plan golden files per scenario.
- Eligibility fixtures from `artifacts/testapps` (`Category=Matrix`) plus small checked-in or build-time generated assemblies for edge cases.
- Runtime packs and crossgen2 packages via `LocalPackageFeedFixture` (offline); no network in default runs.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=702"`.
- Record Test Evidence in the PR.

## Definition of Done

- All AC ticked by the Verifier; CI green; plan schema regenerated and committed with no drift.
- `TLR72xx` codes listed for WU-1001.

## Agent Notes

- Reuse WU-200 inspection facts; do not re-read PE headers.
- Do not execute crossgen2 or copy files; only emit `Optimise` actions with `dependsOn` on the actions producing their inputs.

## Open Questions

- **Resolved** — explicit selection of framework assemblies: never recompiled in v1 (warning + skip); composite/inputbubble exclude framework assemblies (architecture §9.1, §19 item 32).
- **Resolved** — `SatelliteResource` (Ineligible) and runtime-pack framework (Skipped) are now in architecture §9.1.
- Should `--pdb` (R2R native PDB) be tied to the symbols policy (TS §16) or be an explicit R2R option only?
