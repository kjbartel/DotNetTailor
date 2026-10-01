# WU-007 spike-nuget-and-libraries

| Field | Value |
|---|---|
| ID | WU-007 |
| Title | spike-nuget-and-libraries |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) |
| Status | Not started |
| Depends on | WU-000 |
| Parallel with | WU-001–WU-006, WU-100 |
| Target paths | `Docs/Spikes/WU-007-spike-nuget-and-libraries.md`, `Docs/Decisions/ADR-0004-nuget-acquisition.md`, `Docs/Decisions/ADR-0005-json-schema-validator.md`, `spikes/WU-007/` (throwaway), architecture §6.1, §10, §20, §21 |
| Size | M |
| Branch | `wu/007-spike-nuget-and-libraries` |

## Goal

Prove package acquisition with NuGet.Protocol/NuGet.Configuration (config hierarchy, source mapping, credential providers, offline), and choose the JSON Schema validator and the third-party library baseline on licence and capability grounds. Feeds WU-101/102 (validator), WU-700 (acquisition) and risks R5, R8.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/Tailor.architecture.md#10-acquisition) | §10 Acquisition | Assumptions to confirm |
| [Architecture](../../Architecture/Tailor.architecture.md#61-common-rules) | §6.1 schemas + validator | Validator requirement |
| [Architecture](../../Architecture/Tailor.architecture.md#17-security) | §17 Secrets | Credential handling |
| [Architecture](../../Architecture/Tailor.architecture.md#20-open-questions) | §20 validator library | Decision owner |
| [TS](../../Requirements/Transformation_Specification.md) | §29 External Sources and Credentials, §5 Identity and Schema | Sources, credentials, schema |
| [AS](../../Requirements/Application_Specification.md) | §5 schema/versioning | Schema validation |
| [RQ](../../Requirements/Repackage_tool_Requirements_v1.1.md) | §10 CLI and Distribution, §12 Non-Functional | Dependencies, reproducibility |
| [CK](../../Requirements/Read_to_run_Cake.md) | §9.3 Library updates via NuGet | Historical detail |
| [Plan](../../Plans/Tailor.plan.md#risks-register) | Risks R5, R8; M7 criterion 3 | Consumers |

## Scope

**In**: answer Q1–Q9 with evidence, two ADRs, architecture update, throwaway code.

**Out**: production acquisition (WU-700), schema models (WU-101/102), adding packages to `Directory.Packages.props` (done by the consuming WU).

## Questions to Answer

| # | Question |
|---|---|
| Q1 | Settings: does `Settings.LoadDefaultSettings(root)` reproduce the `dotnet restore` view of the `nuget.config` hierarchy (repo, user, machine), including `globalPackagesFolder` and disabled sources? |
| Q2 | Source mapping: how to apply `PackageSourceMapping` when resolving an id to allowed sources; behaviour when no source matches (diagnostic, exit code 4). |
| Q3 | Download + extract into the global packages folder so the SDK layout is reproduced (`<id>/<version>/`, `.nupkg.metadata`, `.sha512`); API used (`FindPackageByIdResource` + `PackageExtractor` vs `GlobalPackagesFolderUtility`); concurrent-access safety. |
| Q4 | Integrity: read `contentHash` (sha512) from `.nupkg.metadata`; verify it against the downloaded `.nupkg`; is repository-signature verification required or optional? |
| Q5 | Credentials: plug-in credential providers (e.g. Azure Artifacts Credential Provider) via `DefaultCredentialServiceUtility`; non-interactive mode for CI; confirm no secret reaches logs or artefacts (§17). Test against an authenticated local feed (e.g. a local HTTP feed with basic auth) and a folder feed (risk R5). |
| Q6 | Offline: resolve id/version from the global packages folder only; `latestPatch` with `--offline` picks the latest cached version and warns (architecture §19 item 18). |
| Q7 | Version listing for `latestPatch`/`range`: `GetAllVersionsAsync`, prerelease exclusion, unlisted packages, caching (`SourceCacheContext`) and determinism (pin in plan). |
| Q8 | JSON Schema validator: compare JsonSchema.Net, Corvus.JsonSchema, NJsonSchema (and any other viable candidate) on licence (current terms, verified at source), draft 2020-12 support (dialect emitted by `JsonSchemaExporter`), System.Text.Json native, error output with JSON pointer (needed for `TLR1xxx`), performance, maintenance. |
| Q9 | Library baseline: licence and version table for every planned third-party package (NuGet.Protocol, NuGet.Configuration, System.CommandLine, Microsoft.Extensions.DependencyModel, Microsoft.Extensions.FileSystemGlobbing, Microsoft.Extensions.DependencyInjection, xUnit v3, the chosen validator, WU-003's native-asset package). Flag any licence incompatible with an undecided product licence. |

## Deliverables

- `Docs/Spikes/WU-007-spike-nuget-and-libraries.md`: Question, Method, Findings (Q1–Q9, **Answer** + **Evidence**), Decision, Recommended ADRs, Impact, Follow-ups.
- `Docs/Decisions/ADR-0004-nuget-acquisition.md` (status `Proposed`): API surface, cache layout, integrity, credentials, offline.
- `Docs/Decisions/ADR-0005-json-schema-validator.md` (status `Proposed`): chosen library, licence, rejected alternatives.
- Architecture update in the same PR: [§10](../../Architecture/Tailor.architecture.md#10-acquisition), [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules) validator sentence, remove the validator item from [§20](../../Architecture/Tailor.architecture.md#20-open-questions), the WU-007 row in [§21](../../Architecture/Tailor.architecture.md#21-spikes-feeding-this-document).
- `spikes/WU-007/` throwaway code (props isolation as in WU-004), README with rerun steps. Not in `Tailor.slnx`.

## Design Notes

- Never commit credentials, tokens or feed URLs with embedded secrets. Local auth-feed test credentials are generated at run time.
- Licence evidence = link to the licence file at a pinned tag/commit plus the SPDX id. Do not rely on memory of licence terms.
- Validator prototype: validate a small AppSpec-like document against a schema produced by `JsonSchemaExporter` for a sample model, including one invalid document; show the reported JSON pointer.
- Evidence text only; no packages committed.

## Acceptance Criteria

- [ ] AC-1 The report exists with Findings for each of Q1–Q9, each with a non-empty **Answer** and **Evidence**.
- [ ] AC-2 Q3/Q4 evidence shows a package downloaded into a temp global packages folder with `.nupkg.metadata`, and a sha512 match check (plus one deliberate mismatch detected).
- [ ] AC-3 Q5 evidence shows successful restore from an authenticated local feed with no secret in captured logs (grep of logs for the test password returns nothing).
- [ ] AC-4 Q8 findings contain a comparison table (licence SPDX, dialect, STJ-native, JSON pointer errors, maintenance) and a prototype output for an invalid document.
- [ ] AC-5 Q9 findings contain a licence table for every listed package with SPDX id and source link.
- [ ] AC-6 `ADR-0004-nuget-acquisition.md` and `ADR-0005-json-schema-validator.md` exist, status `Proposed`, sections Context, Decision, Consequences, Alternatives.
- [ ] AC-7 Architecture §6.1, §10, §20 and the §21 WU-007 row are updated and link the report and ADRs.
- [ ] AC-8 `spikes/WU-007/` exists, is not referenced by `Tailor.slnx`, has a README; no binaries or secrets committed.
- [ ] AC-9 Solution build (`-warnaserror`), `dotnet test --solution Tailor.slnx -c Release` and format verify still pass.

## Test Requirements

- No production tests. Spike code rerunnable; commands recorded in the report.
- Record a Test Evidence block for AC-9.

## Definition of Done

- All AC ticked by the Verifier; solution unaffected.
- Plan status updated; risks R5 and R8 rows and the plan Open Questions entry for the validator updated if resolved.

## Agent Notes

- Load: [architecture §6.1, §10, §17, §19, §20, §21](../../Architecture/Tailor.architecture.md), this spec.
- Use the `microsoft-code-reference` / `microsoft-docs` skills to confirm NuGet client API signatures instead of guessing.
- Keep architecture edits to the sections listed; WU-004/005/006 edit others in parallel.

## Open Questions

- The product licence is undecided (plan Open Questions); the validator decision may need revisiting once it is chosen.
- Is NuGet repository-signature verification in scope for v1?
