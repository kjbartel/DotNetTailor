# .NET Tailor Naming and Identity Plan

## Scope

This plan fixes repository identity, spelling and prose conventions, and tracks package reservation. Work units cite it rather than re-deciding names.

Chosen identity:

| Surface | Value |
| --- | --- |
| Product name | `.NET Tailor` |
| Identifier stem | `Tailor` |
| Repository | `https://github.com/kjbartel/DotNetTailor` |
| Solution | [DotNetTailor.slnx](../../DotNetTailor.slnx) |
| Projects | `src/Tailor.*`, `tests/Tailor.*` |
| Root namespace | `Tailor.*` (derived from `$(MSBuildProjectName)`) |
| Package id | `dotnet-tailor` |
| Tool command | `dotnet-tailor` |
| Sidecar artefacts directory | `.tailor/` |
| Default artefact filenames | `tailor.appspec.json`, `tailor.plan.json`, `tailor.projected.appspec.json`, `tailor.log` |
| Diagnostic code prefix | `TLR` (`TLR0100`, `TLR6201`, …) |
| Environment variable prefix | `DOTNET_TAILOR_` |
| Licence | Apache License 2.0, `Copyright 2026 Kuan Bartel` |
| Spelling | Australian English |

### Applicability

Identity is fixed before the first package release; this plan introduces no compatibility or migration obligations. CLI commands are scaffold stubs; implementation belongs to [the master plan](Tailor.plan.md).

### Goals

- Keep identity and Australian English consistent across docs, code and human-facing output.
- Use descriptive commands and optional, unambiguous tailoring metaphors in prose.
- Reserve the package id before WU-1002 publishes.

### Non-goals

- Behaviour, schemas and diagnostic numbering: owned by their WUs; `AppSpec` and `TransformSpec` follow [the requirements](../Requirements/Application_Specification.md).
- Release workflows and publishing: WU-1002.

## Current state

| Item | State | Evidence |
| --- | --- | --- |
| Solution, project and namespace naming | Settled | [DotNetTailor.slnx](../../DotNetTailor.slnx); all projects are `Tailor.*` |
| Package and tool metadata | Settled | [src/Tailor.Cli/Tailor.Cli.csproj](../../src/Tailor.Cli/Tailor.Cli.csproj) sets `PackageId`, `ToolCommandName`, licence, authors, copyright and repository URLs |
| Licence | Settled | [LICENSE](../../LICENSE) is the unmodified Apache 2.0 text; [README.md](../../README.md) states it |
| Documentation naming | Settled | Architecture, master plan and specs use the identity table |
| CLI command surface | Stubs registered, alias policy settled | [RootCommandFactory.cs](../../src/Tailor.Cli/RootCommandFactory.cs); every verb returns `ExitCodes.NotImplemented` |
| Package id availability on NuGet | `dotnet-tailor` available, not yet reserved | Checked 2026-10-01; no package published under that id |
| Release and ownership configuration | Absent by design | No `.github/workflows` directory |

### Observed CLI inventory

Taken from [RootCommandFactory.cs](../../src/Tailor.Cli/RootCommandFactory.cs); all actions are stubs.

| Command | Arguments and options | Aliases |
| --- | --- | --- |
| `analyse <appDir>` | `--spec-out` | `analyze` |
| `validate <appDir>` | `--spec` (required) | none |
| `plan <appDir>` | `--spec`, `--transform` (required), `--out-plan` | none |
| `apply <appDir>` | `--spec`, `--transform`, `--output` (required), `--dry-run`, `--spec-out`, `--symbols-output` | none |
| `inspect <appDir> [view]` | `--spec` (required), `--plugin` | none |
| `schema export [kind]` | `--output` | none |
| Global | `--verbosity`, `--strict`, `--permissive`, `--artefacts`, `--offline`, `--var`, `--version` | `--artifacts` for `--artefacts` |

## Naming policy

1. **Australian English is canonical.** It applies to documentation, spec prose, internal identifiers, comments, diagnostic messages, option descriptions and generated human-facing text.
2. **US spellings are permanent, non-deprecated CLI aliases.** Register both spellings wherever they differ; Australian names appear first in help, docs and code. Current aliases: `analyze` for `analyse`, `--artifacts` for `--artefacts`.
3. **Machine-readable identifiers follow their requirements and owning WU.** Preserve specified wire spellings; use Australian English in prose. This covers JSON properties, schema `$id`/kinds and diagnostic codes.
4. **Commands stay descriptive:** `analyse`, `validate`, `plan`, `apply`, `inspect`. Changing a verb requires an ADR; tailoring metaphors are not command names.
5. **Metaphors are optional prose, never machine-readable identifiers.** Clarity takes precedence. See [C# conventions](../../.github/instructions/csharp.instructions.md) and [documentation conventions](../../.github/instructions/docs-specs.instructions.md).
6. **Repository-owned identifiers use the identity table.** Diagnostic ranges keep their allocated digits and use the `TLR` prefix.
7. **One identity source.** New work units cite this plan for naming rather than restating it.

## Remaining Steps

1. [ ] **Reserve `dotnet-tailor` before WU-1002 publishes.** Recheck availability immediately before publishing and record the date. If unavailable, choose a fallback `PackageId`, retain `ToolCommandName=dotnet-tailor`, and update [CLI metadata](../../src/Tailor.Cli/Tailor.Cli.csproj) and [README](../../README.md).
2. [x] **Specify permanent aliases and Australian-first help.** [WU-105](../Specs/M1/WU-105-cli-skeleton.spec.md) defines the policy and AC-1/AC-2 cover help and alias equivalence.
3. [x] **Align architecture, plan and specs with the identity table.** Requirements remain unchanged; external identifiers such as `--artifacts-path` retain their spelling.
4. [x] **Specify release identity without creating workflows.** [WU-1002](../Specs/M10/WU-1002-packaging-and-release-workflow.spec.md) records metadata, artefact and cache naming.
5. [x] **Assign naming coverage.** WU-105 owns parsing/help; WU-1002 AC-1/AC-3 own package metadata and local-feed install smoke tests.

## Validation

Run from the repository root:

```powershell
dotnet build DotNetTailor.slnx -c Release -warnaserror
dotnet test --solution DotNetTailor.slnx -c Release
dotnet format DotNetTailor.slnx --verify-no-changes
```

For this plan specifically:

- Repository-owned product, package, command and URL identifiers agree with the identity table.
- Prose and source comments use Australian English; aliases and requirement/external wire identifiers retain their specified spelling.
- [src/Tailor.Cli/Tailor.Cli.csproj](../../src/Tailor.Cli/Tailor.Cli.csproj) metadata, [LICENSE](../../LICENSE) and [README.md](../../README.md) agree on licence, copyright, repository URL, package id and tool command.

## Risks

- **Package reservation:** another publisher may take the unreserved id; complete step 1 before release.
- **Alias omissions:** every differing spelling needs both forms and WU-105 coverage.
- **Naming drift:** check new specs against the identity table.
- **Clarity:** metaphors must not obscure facts; wire identifiers must retain their specified spelling.

## Acceptance Criteria

- [x] The product, repository, solution, project, namespace, package and tool-command names are chosen and recorded in this plan.
- [x] The scaffold matches the chosen names: solution, `src/Tailor.*`, `tests/Tailor.*`, namespaces, package id and tool command.
- [x] Apache License 2.0 and `Copyright 2026 Kuan Bartel` are present in the repository and in CLI package metadata.
- [x] Repository-owned product, package and tool-command identifiers use the chosen identity.
- [x] The naming policy is stated with rationale: Australian English canonical, permanent US CLI aliases, descriptive command names, tailoring metaphors allowed in prose only.
- [x] `dotnet-tailor` is confirmed available on NuGet as of 2026-10-01.
- [x] Agent-facing guidance for metaphor use and for comment style exists in `.github/instructions/`.
- [ ] `dotnet-tailor` is reserved or published, or a fallback package id is recorded.
- [x] The permanent US-alias rule is recorded in the WU-105 spec.
- [x] Specs, architecture and master plan name commands, options, artefacts and the package consistently with this plan.
- [x] WU-105 and WU-1002 own the naming-related test coverage in their own specs.

## Test Evidence

Historical naming-sweep results; identifier-search arguments are omitted, so that check is not reusable as exact-command evidence.

- **State**: naming sweep across `Docs/**` (excluding `Docs/Requirements/`), `.github/**`, `.gitignore`, `src/Tailor.Cli/CliDiagnostics.cs`, `tests/Tailor.Cli.Tests/ScaffoldTests.cs`; docs included: yes; exclusions: `Docs/Requirements/**` (authoritative, never edited during implementation).
- **Environment**: Windows; .NET 10 SDK pinned by [global.json](../../global.json); no network.
- **Impact**: one production source file changed (diagnostic code constants); all other edits are documentation. No behaviour depends on the constants yet — every CLI verb is a stub.
- **Selected checks**: full solution build with warnings as errors, full test suite, format verification, and `git grep` for the retired identifiers.
- **Excluded checks**: none; the suite is small enough to run whole.
- **Command**: `dotnet build DotNetTailor.slnx -c Release -warnaserror`, `dotnet test --solution DotNetTailor.slnx -c Release`, `dotnet format DotNetTailor.slnx --verify-no-changes`, all in the repository root. Historical identifier-search arguments are omitted.
- **Result**: build succeeded; 33 passed, 0 failed, 0 skipped; format clean; identifier search passed with requirement and history exclusions.
- **Evidence source**: run by this agent.
- **Rerun reason**: none.

## Open Questions

- When should `dotnet-tailor` be reserved on NuGet: now with a placeholder publish, or at the WU-1002 release?
- Does project policy require per-file copyright headers, or is repository, package and document level licensing sufficient?
- Does any content under `Docs/Requirements/` carry third-party attribution that repository-level Apache-2.0 does not cover?
