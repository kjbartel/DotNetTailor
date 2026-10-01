# WU-1000 tool-config-and-precedence

| Field | Value |
|---|---|
| ID | WU-1000 |
| Title | tool-config-and-precedence |
| Milestone | M10 Configuration, Hardening & Release → v1.0.0 |
| Status | Not started |
| Depends on | WU-105 |
| Parallel with | M2–M9 |
| Target project(s)/paths | `src/Tailor.Cli/` (config discovery, merge, binding), `src/Tailor.Core/` (settings model, if shared), `schemas/config/v1/` (generated schema), `tests/Tailor.Cli.Tests/`, `Docs/Guides/configuration.md`, `Docs/Architecture/Tailor.architecture.md` §14 + ADR |
| Size | M |

## Goal

Add a tool configuration file at repo and user level. Merge all configuration sources with one documented, deterministic precedence: **CLI (response files expanded inline) > environment variables > repo config > user config > defaults** ([Architecture §14](../../Architecture/Tailor.architecture.md#14-cli)). Cover global options, strict/permissive mode and default tool-package version policies.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [CK §3.2](../../Requirements/Read_to_run_Cake.md) | CLI, configuration file and response file, mergeable with deterministic precedence |
| [CK §12](../../Requirements/Read_to_run_Cake.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Configurable error handling (fail-fast / warn-and-continue) |
| [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md) | dotnet CLI conventions, automation/CI suitability |
| [TS §29.2](../../Requirements/Transformation_Specification.md) | No credentials in configuration documents |
| [Architecture §13, §14, §19 item 6](../../Architecture/Tailor.architecture.md) | `--strict`/`--permissive`, global options, config file (M10) |

## Scope

**In**
- Config file name: `dotnet-tailor.json` ([Architecture §14](../../Architecture/Tailor.architecture.md)). JSON, comments and trailing commas tolerated, `$schema` + `schemaVersion`.
- Discovery:
  - Repo config: from the current directory upwards; the first file found wins (no multi-file merge between directories). Discovery stops at the git root (the first directory containing `.git`); outside a git repository it stops at the filesystem root.
  - User config: `%APPDATA%\dotnet-tailor\dotnet-tailor.json` via `IPlatformKnowledge` (no hard-coded Windows path outside Platform).
  - `--config <file>` replaces repo discovery. `--no-config` disables repo and user config.
- Settings (v1): `verbosity`, `mode` (`strict|permissive|default`), `artifacts`, `offline`, `vars` (map, merged per key), `defaults.toolPackages` (version for crossgen2/host packs; allowed values `matchTarget` — the tool-internal rule "follow the resolved target runtime version", architecture §10 — or an exact version). This is not a TransformSpec runtime version policy.
- Environment variables: `DOTNET_TAILOR_VERBOSITY`, `DOTNET_TAILOR_MODE`, `DOTNET_TAILOR_ARTIFACTS`, `DOTNET_TAILOR_OFFLINE`, `DOTNET_TAILOR_CONFIG`, `DOTNET_TAILOR_VAR_<name>`.
- Precedence per setting: CLI tokens (with `@file` response files expanded inline; one layer, later tokens win for scalars, as System.CommandLine parses them) > environment > repo config > user config > built-in defaults. Scalars: the highest source wins. `vars`: merged per key with the same order.
- Response files are part of the CLI layer; no custom origin tagging.
- Mode conflicts: `--strict` and `--permissive` at the same level → exit 2. A higher level overrides a lower one.
- Validation: unknown keys → warning (`--strict` → error); invalid values → `TLR0xxx` error with file + JSON pointer, exit 2. Keys that look like credentials (`password`, `apiKey`, `token`, …) → error.
- Effective-settings log at `--verbosity diagnostic`: each setting with its source (never secrets).
- Generated JSON Schema under `schemas/config/v1/`, covered by the schema drift test.
- User guide `Docs/Guides/configuration.md`: file format, locations, precedence table, env vars, examples.

**Out**
- Package sources or credentials in the config (use `nuget.config`). TransformSpec/AppSpec defaults (those stay in specs). A `config` verb for editing.

## Deliverables

- Settings model + loader + merger (pure, unit-testable) and CLI binding.
- Schema file, drift test, guide, ADR `Docs/Decisions/ADR-NNNN-tool-config-precedence.md` confirming architecture §14 (already updated by the reconciliation pass).

## Design Notes

- [Architecture §14](../../Architecture/Tailor.architecture.md) is normative for the file name, layers and discovery boundary (§19 item 31). The ADR records the implementation details only.
- The merge must be a pure function `(sources[]) → EffectiveSettings + provenance`. Test it table-driven.
- Configuration never changes transformation intent. It affects only tool behaviour and defaults the TransformSpec leaves open. The explicit-upgrade invariant ([TS §32.8](../../Requirements/Transformation_Specification.md)) is unaffected.
- Config paths (`artifacts`) are resolved relative to the file that defines them.

## Acceptance Criteria

- [ ] AC-1 A table-driven test covers each setting from every one of the five sources (CLI incl. response files, environment, repo, user, defaults), and the highest-precedence source wins in every combination.
- [ ] AC-2 `vars` from env, repo and user config merge per key. A CLI `--var` overrides the same key from all other sources.
- [ ] AC-3 Options from an `@file` response file behave exactly like the same tokens typed inline at that position (same layer; later token wins for scalars).
- [ ] AC-4 Repo config is found from a nested working directory; a `dotnet-tailor.json` above the git root is ignored. `--config` and `DOTNET_TAILOR_CONFIG` select an explicit file. `--no-config` ignores both files.
- [ ] AC-5 An invalid config value returns exit 2 with an `TLR0xxx` diagnostic including the file path and JSON pointer. An unknown key warns, and fails under `--strict`.
- [ ] AC-6 A credential-like key is rejected. No config value marked sensitive appears in logs or artefacts.
- [ ] AC-7 `mode: strict` in config makes warnings exit 3. `--permissive` on the CLI overrides it. `--strict --permissive` together exit 2.
- [ ] AC-8 The committed `schemas/config/v1` schema equals the generated one (drift test).
- [ ] AC-9 `Docs/Guides/configuration.md` lists every setting, env var and the precedence table. A test checks that every settings-model member is mentioned in the guide.
- [ ] AC-10 The ADR exists and architecture §14 matches the implemented precedence.

## Test Requirements

- xUnit v3 on MTP. Use temp directories for config discovery and an injected environment/user-profile abstraction. Do not mutate process environment variables in parallel tests.
- CLI-level tests via the in-process command invocation from WU-105.
- Run: `dotnet test --project tests/Tailor.Cli.Tests --filter-trait "WU=1000"`.
- Record Test Evidence: commands and TRX.

## Definition of Done

- All AC verified. CI green. Schema committed. Guide, ADR and architecture updated.
- The plan status is updated. Test Evidence is recorded.

## Agent Notes

- Use System.CommandLine's built-in response-file expansion from WU-105 unchanged; no origin tagging is needed.
- Keep environment access behind an interface so tests stay parallel-safe.

## Open Questions

- **Resolved** — file name: `dotnet-tailor.json` (architecture §14; may change with the final product name, see plan open questions).
- **Resolved** — repo discovery stops at the git root.
- Is `defaults.toolPackages` needed in v1, or should tool packages always follow the resolved target runtime version (`matchTarget`)?
