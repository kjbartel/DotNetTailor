# WU-004 spike-crossgen2

| Field | Value |
|---|---|
| ID | WU-004 |
| Title | spike-crossgen2 |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) |
| Status | Not started |
| Depends on | WU-003 |
| Parallel with | WU-005, WU-006, WU-007, M1, M2 |
| Target paths | `Docs/Spikes/WU-004-spike-crossgen2.md`, `Docs/Decisions/ADR-0001-crossgen2-acquisition-and-invocation.md`, `spikes/WU-004/` (throwaway), architecture §9.1 and §21 |
| Size | M |
| Branch | `wu/004-spike-crossgen2` |

## Goal

Establish how the tool acquires and invokes crossgen2 for `net8.0` and `net10.0` targets without an SDK, with which arguments, and whether the output is deterministic. The answers feed WU-702/WU-703 and plan risk R1.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/Tailor.architecture.md#91-readytorun-details) | §9.1 ReadyToRun details | Assumptions to confirm |
| [Architecture](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) | §19 item 13 (no SDK at run time) | Must hold for crossgen2 hosting |
| [Architecture](../../Architecture/Tailor.architecture.md#21-spikes-feeding-this-document) | §21 | Spike charter |
| [TS](../../Requirements/Transformation_Specification.md) | §14 Optimisation, §3 Design Principles (determinism) | R2R semantics |
| [RD](../../Requirements/R2R_tool_Design.md) | §8 Ready-to-Run Planning Model | Eligibility, compilation units |
| [CK](../../Requirements/Read_to_run_Cake.md) | §4.3 Determinism, §6.2 R2R generation | Historical detail |
| [Plan](../../Plans/Tailor.plan.md#risks-register) | Risk R1; M7 criteria | Determinism, version matching |

## Scope

**In**: answer Q1–Q8 with evidence, report, ADR, architecture update, throwaway code.

**Out**: production code in `src/`, composite/`--inputbubble` beyond a feasibility check, PGO/MIBC tuning, non-win-x64 hosts.

## Questions to Answer

| # | Question |
|---|---|
| Q1 | Layout of `Microsoft.NETCore.App.Crossgen2.win-x64` for the latest 8.0.x and 10.0.x: contents of `tools/`, `crossgen2.exe` (single-file/self-contained?) vs `crossgen2.dll`, JIT DLLs, bundled `*.mibc`, any other files. |
| Q2 | Hosting: can each version run with only the tool's own .NET 10 runtime and no SDK (architecture §19 item 13)? If a variant needs `dotnet crossgen2.dll`, which runtime/host is required? |
| Q3 | SDK baseline: argument set in the retained `.rsp` files (`artifacts/testapps/_r2r-rsp/`) for FD vs SC, net8 vs net10, console vs WPF. Tabulate every switch and its source (`-r` set, `--targetos`, `--targetarch`, `-O`, `-m`/`--embed-pgo-data`, `--pdb`, `--resilient`, others). |
| Q4 | References: does compiling against implementation assemblies from `Microsoft.NETCore.App.Runtime.win-x64` (+ `Microsoft.WindowsDesktop.App.Runtime.win-x64`) reproduce the SDK R2R output (byte-equal or semantically equal) for FD and SC apps? |
| Q5 | Version compatibility: confirm crossgen2 major must equal target major. Does a crossgen2 patch different from the target runtime patch (e.g. crossgen2 8.0.latest vs app on 8.0.13) produce valid, loadable output? What happens with a mismatched major (exit code/message)? |
| Q6 | Determinism: are outputs byte-identical across two runs, across different working directories, and with `--parallelism` > 1? Same for `--pdb` output. If not, what differs and can semantic comparison replace byte comparison (R1 fallback)? |
| Q7 | Failure modes: exit codes and stderr for mixed-mode input, reference assembly input, already-R2R input, missing reference, architecture mismatch. Which can the planner pre-detect (feeds skip reasons §9.1)? |
| Q8 | Detection: does the `ManagedNativeHeaderDirectory` + `RTR` signature check (architecture §7.4) identify every R2R output from Q4, and distinguish composite components? |

## Deliverables

- `Docs/Spikes/WU-004-spike-crossgen2.md` with sections: Question, Method, Findings (one subsection per Q1–Q8 with **Answer** and **Evidence**), Decision, Recommended ADR, Impact on architecture/plan, Follow-ups.
- `Docs/Decisions/ADR-0001-crossgen2-acquisition-and-invocation.md` (status `Proposed`; context, decision, consequences, alternatives).
- Architecture update in the same PR: [§9.1](../../Architecture/Tailor.architecture.md#91-readytorun-details) (confirmed layout, argument baseline, skip reasons, determinism result) and the WU-004 row in [§21](../../Architecture/Tailor.architecture.md#21-spikes-feeding-this-document) (link report + ADR).
- `spikes/WU-004/` throwaway code (e.g. PowerShell scripts or a console project) with its own `Directory.Build.props` and `Directory.Packages.props` (`ManagePackageVersionsCentrally=false`) so the root props do not apply. Not added to `Tailor.slnx`.

## Design Notes

- Evidence = committed text: command lines, trimmed outputs, file listings, SHA-256 tables, `.rsp` excerpts with machine paths replaced by `<root>`. Never commit packages or binaries.
- Use the matrix from WU-003 (`artifacts/testapps/…-il` inputs, `…-r2r` SDK outputs) as the comparison baseline.
- Start processes with `ArgumentList` and `.rsp` files even in spike code; this is the production pattern (§17).
- If Q6 fails, the report must define the semantic comparison the M7 criterion should use and propose the plan/architecture wording change.

## Acceptance Criteria

- [ ] AC-1 `Docs/Spikes/WU-004-spike-crossgen2.md` exists and contains a Findings subsection for each of Q1–Q8, each with a non-empty **Answer** and **Evidence**.
- [ ] AC-2 The Q3 findings include a switch-by-switch table covering net8/net10 × FD/SC.
- [ ] AC-3 The Q6 findings include a hash table from two runs (same inputs) and state byte-identical: yes/no, with the differing files if no.
- [ ] AC-4 `Docs/Decisions/ADR-0001-crossgen2-acquisition-and-invocation.md` exists with status `Proposed` and sections Context, Decision, Consequences, Alternatives.
- [ ] AC-5 Architecture §9.1 and the §21 WU-004 row reflect the decision and link the report and ADR; §19 item 13 status is updated if Q2 changes it.
- [ ] AC-6 `spikes/WU-004/` exists, is not referenced by `Tailor.slnx` (`Select-String -Path Tailor.slnx -Pattern spikes` returns nothing), and contains a README stating how to rerun the evidence.
- [ ] AC-7 `dotnet build Tailor.slnx -c Release -warnaserror`, `dotnet test --solution Tailor.slnx -c Release` and `dotnet format Tailor.slnx --verify-no-changes` still pass.
- [ ] AC-8 No binaries or `.nupkg` files are committed (`git diff --stat main -- '*.dll' '*.exe' '*.nupkg'` is empty).

## Test Requirements

- No production tests. Spike scripts must be rerunnable; record the commands as evidence in the report.
- Record a Test Evidence block for AC-7.

## Definition of Done

- All AC ticked by the Verifier; solution unaffected (zero warnings, tests green, format clean).
- Plan status updated; risk R1 row updated if the mitigation changes.

## Agent Notes

- Load: [architecture §7.4, §9.1, §17, §19, §21](../../Architecture/Tailor.architecture.md), [WU-003 spec](WU-003-test-app-suite.spec.md) (matrix layout, `_r2r-rsp`), this spec.
- Download packages from nuget.org flat container into a temp folder (`https://api.nuget.org/v3-flatcontainer/<id>/<ver>/<id>.<ver>.nupkg`); record versions and sha512.
- Coordinate architecture edits: WU-005/006/007 edit other sections in parallel — keep edits to §9.1, §19 item 13 and the §21 row.

## Open Questions

- ADR numbering: ADR-0001..0005 are reserved for WU-004..007 by these specs; confirm the convention.
- Should composite/`--inputbubble` (SC only) be verified now or deferred to WU-703?
