# WU-1001 diagnostics-catalogue

| Field | Value |
|---|---|
| ID | WU-1001 |
| Title | diagnostics-catalogue |
| Milestone | M10 Configuration, Hardening & Release → v1.0.0 |
| Status | Not started |
| Depends on | WU-603 |
| Parallel with | M7–M9 |
| Target project(s)/paths | `src/Tailor.Core/` (diagnostic descriptor registry), `src/Tailor.Cli/` (help text), `Docs/Guides/diagnostics.md` (generated), `tests/Tailor.Core.Tests/`, `tests/Tailor.Cli.Tests/` |
| Size | M |

## Goal

Every `TLR` code has one registry descriptor and a user-facing entry in `Docs/Guides/diagnostics.md`. Tests keep the registry, the source code and the docs in sync. Every CLI command and option has complete help text.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | dotnet CLI conventions, clear error reporting, auditability |
| [CK §12](../../Requirements/Read_to_run_Cake.md) | Failures clearly reported, meaningful exit codes |
| [TS §24](../../Requirements/Transformation_Specification.md) | Policy-configurable vs structural (non-downgradable) conditions |
| [Architecture §13, §14](../../Architecture/Tailor.architecture.md) | Code ranges, diagnostic shape, policy mapping, exit codes, CLI verbs |

## Scope

**In**
- Descriptor registry (extend the WU-100 `Diagnostic`/codes design): `code`, `title`, `defaultSeverity`, `category` (range), `messageFormat`, `description`, `remedy`, `IsStructural` and `IsPolicyConfigurable` (from WU-100; [TS §24.4](../../Requirements/Transformation_Specification.md) structural conditions are never policy-configurable), `exitCode` mapping where relevant.
- Migrate all existing codes (M1–M9) into the registry. No emission site may build a code from a string literal.
- Generator: renders `Docs/Guides/diagnostics.md` from the registry (grouped by range, sorted by code, plus an exit-code table). Output is canonical (LF, no timestamps).
- Parity tests:
  - Registry ↔ committed markdown: regenerated content equals the committed file.
  - Source ↔ registry: no `TLR\d{4}` literal outside the registry. Every registered code is referenced by at least one emission site or marked `reserved`.
  - Range check: each code is inside its project's range ([Architecture §13](../../Architecture/Tailor.architecture.md)). Codes are unique.
- Help text completeness: every command, subcommand, option and argument has a non-empty description. `--help` output per verb is compared against golden files. Help mentions exit codes (root help) and `analyse` as an alias.

**Out**
- Localisation of messages. An online docs site. `helpUri` hosting (may be added later).

## Deliverables

- Registry + descriptors for all codes, generator (test-invoked or small `build/` script), committed `Docs/Guides/diagnostics.md`, parity and help tests.

## Design Notes

- Choose one generation path and document it in the guide header ("generated, do not edit; run `<command>`"). Preferred: a golden-file-style drift test (same conventions as `Tailor.Testing.Golden`) that fails on drift, writes a `.received` file for acceptance and rewrites the guide under `DOTNET_TAILOR_UPDATE_GOLDEN=1`.
- Keep descriptors in the owning projects if the architecture's dependency rules require it (e.g. `Transforms` codes). A Core-level registry discovers them through an assembly-scan-free, explicit registration list so that ordering stays deterministic.
- WUs that finish after this one must add descriptors + docs. The parity test enforces it.

## Acceptance Criteria

- [ ] AC-1 Every code emitted anywhere in `src/` is a registered descriptor. A test fails on an unregistered `TLR` literal.
- [ ] AC-2 `Docs/Guides/diagnostics.md` equals the generator output. Changing any descriptor without regenerating fails a test.
- [ ] AC-3 Each entry shows code, title, default severity, whether policy can change it, description and remedy. Entries are grouped by range and sorted.
- [ ] AC-4 Codes are unique and each falls in the range for its category. The test fails on violations.
- [ ] AC-5 Every CLI command/option/argument has a non-empty description (reflection over the command tree). `--help` golden files exist for the root and every verb.
- [ ] AC-6 The guide contains the exit-code table from [Architecture §13](../../Architecture/Tailor.architecture.md), and root `--help` references it.

## Test Requirements

- xUnit v3 on MTP, golden files for the markdown and help text. No network or test apps needed.
- Run: `dotnet test --project tests/Tailor.Core.Tests --filter-trait "WU=1001"`; `dotnet test --project tests/Tailor.Cli.Tests --filter-trait "WU=1001"`.
- Record Test Evidence: commands and TRX.

## Definition of Done

- All AC verified. CI green. Guide committed.
- `AGENTS.md`/work-unit instructions mention that new codes need a descriptor (a one-line addition is allowed if the WU-001 files exist).
- The plan status is updated. Test Evidence is recorded.

## Agent Notes

- Search for `"TLR` in `src/` first to inventory codes. Expect ad-hoc codes from earlier WUs.
- Do not renumber released codes (v0.x previews). Mark retired codes `obsolete` instead.

## Open Questions

- Should the guide link to a stable `helpUri` per code (depends on the `$schema`/docs hosting open question)?
