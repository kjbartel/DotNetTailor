# WU-006 spike-runtime-packs

| Field | Value |
|---|---|
| ID | WU-006 |
| Title | spike-runtime-packs |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap) |
| Status | Not started |
| Depends on | WU-000 |
| Parallel with | WU-001–WU-004, WU-007, WU-100 |
| Target paths | `Docs/Spikes/WU-006-spike-runtime-packs.md`, `Docs/Decisions/ADR-0003-runtime-pack-catalogue.md`, `spikes/WU-006/` (throwaway), architecture §10, §7.5, §21 |
| Size | M |
| Branch | `wu/006-spike-runtime-packs` |

## Goal

Determine whether `data/RuntimeList.xml` in the runtime packs is a complete, reliable catalogue for framework ownership, patch diffs, `Profile` filtering and SC→FD removal. Feeds WU-701, WU-802–WU-804, WU-900 and risk R4.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/DotNetRepack.architecture.md#10-acquisition) | §10 Acquisition (RuntimeList catalogue) | Assumption to confirm |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#75-reference-resolution) | §7.5 framework catalogue | Framework reference resolution |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#9-transformation-handlers) | §9 Patch.Runtime, DeploymentModel rows | Diff and removal semantics |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) | §19 items 10, 15 | Catalogue, AspNetCore support |
| [TS](../../Requirements/Transformation_Specification.md) | §12 Patching, §13 Deployment Model, §29 External Sources | Semantics |
| [CK](../../Requirements/Read_to_run_Cake.md) | §9.1 Framework patch replacement | Historical detail |
| [RD](../../Requirements/R2R_tool_Design.md) | §9.1 Framework Patching | Historical detail |
| [Plan](../../Plans/DotNetRepack.plan.md#risks-register) | Risk R4; M8/M9 criteria | Consumers |

## Scope

**In**: answer Q1–Q8 for `Microsoft.NETCore.App.Runtime.win-x64`, `Microsoft.WindowsDesktop.App.Runtime.win-x64`, `Microsoft.AspNetCore.App.Runtime.win-x64` at 8.0.13, 8.0.25 (or the latest 8.0.x if 8.0.25 does not exist) and the latest 10.0.x; report, ADR, architecture update, throwaway code.

**Out**: production catalogue code (WU-701), acquisition code (WU-700), non-win-x64 packs.

## Questions to Answer

| # | Question |
|---|---|
| Q1 | `RuntimeList.xml` schema per pack and version: all element/attribute names and value sets (`Type`, `Path`, `AssemblyVersion`, `FileVersion`, `Culture`, `Profile`, `DropFromSingleFile`, others). Differences between 8 and 10? |
| Q2 | Completeness: files in the pack's `runtimes/win-x64/{lib,native}` that are missing from RuntimeList and vice versa. Are `hostfxr.dll`/`hostpolicy.dll` listed, and in which pack? |
| Q3 | Patch diff 8.0.13 → 8.0.25 per pack: added/removed/changed files (by hash), which change `FileVersion` vs `AssemblyVersion`. Is RuntimeList alone sufficient to compute the replace set, or are file hashes needed? |
| Q4 | Major diff 8.0.x → 10.0.x: added/removed assemblies (input for retarget compatibility, WU-901). |
| Q5 | WindowsDesktop `Profile`: values present (`WindowsForms`, `WPF`, others, none); how the SDK maps `UseWPF`/`UseWindowsForms` to the published file set. Does a Profile-filtered list equal the matrix SC WinForms/WPF publish output (if WU-003 is available) or a spike-local SC publish? |
| Q6 | SC publish vs catalogue: files in an SDK SC publish that come from the runtime packs but are not in RuntimeList (and the reverse). Can SC→FD remove exactly the catalogued set without touching app-owned natives? |
| Q7 | Satellite/culture entries in WindowsDesktop (localized WPF/WinForms resources): how listed, and interplay with resource culture filtering. |
| Q8 | Version discovery: how to list available patch versions (nuget flat-container `index.json`) for `latestPatch`; prerelease/unlisted handling; what must be pinned in the plan. |

## Deliverables

- `Docs/Spikes/WU-006-spike-runtime-packs.md`: Question, Method, Findings (Q1–Q8, **Answer** + **Evidence**), Decision, Recommended ADR, Impact, Follow-ups. Diff tables summarised (counts + representative rows); full diffs as text files under `spikes/WU-006/evidence/` if large.
- `Docs/Decisions/ADR-0003-runtime-pack-catalogue.md` (status `Proposed`): catalogue source of truth, Profile handling, patch-diff algorithm (RuntimeList vs hashes).
- Architecture update in the same PR: [§10](../../Architecture/DotNetRepack.architecture.md#10-acquisition) catalogue paragraph, §9 Patch.Runtime/DeploymentModel rows if semantics change, the WU-006 row in [§21](../../Architecture/DotNetRepack.architecture.md#21-spikes-feeding-this-document).
- `spikes/WU-006/` throwaway code (props isolation as in WU-004), README with rerun steps. Not in `DotNetRepack.slnx`.

## Design Notes

- WU-006 does not depend on WU-003. For Q5/Q6, compare against the matrix if it exists; otherwise publish a minimal SC WinForms/WPF app inside `spikes/WU-006/` and say so in the evidence.
- Record each package's id, version and sha512 in the report.
- Evidence text only; do not commit packages or extracted binaries. Keep committed evidence files small (< 1 MB total).

## Acceptance Criteria

- [ ] AC-1 The report exists with Findings for each of Q1–Q8, each with a non-empty **Answer** and **Evidence**.
- [ ] AC-2 Q1 findings include an attribute table covering all three packs for 8.0.x and 10.0.x.
- [ ] AC-3 Q3 findings include added/removed/changed counts per pack for 8.0.13 → 8.0.25 (or the stated substitute version).
- [ ] AC-4 Q5 findings state "Profile-filtered list equals SDK SC output: yes/no" for WinForms and WPF with the diff as evidence.
- [ ] AC-5 `Docs/Decisions/ADR-0003-runtime-pack-catalogue.md` exists, status `Proposed`, sections Context, Decision, Consequences, Alternatives.
- [ ] AC-6 Architecture §10 and the §21 WU-006 row are updated and link the report and ADR.
- [ ] AC-7 `spikes/WU-006/` exists, is not referenced by `DotNetRepack.slnx`, has a README; no binaries committed.
- [ ] AC-8 Solution build (`-warnaserror`), `dotnet test --solution DotNetRepack.slnx -c Release` and format verify still pass.

## Test Requirements

- No production tests. Spike scripts rerunnable; commands recorded in the report.
- Record a Test Evidence block for AC-8.

## Definition of Done

- All AC ticked by the Verifier; solution unaffected.
- Plan status updated; risk R4 row updated if the mitigation changes.

## Agent Notes

- Load: [architecture §7.5, §9, §10, §19, §21](../../Architecture/DotNetRepack.architecture.md), this spec.
- Keep architecture edits to the sections listed; WU-004/005/007 edit others in parallel.

## Open Questions

- Confirm the patch versions to compare: 8.0.13 → 8.0.25 as in architecture §21, or 8.0.13 → latest 8.0.x.
- Should AspNetCore coverage stay catalogue-only given architecture §19 item 15 is still open?
