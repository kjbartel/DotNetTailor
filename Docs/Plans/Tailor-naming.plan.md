# .NET Tailor Naming and Identity Plan

## Scope

This plan records the selection of the product, repository, project and package names for this repository, and the remaining refinement needed in the specifications that depend on those names. It replaces the earlier migration-style rename plan, which assumed a shipped product.

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

### Repository state this plan assumes

The repository is a scaffold. [src/Tailor.Cli](../../src/Tailor.Cli) holds the only production source; every other `src/Tailor.*` project is an empty stub, and every `tests/Tailor.*` project holds scaffold tests. No AppSpec, TransformSpec, plan document, schema, golden file or generated artefact exists yet. No package has been published, no workflow exists under `.github/workflows`, and nothing outside this repository depends on any of these names.

Consequently this plan carries **no migration obligations**: no compatibility ledger, no deprecation lifecycle, no schema migration, no package-transition guidance and no rollback strategy for a published identity. Names are chosen once and written into the scaffold and the specs.

### Goals

- Record `.NET Tailor` / `Tailor` / `dotnet-tailor` as the settled identity so work units stop re-deciding it.
- Keep Australian English canonical across documentation, internal names, comments, diagnostics and generated human-facing text.
- Set the house voice: descriptive command names, tailoring metaphors in prose where they stay unambiguous.
- Resolve the remaining naming questions downstream work units need: artefact naming and package reservation.

### Non-goals

- Implementing any analysis, validation, planning, transformation or platform behaviour. Those belong to their work units in [the master plan](Tailor.plan.md).
- Inventing schema identifiers, diagnostic codes or artefact formats. `Application Specification`, `Transformation Specification`, `AppSpec` and `TransformSpec` come from [the requirements](../Requirements/Application_Specification.md) and are owned by WU-101 and WU-102.
- Creating release workflows or publishing a package. That is WU-1002.

## Current state

| Item | State | Evidence |
| --- | --- | --- |
| Solution, project and namespace naming | Settled | [DotNetTailor.slnx](../../DotNetTailor.slnx); all projects are `Tailor.*` |
| Package and tool metadata | Settled | [src/Tailor.Cli/Tailor.Cli.csproj](../../src/Tailor.Cli/Tailor.Cli.csproj) sets `PackageId`, `ToolCommandName`, licence, authors, copyright and repository URLs |
| Licence | Settled | [LICENSE](../../LICENSE) is the unmodified Apache 2.0 text; [README.md](../../README.md) states it |
| Documentation naming | Settled | A tracked-file search finds no old-name reference outside this plan |
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
2. **US spellings are permanent CLI aliases**, never the primary form in help, docs or code. Every command and option whose Australian spelling differs from the US one registers both; the Australian form is canonical and listed first. Today that is `analyze` for `analyse` and `--artifacts` for `--artefacts`. The aliases are not deprecated and are not scheduled for removal: they let scripts, shell completion and other tools find the command with whichever spelling an author reaches for first.
3. **Machine-readable identifiers follow their source requirement.** JSON property names, schema `$id` values, schema kinds and diagnostic codes are decided by the work unit that introduces them, not by this plan. Where a requirement document fixes a US spelling in a wire format, that spelling is kept and the Australian form is used only in prose.
4. **Command names stay descriptive.** The earlier proposal to adopt tailoring metaphors as command names (`measure`, `pattern`, `cut`, `stitch`) is **rejected**: as commands the metaphor obscures the operation and collides with ordinary verbs, and the existing `analyse`/`validate`/`plan`/`apply`/`inspect` set already matches the requirement vocabulary and the architecture pipeline. Any future proposal to change a verb needs an ADR.
5. **Tailoring metaphors are welcome in prose.** Progress messages, documentation, spec and code comments may use the sewing and tailoring metaphor to keep the tool's voice light, in the same spirit as the baking metaphors in Copilot's own messages. The rule is clarity first: never let a metaphor stand in for the fact a reader needs, never use a metaphor in a machine-readable field, and drop it whenever the plain word is clearer. Guidance for agents is in [the C# conventions](../../.github/instructions/csharp.instructions.md) and [the documentation conventions](../../.github/instructions/docs-specs.instructions.md).
6. **Repository-owned identifiers carry the `Tailor` stem.** The sidecar directory, default artefact filenames, diagnostic code prefix and environment variable prefix in the identity table replaced the old product's `repack` directory and filename stem, its `RPK` diagnostic prefix and its `DOTNET_REPACK_` environment prefix. Diagnostic numbering is unchanged: only the three-letter prefix differs, so every range reserved in a WU spec keeps its digits.
7. **One identity source.** New work units cite this plan for naming rather than restating it.

## Remaining Steps

1. [ ] **Reserve `dotnet-tailor` on NuGet.** The id was confirmed available on 2026-10-01 but is not reserved; nothing holds it until the first publish. Reserve or publish a placeholder before WU-1002 ships, and re-check availability immediately before publishing. If the id is lost, choose a fallback `PackageId`, keep `ToolCommandName` as `dotnet-tailor`, and update [src/Tailor.Cli/Tailor.Cli.csproj](../../src/Tailor.Cli/Tailor.Cli.csproj) and [README.md](../../README.md).
   Validation: the reservation or first publish is recorded here with its date.

2. [x] **Fold the permanent US-alias policy into the WU-105 spec.** Policy item 2 settles the decision: US spellings are permanent, non-deprecated aliases. Record the rule, the Australian-first help ordering and the "register both spellings whenever they differ" obligation for future options in [Docs/Specs/M1/WU-105-cli-skeleton.spec.md](../Specs/M1/WU-105-cli-skeleton.spec.md) so later work units do not re-decide per option.
   Evidence: WU-105 gained a `### Spelling and aliases` design section stating the rule and the obligation on later WUs. AC-1 now requires Australian names in help, AC-2 asserts alias equivalence for `analyze` and `--artifacts` with no deprecation diagnostic, and AC-4 invokes the canonical verbs.

3. [x] **Align every spec that names a command, option, artefact or package with this plan.** Sweep `Docs/Specs/**`, [the architecture](../Architecture/Tailor.architecture.md) and [the master plan](Tailor.plan.md) for command names, option spellings, artefact filenames, package ids and tool-command references that disagree with the tables above, and correct them. Requirements under `Docs/Requirements/` are authoritative and are not edited.
   Evidence: `analyze` → `analyse` and `--artifacts` → `--artefacts` across the architecture, master plan and specs, keeping the alias mentions intact and leaving the real `dotnet publish --artifacts-path` option and the .NET "analyzers" term alone. WU-404 was renamed to [WU-404-cli-analyse-validate.spec.md](../Specs/M4/WU-404-cli-analyse-validate.spec.md) with its title, branch and command-folder paths. The identity-table identifiers replaced the old sidecar directory, artefact filenames, diagnostic prefix and environment prefix across docs, `.github`, `.gitignore`, [CliDiagnostics.cs](../../src/Tailor.Cli/CliDiagnostics.cs) and the scaffold tests. `git grep` for the old forms returns nothing outside `Docs/Requirements/` and this plan.

4. [x] **Record ownership and release naming expectations for WU-1002 without creating workflows.** Note in [Docs/Specs/M10/WU-1002-packaging-and-release-workflow.spec.md](../Specs/M10/WU-1002-packaging-and-release-workflow.spec.md) the repository URL, package id, tool command, licence expression and the artefact and cache naming stem the release workflow must use.
   Evidence: WU-1002 gained an `## Identity` table citing this plan, and its stale open questions about the licence and the package id are now marked resolved.

5. [x] **Leave naming regression coverage with the owning work units.** WU-105 covers canonical-versus-alias command and option parsing plus golden help ordering; WU-1002 covers package metadata and a `dotnet tool install` smoke test. This plan only records that the coverage is owed.
   Evidence: WU-105 AC-1 and AC-2 cover help ordering and alias equivalence; WU-1002 AC-1 covers nuspec metadata and AC-3 the local-feed install smoke test.

## Validation

Run from the repository root:

```powershell
dotnet build DotNetTailor.slnx -c Release -warnaserror
dotnet test --solution DotNetTailor.slnx -c Release
dotnet format DotNetTailor.slnx --verify-no-changes
```

For this plan specifically:

- A tracked-file search finds no old product, package, tool-command or repository-URL reference outside this plan's history note.
- `Docs/**` and source comments use Australian English; US spellings appear only as the CLI aliases listed above.
- [src/Tailor.Cli/Tailor.Cli.csproj](../../src/Tailor.Cli/Tailor.Cli.csproj) metadata, [LICENSE](../../LICENSE) and [README.md](../../README.md) agree on licence, copyright, repository URL, package id and tool command.

## Risks

- **Package id not yet held.** `dotnet-tailor` was available on 2026-10-01 but is unreserved, so another publisher could take it, and an id cannot be reclaimed after publication. Step 1 must complete before WU-1002 publishes anything.
- **Missed aliases.** Permanent US aliases only help discovery if every differing spelling actually registers both forms. Step 2 makes that an obligation on new options rather than a per-option judgement.
- **Spec drift.** Specs written before this plan may name commands or artefacts inconsistently; step 3 is the only guard against that.
- **Metaphor overreach.** A tailoring metaphor in a message that hides what actually happened costs more than the charm is worth. Policy item 5 keeps the metaphor out of machine-readable fields and subordinate to clarity.
- **Requirement terminology.** Requirement documents may use US spelling in names that later become wire format. Policy item 3 keeps those stable rather than localising them.

## Acceptance Criteria

- [x] The product, repository, solution, project, namespace, package and tool-command names are chosen and recorded in this plan.
- [x] The scaffold matches the chosen names: solution, `src/Tailor.*`, `tests/Tailor.*`, namespaces, package id and tool command.
- [x] Apache License 2.0 and `Copyright 2026 Kuan Bartel` are present in the repository and in CLI package metadata.
- [x] No old product, package or tool-command reference remains outside this plan.
- [x] The naming policy is stated with rationale: Australian English canonical, permanent US CLI aliases, descriptive command names, tailoring metaphors allowed in prose only.
- [x] `dotnet-tailor` is confirmed available on NuGet as of 2026-10-01.
- [x] Agent-facing guidance for metaphor use and for comment style exists in `.github/instructions/`.
- [ ] `dotnet-tailor` is reserved or published, or a fallback package id is recorded.
- [x] The permanent US-alias rule is recorded in the WU-105 spec.
- [x] Specs, architecture and master plan name commands, options, artefacts and the package consistently with this plan.
- [x] WU-105 and WU-1002 own the naming-related test coverage in their own specs.

## Test Evidence

- **State**: naming sweep across `Docs/**` (excluding `Docs/Requirements/`), `.github/**`, `.gitignore`, `src/Tailor.Cli/CliDiagnostics.cs`, `tests/Tailor.Cli.Tests/ScaffoldTests.cs`; docs included: yes; exclusions: `Docs/Requirements/**` (authoritative, never edited during implementation).
- **Environment**: Windows; .NET 10 SDK pinned by [global.json](../../global.json); no network.
- **Impact**: one production source file changed (diagnostic code constants); all other edits are documentation. No behaviour depends on the constants yet — every CLI verb is a stub.
- **Selected checks**: full solution build with warnings as errors, full test suite, format verification, and `git grep` for the retired identifiers.
- **Excluded checks**: none; the suite is small enough to run whole.
- **Command**: `dotnet build DotNetTailor.slnx -c Release -warnaserror`, `dotnet test --solution DotNetTailor.slnx -c Release`, `dotnet format DotNetTailor.slnx --verify-no-changes`, `git grep -nI "repack" / -E "RPK|DOTNET_REPACK"`, all in the repository root.
- **Result**: build succeeded; 33 passed, 0 failed, 0 skipped; format clean; `git grep` found no retired identifier outside `Docs/Requirements/` and this plan.
- **Evidence source**: run by this agent.
- **Rerun reason**: none.

## Open Questions

- When should `dotnet-tailor` be reserved on NuGet: now with a placeholder publish, or at the WU-1002 release?
- Does project policy require per-file copyright headers, or is repository, package and document level licensing sufficient?
- Does any content under `Docs/Requirements/` carry third-party attribution that repository-level Apache-2.0 does not cover?

## History

This document began as a migration-style rename plan from the repository's original name to `.NET Tailor`, written as though a released product with consumers, published packages, schema documents and generated artefacts existed. It required a rename inventory and compatibility ledger, an alias deprecation lifecycle, schema-compatibility round-tripping, package-transition guidance and a rollback strategy for a published identity. None of that applied: the rename happened while the repository was a scaffold, so no ledger was created and none is retained. Those sections were removed rather than left as permanently unmet obligations. The tailoring-metaphor command vocabulary proposed there (`measure`, `pattern`, `cut`, `stitch`) is rejected as a command surface but kept alive as prose voice under policy item 5. The file was previously named `Docs/Plans/DotNetTailor-Rename.plan.md`.
