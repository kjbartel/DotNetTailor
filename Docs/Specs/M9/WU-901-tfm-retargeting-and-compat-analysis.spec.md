# WU-901 tfm-retargeting-and-compat-analysis

| Field | Value |
|---|---|
| ID | WU-901 |
| Title | tfm-retargeting-and-compat-analysis |
| Milestone | M9 Retargeting & Patching → v0.5.0-preview |
| Status | Not started |
| Depends on | WU-801, WU-802, WU-701, WU-503, WU-201, WU-800, WU-702 |
| Parallel with | WU-703, WU-704, WU-803–WU-805, WU-900, WU-902 |
| Target project(s)/paths | `src/DotNetRepack.Transforms/` (`Retarget` handler, compatibility analyser), `src/DotNetRepack.Acquisition/` (known-breaking-API data, if placed there), `src/DotNetRepack.Specifications/` (compatibility policy, only if missing), `tests/DotNetRepack.Transforms.Tests/`, `tests/DotNetRepack.IntegrationTests/`, `tests/TestApps/` (BinaryFormatter fixture) |
| Size | L |

## Goal

Implement the `Retarget` handler. It changes the TFM (e.g. `net8.0` → `net10.0`, `net8.0-windows` → `net10.0-windows`) for FD and SC apps. It updates runtimeconfig and deps.json, replaces SC framework files, and refreshes the apphost. Before any action is emitted, a compatibility analysis checks the app against the target runtime pack. Incompatibilities fail planning unless an explicit compatibility policy allows them.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [TS §11.1–11.5](../../Requirements/Transformation_Specification.md) | Retarget target state, scope, derived actions, compatibility failure |
| [TS §23.3–23.4](../../Requirements/Transformation_Specification.md), [RQ §6](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Retarget phase before patch/deployment. Impossible combinations fail |
| [TS §24.2](../../Requirements/Transformation_Specification.md) | "Version compatibility uncertain" policy |
| [TS §28](../../Requirements/Transformation_Specification.md), [TS §29.3](../../Requirements/Transformation_Specification.md) | `${targetTfm}` variables, pinned versions |
| [RQ §5.1](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §11](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Retarget + compatibility validation; target runtime independent of tool runtime |
| [CK §9.2](../../Requirements/Read_to_run_Cake.md), [CK §6.1](../../Requirements/Read_to_run_Cake.md) | TFM retargeting for FD and SC, required assemblies present |
| [Architecture §4, §9, §9.1, §9.2, §10](../../Architecture/DotNetRepack.architecture.md) | Phase 3, `Retarget` handler, crossgen2 major = target major, apphost, catalogue |
| [Plan risk R6](../../Plans/DotNetRepack.plan.md) | Undetectable breaking changes: document the limits |

## Scope

**In**
- `Retarget` handler: category `retarget`, phase `Retarget`. Whole-application retarget only (every framework context).
- Target resolution: TFM (with optional `-windows` platform suffix) → shared frameworks (NETCore, WindowsDesktop, AspNetCore as referenced) → runtime version from an explicit `RuntimeVersionPolicy` (`exact`, `latestPatch`, `range`; `latestPatch` relative to the target major.minor), mapped with the WU-102 `RuntimeVersionPolicy.ToRangeString`, pinned in the plan. No implicit version: a retarget without a runtime version or policy is a validation error (architecture §10).
- Config: runtimeconfig `tfm`, `framework(s)`/`includedFrameworks` versions; deps.json `runtimeTarget` name (`.NETCoreApp,Version=vX.Y[/win-x64]`) and `runtimepack.*` entries. Contributed through WU-801/WU-802 in phase `ConfigGeneration`.
- SC: framework replacement = remove source-major RuntimeList files, add target-major RuntimeList files. App-owned files are untouched. Apphost refreshed from the target `Microsoft.NETCore.App.Host.win-x64` via WU-800, keeping binding, subsystem and Win32 resources.
- FD: the apphost is also refreshed from the target host pack (placeholder formats differ between majors, plan risk R2).
- Compatibility analysis (plan time, read-only):
  - Scan `TypeRef`/`MemberRef` (plus `TypeSpec`/`MethodSpec` parents) of every app/plugin managed assembly against target runtime pack **implementation** assemblies (WU-201 MemberRef scan API).
  - Report missing types, missing members (signature-matched) and missing forwarded types.
  - Report "known removed/throwing APIs" from a versioned data file (e.g. `BinaryFormatter` serialisation, which exists in net9+ but always throws).
  - Report app library assemblies whose `TargetFramework` is newer than the target TFM.
- Compatibility policy (TransformSpec): default `error`. `warning` or `skip` must be set explicitly. The `--strict` mapping applies (warning → exit 3).
- R2R: images compiled against the source runtime major are recompiled with the target-major crossgen2 when R2R is selected (via WU-702). Otherwise they are preserved with an `RPK9xxx` warning (stale R2R code).
- Diagnostics in `RPK9xxx` (proposed sub-range `RPK91xx`), each with assembly, referencing member and reason.

**Out**
- Partial retarget (selected plugins/components, [TS §11.3](../../Requirements/Transformation_Specification.md)). Rejected with an explicit "not supported in v1" error.
- Downgrading TFMs. Library upgrades to better-matching assets (WU-902). Same-major runtime patching (WU-900).
- Behavioural break detection beyond metadata (documented limitation, R6).

## Deliverables

- `Retarget` handler, target-state resolver and compatibility analyser with result model (`CompatibilityFinding {assembly, kind, symbol, reason}`).
- Known-breaking-API data file (embedded resource, keyed by target major, sorted), with a documented update procedure.
- `tests/TestApps/` fixture referencing `BinaryFormatter.Serialize`, and a synthetic fixture referencing a member absent from net10 (built with `MetadataBuilder`/a reference-only stub). Both added to `Build-TestApps.ps1`.
- Tests (see below). Detection limits (R6) are documented in the user guides by WU-1003.

## Design Notes

- The architecture is the baseline. Spike reports/ADRs (WU-004 crossgen2 majors, WU-005 apphost placeholders, WU-006 RuntimeList) override it where they differ.
- The analysis uses implementation assemblies from `runtimes/win-x64/lib/<tfm>/` of the target runtime pack, never reference packs. Resolve forwarders transitively.
- Deterministic output: sort findings by assembly, then by symbol, ordinal.
- Retarget runs in phase 3. WU-900/WU-803 in later phases must see the retargeted target state (`ResolveTargetState` exposes one resolved TFM + runtime version to all handlers).
- A retarget combined with `patch.runtime` resolves to one runtime version. Conflicting majors are an impossible combination ([TS §23.4](../../Requirements/Transformation_Specification.md)) → error.
- The compatibility policy member follows the WU-102 schema. If absent, add it with the `schema-change` skill.

## Acceptance Criteria

- [ ] AC-1 net8.0 FD console → `net10.0`: runtimeconfig `tfm` and framework version and deps.json `runtimeTarget` are normalised-equal to the matrix net10 FD counterpart. App library entries are unchanged.
- [ ] AC-2 net8.0-windows FD WPF and WinForms → `net10.0-windows`: the `Microsoft.WindowsDesktop.App` reference and version are updated. The outputs launch (harness).
- [ ] AC-3 net8.0 SC console and WPF → net10.0 SC: the removed/added framework files equal the source/target RuntimeList sets (with `Profile` filtering). App-owned files keep their hashes. The apphost is from the target host pack with preserved subsystem, icon and version resources. The outputs launch.
- [ ] AC-4 The BinaryFormatter fixture retargeted to net10 fails planning with exit code 1 and an `RPK9xxx` error naming the assembly and the API. With compatibility policy `warning`, the plan succeeds with the warning. With `--strict`, the exit code is 3.
- [ ] AC-5 The missing-member fixture yields a "missing member" finding with the declaring type and signature.
- [ ] AC-6 An assembly whose `TargetFramework` is newer than the target TFM yields an error.
- [ ] AC-7 An R2R input (matrix `R2R on`) with R2R selected gets `Optimise` actions using crossgen2 of the target major (recorded in the plan). Without R2R selected, each image is preserved with an `RPK9xxx` warning.
- [ ] AC-8 Partial-scope retarget and TFM downgrade are rejected with distinct errors before planning.
- [ ] AC-9 The runtime version is pinned in the plan. Two plan runs are byte-identical. Compatibility findings are sorted deterministically.
- [ ] AC-10 No library other than framework/runtimepack entries changes version (test-enforced).

## Test Requirements

- xUnit v3 on MTP. Golden files for compatibility reports and normalised plans.
- Offline local package feed fixture with net8 + net10 runtime packs, host packs and crossgen2 packs (`win-x64`). Populated by script/CI cache.
- Integration over `artifacts/testapps/` net8 FD/SC console, WinForms and WPF. Launch smoke only in the harness.
- Unit tests for the analyser against small synthetic metadata (no network).
- Record Test Evidence: commands, TRX, feed contents and plan hashes.
- Run: `dotnet test --project tests/DotNetRepack.Transforms.Tests --filter-trait "WU=901"`; `dotnet test --project tests/DotNetRepack.IntegrationTests --filter-trait "WU=901"` (integration tests carry `Category=Integration`, `Category=Matrix`, and `Category=Launch` for smoke runs).

## Definition of Done

- All AC verified. CI green.
- Fixtures added to the matrix and manifest. The known-breaking-API data file has an update procedure in its header comment or the WU PR description.
- An ADR is recorded if the handler dependency set or schema changes the architecture.
- The plan status is updated. Test Evidence is recorded.

## Agent Notes

- All dependencies (including WU-503, WU-201, WU-800 and WU-702) are in the plan's dependency column.
- Use WU-201's MemberRef scan API. Do not re-implement metadata walking.
- Keep the analyser pure (inputs: assemblies + target implementation set). This makes it unit-testable without packages.

## Open Questions

- **Resolved** — plan dependency gap: WU-503, WU-201, WU-800 and WU-702 added to the plan.
- Policy member name and values (`compatibility: error|warning|skip`) and whether it can be scoped per assembly selector.
- Content and ownership of the known-breaking-API list beyond BinaryFormatter (source: official breaking-change docs).
- Whether partial retarget ([TS §11.3](../../Requirements/Transformation_Specification.md)) is required for v1.
