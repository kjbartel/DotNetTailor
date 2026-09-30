# WU-005 spike-apphost-deployment-model

| Field | Value |
|---|---|
| ID | WU-005 |
| Title | spike-apphost-deployment-model |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap) |
| Status | Not started |
| Depends on | WU-003, WU-006 |
| Parallel with | WU-004, WU-007, M1, WU-200, WU-201, WU-203 |
| Target paths | `Docs/Spikes/WU-005-spike-apphost-deployment-model.md`, `Docs/Decisions/ADR-0002-apphost-patching-and-deployment-model.md`, `spikes/WU-005/` (throwaway), architecture §9.2, §19 item 12, §21 |
| Size | M |
| Branch | `wu/005-spike-apphost-deployment-model` |

## Goal

Prove that a custom apphost patcher plus runtimeconfig/deps.json rewriting can convert FD⇄SC for net8 and net10 console, WinForms and WPF apps that then launch. Confirm or reject architecture §19 item 12. Feeds WU-202, WU-800–WU-804 and risks R2, R3, R10.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/DotNetRepack.architecture.md#92-apphost) | §9.2 Apphost | Assumptions to confirm |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#9-transformation-handlers) | §9 DeploymentModel row | FD→SC / SC→FD steps |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#74-identities-and-inspection) | §7.4 Apphost binding, bundle marker | Reader facts (WU-202) |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) | §19 item 12 (custom patcher, **Open**) | Decision to confirm |
| [TS](../../Requirements/Transformation_Specification.md) | §13 Deployment Model Transformation | Semantics |
| [RQ](../../Requirements/Repackage_tool_Requirements_v1.1.md) | §5.3 Deployment / Packaging Model Changes | Requirement |
| [CK](../../Requirements/Read_to_run_Cake.md) | §5 Deployment Model Transformations | Historical detail |
| [RD](../../Requirements/R2R_tool_Design.md) | §1.3 Non-Negotiable Principles | "Documented SDK behaviour only" conflict |
| [Plan](../../Plans/DotNetRepack.plan.md#risks-register) | Risks R2, R3, R10; M8 criteria | Consumers |

## Scope

**In**: answer Q1–Q9 with evidence, report, ADR, architecture update, throwaway code.

**Out**: production code, Authenticode re-signing, single-file bundle *creation*, non-Windows hosts.

## Questions to Answer

| # | Question |
|---|---|
| Q1 | Apphost placeholders in `Microsoft.NETCore.App.Host.win-x64` 8.0.x and 10.0.x: the SHA-256("foobar") DLL-path placeholder (offset, count, max length, encoding, terminator); the .NET 9+ app-relative `DOTNET_ROOT` search-location placeholder (bytes, default value, required value for FD and SC). |
| Q2 | Is the apphost in a matrix FD publish byte-identical to the SC publish of the same app/TFM (architecture claim "same apphost serves FD and SC")? If not, what differs? |
| Q3 | Subsystem: PE optional-header subsystem field location; does changing it require a PE checksum update? Confirm SDK sets `WINDOWS_GUI` for WinExe. |
| Q4 | Win32 resources: which resources (icon, version, manifest) the SDK places in the apphost and where they originate; viable copy mechanism (Win32 `BeginUpdateResource`/`UpdateResource` P/Invoke vs managed PE rewrite); is the result deterministic? |
| Q5 | Bundle detection: bundle-header marker/signature for single-file apps in 8 and 10; reliable read-only check for WU-202/WU-800. |
| Q6 | runtimeconfig.json FD vs SC diff (`frameworks` vs `includedFrameworks`, `configProperties`, rollForward) for console/WinForms/WPF, net8/net10. |
| Q7 | deps.json FD vs SC diff (`runtimeTarget` name/signature, `runtimepack.*` libraries, `native`/`runtime` assets, `serviceable`, `sha512`). Does `Microsoft.Extensions.DependencyModel` reader + `DependencyContextWriter` round-trip SDK files losslessly? If not, which JSON-DOM approach is required? |
| Q8 | Launch check: manually convert matrix apps FD→SC (runtime pack files + `hostfxr`/`hostpolicy` + runtimeconfig + deps.json + apphost) and SC→FD (reverse) for console, WinForms, WPF × net8, net10. Do all launch with `--smoke`? Minimum required file set? |
| Q9 | Signing: is the pack's `apphost.exe` Authenticode-signed; is any signature on an input host invalidated by patching (R10 warning basis)? |

## Deliverables

- `Docs/Spikes/WU-005-spike-apphost-deployment-model.md`: Question, Method, Findings (Q1–Q9, each **Answer** + **Evidence**), Decision, Recommended ADR, Impact, Follow-ups.
- `Docs/Decisions/ADR-0002-apphost-patching-and-deployment-model.md` (status `Proposed`): custom patcher vs HostModel, resource-copy mechanism, deps.json/runtimeconfig transformation approach.
- Architecture update in the same PR: [§9.2](../../Architecture/DotNetRepack.architecture.md#92-apphost), §9 DeploymentModel row if the file set changes, [§19 item 12](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) status, the WU-005 row in [§21](../../Architecture/DotNetRepack.architecture.md#21-spikes-feeding-this-document).
- `spikes/WU-005/` throwaway code with its own `Directory.Build.props`/`Directory.Packages.props` (`ManagePackageVersionsCentrally=false`), README with rerun steps. Not in `DotNetRepack.slnx`.

## Design Notes

- Work on copies of matrix variants in a temp folder; never modify `artifacts/testapps/` in place.
- Evidence = offsets, hex excerpts (≤ 64 bytes), JSON diffs, hash tables, launch exit codes. No binaries committed.
- Microsoft.NET.HostModel may be used in spike code only for comparison, not as the recommendation unless the ADR explicitly argues for it (architecture §9.2).
- WinForms/WPF SC conversion needs WindowsDesktop runtime pack files filtered by `Profile`; take the list from the WU-006 findings (WU-006 is a dependency).
- The apphost binding-read algorithm recorded here is consumed by WU-202, the single owner of binding reads (architecture §3.2); WU-800 only creates and patches.

## Acceptance Criteria

- [ ] AC-1 The report exists with Findings for each of Q1–Q9, each with a non-empty **Answer** and **Evidence**.
- [ ] AC-2 Q1 findings state placeholder offsets/lengths for both 8.0.x and 10.0.x packs with package versions and sha512.
- [ ] AC-3 Q8 findings contain a 12-row result table (console/WinForms/WPF × net8/net10 × FD→SC/SC→FD) with launch exit codes; any failure has a root cause.
- [ ] AC-4 Q7 findings state "lossless round-trip: yes/no" with a diff excerpt as evidence.
- [ ] AC-5 `Docs/Decisions/ADR-0002-apphost-patching-and-deployment-model.md` exists, status `Proposed`, sections Context, Decision, Consequences, Alternatives.
- [ ] AC-6 Architecture §9.2, §19 item 12 and the §21 WU-005 row are updated and link the report and ADR.
- [ ] AC-7 `spikes/WU-005/` exists, is not referenced by `DotNetRepack.slnx`, and has a README.
- [ ] AC-8 Solution build (`-warnaserror`), `dotnet test --solution DotNetRepack.slnx -c Release` and format verify still pass; no binaries committed.

## Test Requirements

- No production tests. Spike scripts rerunnable; commands recorded in the report.
- Record a Test Evidence block for AC-8.

## Definition of Done

- All AC ticked by the Verifier; solution unaffected.
- Plan status updated; risks R2, R3, R10 rows updated if mitigations change.

## Agent Notes

- Load: [architecture §7.4, §9, §9.2, §12, §19, §21](../../Architecture/DotNetRepack.architecture.md), [WU-003 spec](WU-003-test-app-suite.spec.md), this spec.
- Launching apps is test-harness activity only (architecture §19 item 21).
- Keep architecture edits to the sections listed; WU-004/006/007 edit others in parallel.

## Open Questions

- **Resolved** — WU-006 as a formal dependency of WU-005: yes (plan updated).
- If Q7 shows DependencyModel cannot round-trip, confirm WU-202/WU-604/WU-802 switch to a JSON-DOM reader/writer.
