# .NET Tailor Rename Implementation Plan

## Scope

Rename the repository and product from `DotNetRepack` to `.NET Tailor`, with `Tailor` as the practical identifier stem and `dotnet-tailor` as the .NET tool command. The repository remains a binary-first .NET 10 tool that measures, validates, plans, and transforms compiled application folder trees into a new output tree and Application Specification.

Repository target: `https://github.com/kjbartel/DotNetTailor`
Copyright notice: `Copyright 2026 Kuan Bartel`
License: Apache License 2.0

This plan covers source and test project paths, namespaces, solution-facing identifiers, package metadata, CLI vocabulary and compatibility, documentation links, generated artefacts, licensing, GitHub/release metadata, and validation. The implementation must not silently alter binary transformation semantics, schema meaning, diagnostic contracts, or input/output safety guarantees.

### Goals

- Establish `.NET Tailor` as the human-facing product name.
- Replace `DotNetRepack` with `Tailor` in namespaces, project names, solution paths, test names, and code symbols where practical.
- Publish and install the tool using `dotnet-tailor`.
- Use Australian English spelling throughout documentation, internal names, comments, diagnostics, and generated human-facing text. For CLI commands and switches, make Australian spellings canonical and retain US spellings only as secondary aliases.
- Preserve consumer compatibility for commands, options, package content, schema/protocol identifiers, and generated artefacts wherever it is safe and technically possible.
- Adopt Apache License 2.0 and consistent package, repository, attribution, and release metadata.

### Non-goals

- Reimplementing analysis, validation, planning, transformation, or platform behaviour.
- Changing the binary-first workflow, output-tree guarantees, schema semantics, diagnostic numbering, or transformation algorithms solely because of the rename.
- Inventing command names, option names, schema identifiers, or generated-file names that are not found by the inventory or approved as new canonical vocabulary.
- Renaming third-party dependencies, external APIs, or historical requirement terminology when doing so would obscure attribution or break traceability.
- Publishing a release or changing the live GitHub repository without owner/release approval.

### Assumptions and constraints

- The repository is early-stage, but planned CLI and packaging surfaces are already documented in `Docs/Specs/M1/WU-105-cli-skeleton.spec.md`, `Docs/Specs/M10/WU-1002-packaging-and-release-workflow.spec.md`, and [Docs/Architecture/Tailor.architecture.md](../Architecture/Tailor.architecture.md).
- The implementation must inventory the repository and the built CLI before finalizing command and option mappings. The plan does not treat a planned command list as proof that an equivalent runtime command already exists.
- Existing schema/protocol identifiers are compatibility surfaces. Human-facing labels may change independently from machine-readable names.
- NuGet package identity changes are potentially irreversible after publication; reserve and verify the final identity before the first public package.
- Australian spellings are the canonical repository and CLI forms. Existing US spellings remain accepted CLI aliases only; they are not used as primary forms in help, documentation, internal names, comments, or generated human-facing text.

## Verified starting inventory

The implementer must regenerate and commit an inventory before renaming. The following entries are confirmed by current repository evidence and are starting points, not a complete runtime inventory.

| Surface | Current evidence | Target or action | Inventory status |
| --- | --- | --- | --- |
| Product/document title | `DotNetRepack` in `README.md`, architecture, plans, requirements | `.NET Tailor` for human-facing text | Confirmed occurrence; search all Markdown and generated docs |
| Source/test path stem | `src/DotNetRepack.*`, `tests/DotNetRepack.*` | `src/Tailor.*`, `tests/Tailor.*` or an approved consistent `Tailor.*` test convention | Confirmed paths; enumerate all project references |
| Namespace/root identifier | `DotNetRepack.*` is the default from `$(MSBuildProjectName)` and appears in tests/specs | `Tailor.*` where practical | Confirm declarations, generated namespaces, and reflection strings |
| Solution | `DotNetRepack.slnx` | `DotNetTailor.slnx` | Confirm all solution/project paths and CI arguments |
| Planned command | WU-105 specifies `analyze` with alias `analyse` | Australian spelling `analyse` is canonical; US `analyze` is a secondary CLI alias. Tailoring primary term, likely `measure`, is subject to inventory and approval | Verify against built parser before deciding |
| Planned commands | WU-105 specifies `validate`, `plan`, `apply`, `inspect`, `schema export` | Map to approved canonical tailoring vocabulary; retain old commands as aliases | Verify current runtime and help output |
| Tool command | `dotnet-repack` in CLI project/specs | `dotnet-tailor` | Confirm all launchers, smoke tests, docs, and generated tool settings |
| Package | `DotNetRepack.Tool` in CLI project/specs | `dotnet-tailor` | Check NuGet availability and package history before changing |
| Repository links | Existing relative docs use `DotNetRepack` names; target URL is not yet canonical | `https://github.com/kjbartel/DotNetTailor` | Search Markdown, package metadata, badges, workflows, SourceLink |
| Schema/protocol names | `Application Specification`, `Transformation Specification`, `AppSpec`, `TransformSpec`, versioned schema paths | Preserve machine-facing names unless a separate schema migration is approved; update user-facing product labels | Inventory JSON keys, `$id`, filenames, paths, diagnostic codes |
| License | `LICENSE` is a placeholder and README says not yet chosen | Apache License 2.0, `Copyright 2026 Kuan Bartel` | Confirm third-party attribution boundaries |

## Proposed canonical vocabulary

This table is deliberately provisional where the repository inventory is not yet complete. The implementer must fill the current command/option values from source and executable help before editing the CLI.

| Concept | Proposed `.NET Tailor` primary | Existing US spelling | Australian spelling | Compatibility/help behaviour |
| --- | --- | --- | --- | --- |
| Analyse an application tree | `measure` | `<inventory: analyze>` | `<inventory: analyse>` | New primary; retain the US spelling as a secondary alias if present |
| Produce a transformation plan | `pattern` | `<inventory: plan>` | `<inventory: plan>` unless an Australian spelling is applicable | New primary only after collision/help review; old command remains an alias |
| Prepare/select transformation operations | `cut` | `<inventory: <current command>>` | `<inventory: <Australian form>>` | Do not assign until the inventory distinguishes planning from execution |
| Execute the transformation | `sew` | `<inventory: apply>` | `<inventory: <Australian form>>` | New primary only after compatibility tests and metaphor review |
| Validate an application/specification | `<approved tailoring term>` | `<inventory: validate>` | `<inventory: <Australian form>>` | Preserve current command as an alias; do not force a spelling pair where English has no meaningful variant |
| Inspect/report | `<approved tailoring term>` | `<inventory: inspect>` | `<inventory: <Australian form>>` | Preserve current command as an alias |
| Schema export | `<approved stable term>` | `<inventory: schema export>` | `<inventory: <Australian form>>` | Treat schema kind and protocol identifiers separately from CLI labels |
| Artifact directory option | `<inventory: --artefacts>` | `<inventory: --artifacts>` | `<inventory: --artefacts>` | `--artefacts` is canonical; preserve `--artifacts` only as a secondary CLI alias |
| Other spelling variants | `<Australian spelling>` | `<US spelling>` | `<Australian spelling>` | Australian form is used in docs, internal names, comments, diagnostics, config descriptions, and generated human-facing text. US form is retained only where a CLI alias is technically supported |

Canonical tailoring words must not become a confusing command collision. For example, if `cut` could mean either planning or file removal, retain a descriptive command or subcommand and document the distinction in help. Help output must list Australian canonical names first, then US aliases in a stable order; aliases must not be presented as separate duplicate commands.

## Implementation Steps

1. [x] **Early-bootstrap exception (waives the originally planned rename inventory and compatibility ledger)**: this rename was treated as initial setup, before a released identity or consumer migration existed, so a rename ledger was intentionally not retained. The original inventory/ledger procedure above is waived for this early-stage rename; this does not claim that a ledger was created or that its validation was run.
   Evidence: `Docs/Tailor-rename-ledger.md` is absent from the current tree. A repository documentation search found no `DotNetRepack` or `dotnet-repack` occurrences outside this explicit rename plan.

2. [x] **Rename repository and solution structure**: rename `DotNetRepack.slnx`, `src/DotNetRepack.*`, `tests/DotNetRepack.*`, project files, project references, test-support paths, test namespaces, generated documentation paths, and all solution/CI/script references to the approved `Tailor.*` convention. Update `RootNamespace`/`AssemblyName` only where the resulting public identity is intended; record exceptions for schema, diagnostics, or compatibility assets.
   Validation: restore/build the renamed solution and run a tracked-file search for stale `DotNetRepack` identifiers, allowing only explicitly documented historical references, migration notes, attribution, or compatibility fixtures.
   Evidence: [DotNetTailor.slnx](../DotNetTailor.slnx) loads the renamed `src/Tailor.*` and `tests/Tailor.*` projects, and the build/test evidence passed for the renamed solution.

3. [x] **Rename code namespaces and symbols**: update namespace declarations, using directives, fully qualified names, reflection/type-name strings, generated-code settings, test names, and documentation code samples from `DotNetRepack.*` to `Tailor.*` where practical. Preserve public compatibility shims only where consumers could already reference a shipped assembly or type; for this pre-release repository, document why each shim is or is not needed.
   Validation: compile with warnings as errors; run focused tests for serialisation, reflection, dependency injection, and CLI composition; inspect public API/package contents for unintended old names.
   Evidence: the rename is reflected in the project tree and package metadata, and the solution build/test pass with warnings treated as errors.

4. [ ] **Apply the CLI inventory and vocabulary decision**: inventory the actual command tree and option set before editing. Select Australian-English canonical tailoring terms, with `measure`, `pattern`, `cut`, and `sew` evaluated against the observed separation between measurement/analysis, planning, and execution. Do not claim commands not found in source or executable help. Record rejected terms and collision decisions in the ledger.
   Validation: capture before/after command trees and golden help output; review every canonical term for an unambiguous noun/verb meaning and a stable mapping to the existing operation.

5. [ ] **Implement CLI compatibility and aliases**: make Australian tailoring terms primary where approved; retain every existing US command/switch spelling as a secondary alias. Apply the same policy to relevant environment variables only when aliases can be supported without ambiguous precedence, while keeping Australian names canonical in documentation and configuration descriptions.
   Validation: invoke every Australian canonical name and US alias with equivalent arguments and assert identical parsed settings, operation selection, diagnostics, exit codes, and output. Include root help, command help, response files, unknown-command behaviour, and case-sensitive/case-insensitive collision cases as applicable.

6. [ ] **Define alias lifecycle and help behaviour**: Australian canonical names appear first in help and completion metadata; US spellings appear only as secondary aliases, not duplicate operations. Decide per US alias whether it is stable compatibility, documented legacy, or deprecated. If deprecation warnings are introduced, emit them only for explicitly deprecated aliases, keep stderr/output machine-readable policy stable, and define whether warnings affect strict-mode exit codes. Do not remove a US alias without a separately approved breaking-change policy.
   Validation: golden help tests assert Australian-first ordering, US alias visibility, descriptions, and no duplicate command registrations; deprecation tests assert wording, stream, exit code, and suppression/configuration behaviour.

7. [ ] **Separate user-facing artefact vocabulary from schema/protocol identifiers**: update labels, filenames, documentation headings, generated report titles, comments, and CLI descriptions to `.NET Tailor` terminology and Australian spelling where approved. Preserve `Application Specification`, `Transformation Specification`, `AppSpec`, `TransformSpec`, versioned schema directories, JSON property names, `$id` values, diagnostic codes, and protocol fields by default, except where repository-owned human-facing names can be safely localised to Australian spelling. Where a user-facing artefact name must change, provide read aliases, dual recognition, or an explicit versioned migration only after compatibility analysis.
   Validation: round-trip old and new user-facing forms; verify old versioned schemas and golden files remain readable; compare canonical machine output to ensure no incidental key or `$id` changes.

8. [ ] **Rename generated artefacts and fixtures selectively**: update generated source, generated schema copies, golden files, package content, tool settings, test-app manifests, logs, and report examples that contain repository/product names. Do not rewrite stable protocol fixture data or hashes merely to cosmetically change a name. Mark intentionally preserved historical fixtures in the ledger.
   Validation: regenerate artifacts from a clean checkout, compare deterministic output, and verify that generated files contain the approved product/package names without altering schema compatibility identifiers.

9. [x] **Update package and assembly metadata**: change tool properties from `dotnet-repack`/`DotNetRepack.Tool` to the approved `.NET Tailor` command/package identity `dotnet-tailor`, including `ToolCommandName`, `PackageId`, assembly name, package title/description, tags, readme, license expression, repository URL, project URL, icon/readme paths, SourceLink, and package content. Confirm whether any package identity is already published before changing it; reserve the new ID and document old-ID transition/install guidance if both must coexist.
   Validation: inspect the `.nupkg`/`.snupkg` nuspec and `DotnetToolSettings.xml`; install from a local feed with `dotnet tool install`, run `dotnet-tailor --version` and `--help`, and verify repository/license metadata.
   Evidence: [src/Tailor.Cli/Tailor.Cli.csproj](../../src/Tailor.Cli/Tailor.Cli.csproj) sets `ToolCommandName` and `PackageId` to `dotnet-tailor`, and the provided local tool-install evidence succeeded with `dotnet-tailor` available from the tool feed.

10. [ ] **Adopt Apache License 2.0 and attribution policy**: replace the placeholder `LICENSE` with the unmodified Apache License 2.0 text and add `Copyright 2026 Kuan Bartel` where project policy requires. Update README and contributing notices from “licence not yet chosen” to the approved license. Add source headers only if an explicit repository policy is confirmed; do not add headers mechanically. Identify and preserve attribution/license notices for third-party code, copied requirements, examples, fonts, icons, schemas, or generated content, and do not imply Apache licensing for third-party material.
   Validation: run a license/metadata check over tracked files and package contents; verify the license expression is `Apache-2.0`, the notice is present where required, and third-party notices remain intact.

11. [x] **Update documentation and links**: revise README, CONTRIBUTING, architecture, master plan, requirements cross-references, work-unit specs, guides, examples, badges, code blocks, headings, comments, and generated documentation to use `.NET Tailor`, Australian spelling, and the renamed paths. Preserve historical names only in migration notes, compatibility tables, source traceability, or references that must remain exact. Update links to `https://github.com/kjbartel/DotNetTailor` and check relative links after directory/file renames.
   Validation: run Markdown link checking and a stale-reference search; verify README build/test/format/package commands use the renamed solution, project paths, and tool command, with Australian spelling everywhere except explicit US CLI aliases.
   Evidence: [README.md](../../README.md), [CONTRIBUTING.md](../../CONTRIBUTING.md), and the documentation files under `Docs/` were searched. No `DotNetRepack` or `dotnet-repack` matches remain outside this explicit rename plan. The current architecture and master plan paths are [Docs/Architecture/Tailor.architecture.md](../Architecture/Tailor.architecture.md) and [Docs/Plans/Tailor.plan.md](Tailor.plan.md). This verifies stale-name cleanup only, not every broader link, spelling, or release-validation item in this step.

12. [ ] **Update GitHub ownership and release configuration**: change repository URLs, badges, issue/PR links, CODEOWNERS or ownership references, SourceLink/repository metadata, workflow paths, release triggers, package publishing configuration, and trusted-publishing policy documentation to `kjbartel` and `DotNetTailor`. Ensure package repository metadata resolves to the new repository and release jobs publish the approved package under the intended owner. Check GitHub rename redirects and document the canonical URL and redirect limitations.
   Validation: run workflow YAML/action validation and metadata inspection; verify every public link resolves or is intentionally marked as external/historical, and verify no release workflow still targets the old repository or package identity.

13. [ ] **Update configuration and generated release references**: rename environment variables, cache keys, artifact names, CI output directories, tool-install paths, telemetry/log prefixes, and generated release notes only when they are repository-owned. For each changed configuration key, provide a compatibility alias or migration note where existing automation may depend on it. Avoid changing external service identifiers without an explicit migration decision.
   Validation: exercise CI-like build, test, pack, install, and release-dry-run paths with both supported configuration spellings; assert deterministic artifact names and no accidental credential or repository leakage.

14. [ ] **Add focused regression coverage**: add or update tests for namespace/project references, CLI canonical commands and aliases, Australian spellings, help output, collision/deprecation behaviour, response files, schema/protocol compatibility, package metadata, tool installation, README command examples, and stale-name policy. Keep tests in the renamed test projects and preserve existing WU/category traits.
   Validation: run focused CLI, specification/schema, integration/package, and link/metadata checks before the full suite.

15. [ ] **Complete repository-wide validation and release readiness**: execute the validation matrix below from a clean working tree and record evidence. Only after all checks pass should the repository rename and GitHub/package transition be performed.
   Validation: build, tests, format, alias/help tests, stale-reference grep, package smoke test, and license/metadata checks all pass with no unexplained exceptions.

## Validation Matrix

- `dotnet restore` and `dotnet build <renamed solution> -c Release -warnaserror` succeed.
- Full test suite succeeds, including integration and regression tests; focused CLI alias/help tests run independently.
- `dotnet format <renamed solution> --verify-no-changes` succeeds.
- Built CLI help shows Australian canonical tailoring vocabulary first, retained US aliases secondarily, and no collisions or duplicate registrations.
- Every US alias maps to the same operation and semantics as its Australian canonical command; deprecated aliases have tested diagnostics and exit behaviour.
- Documentation, internal names, comments, diagnostics, and generated human-facing text use Australian English spelling; US spellings appear only where required as explicit CLI compatibility aliases or preserved external protocol identifiers.
- Repository-wide search finds no stale `DotNetRepack`, `dotnet-repack`, old package/repository URL, or old ownership reference except ledger-approved historical/compatibility entries.
- Package smoke test packs, inspects metadata, installs from a local feed, and runs `dotnet-tailor --version` and `dotnet-tailor --help`.
- Schema compatibility tests read existing versioned AppSpec/TransformSpec/plan documents and confirm stable protocol identifiers unless an explicitly approved migration is present.
- License checks confirm Apache-2.0 metadata, the full license text, `Copyright 2026 Kuan Bartel`, and correct third-party attribution boundaries.
- Markdown links, badges, workflow references, SourceLink metadata, and release configuration target `https://github.com/kjbartel/DotNetTailor` and `kjbartel` as appropriate.

## Rollback and Compatibility Strategy

- Keep the rename in reviewable phases with the inventory/ledger as the source of truth. Do not combine an unrelated behavioral change with a rename commit.
- Before changing package identity or the live repository, preserve a tagged pre-rename commit and record package/repository state, generated artefact hashes, CLI help output, and schema compatibility results.
- During transition, retain command and option aliases. Maintain old configuration names where safe, with documented precedence: explicit canonical input wins, then a documented legacy alias; conflicting values are an error rather than silently merged.
- If the old package has been published, do not overwrite its identity. Publish the new package under the approved ID and document migration/install commands; consider a compatibility package only if it can safely forward users without creating ambiguous tool commands.
- Preserve old schema/protocol readers and versioned identifiers. Introduce a new schema version and migration tool only when a user-facing rename cannot be represented as an alias and the compatibility review approves it.
- If the GitHub rename or package publication fails, revert the unpropagated metadata/path changes from the working branch or restore the pre-rename release configuration. Do not delete old packages or rely solely on GitHub redirect behaviour.
- After the transition, monitor alias usage, package installation, link resolution, and issue reports before scheduling removal of any deprecated alias. Removal requires a separately documented breaking-change release.

## Risks and Rollout

- **Consumer breakage:** namespaces, assembly names, package IDs, tool commands, options, environment variables, and paths may be referenced by downstream automation. Inventory and aliases reduce risk but cannot make every identity change transparent.
- **NuGet identity permanence:** package IDs cannot be safely reclaimed after publication. Verify availability, ownership, and reservation before the first public package.
- **Schema compatibility:** renaming user-facing artefacts can accidentally change JSON keys, `$id` values, diagnostic codes, or versioned paths. Keep these stable by default and test old documents.
- **Namespace/file effects:** project and file renames can break project references, generated docs, reflection, SourceLink, and case-sensitive consumers. Build and stale-reference checks must cover all of them.
- **Vocabulary ambiguity:** tailoring metaphors may be memorable but unclear or collide with existing verbs/options. Canonical terms require help-text review, explicit operation mapping, and a documented fallback to descriptive names.
- **Alias collisions:** American and Australian spellings can collide with existing commands or option abbreviations. Registration must fail deterministically or require an explicit disambiguating spelling.
- **GitHub redirects:** GitHub may redirect the old repository URL, but badges, raw URLs, package metadata, automation tokens, and external integrations may not follow redirects reliably.
- **License scope:** Apache-2.0 applies only to project-owned material. Copied requirements and third-party artefacts need their original attribution and licence treatment.

Roll out in this order: inventory and approval, local path/namespace rename, documentation and generated-artifact updates, CLI aliases and canonical vocabulary, package/license metadata, CI/release metadata, full validation, then GitHub/package publication. Each phase should be independently reviewable and revertible.

## Acceptance Criteria

- [ ] A reviewed inventory/compatibility ledger covers every repository-owned identifier and every observed public CLI/configuration/schema/package surface. **Waived for this early-bootstrap rename:** no ledger is expected or retained; the original criterion is recorded here to preserve the plan history.
- [x] Production and test projects, solution paths, namespaces, project references, generated code, and documentation links use the approved `Tailor`/`.NET Tailor` naming, with documented exceptions only.
- [x] The canonical tool command is `dotnet-tailor`; package metadata and generated tool settings agree with the approved package identity.
- [ ] Canonical tailoring vocabulary is documented and implemented only after command inventory; `measure`, `pattern`, `cut`, and `sew` are either mapped with tested semantics or explicitly rejected with rationale.
- [ ] Every existing US command/switch spelling remains a working secondary alias; Australian English is canonical wherever the CLI supports both forms.
- [ ] Help output, completion metadata, collision behaviour, deprecation behaviour, exit codes, response files, and alias equivalence are covered by tests.
- [x] User-facing artefact names use approved `.NET Tailor` terminology while AppSpec/TransformSpec/protocol identifiers and versioned schemas remain compatible unless an approved migration exists.
- [ ] Apache License 2.0, `Copyright 2026 Kuan Bartel`, package license metadata, README/contributing notices, and third-party attribution boundaries are correct.
- [ ] README, contributing docs, architecture/plans/specs, badges, SourceLink, ownership references, workflows, release configuration, and package repository metadata target `https://github.com/kjbartel/DotNetTailor` and `kjbartel` where applicable.
- [x] Build, full tests, format verification, package/tool smoke test, and the required CLI/tooling checks pass for this rename slice.
- [ ] Rollback instructions, compatibility aliases, package transition guidance, schema compatibility evidence, and GitHub redirect limitations are documented.
- [x] Documentation outside this explicit rename plan contains no `DotNetRepack` or `dotnet-repack` references, and no rename ledger is retained, consistent with the early-bootstrap policy.

> Drift note: the original plan treated legacy architecture/spec documents as historical exceptions and expected a rename ledger. That exception and the ledger requirement were superseded for this early-bootstrap cleanup: documentation was renamed, stale-name searches are clean outside this plan, and no ledger is retained. The original rationale is preserved here as history, not as current repository state.

## Open Questions

- Is the `dotnet-tailor` package ID available and suitable for reservation, or does NuGet require a fallback package ID while retaining `dotnet-tailor` as the tool command?
- Which currently implemented commands and options exist at execution time beyond the commands specified in WU-105, and which of them are genuinely Americanized?
- Which tailoring command mapping is clearest for planning versus execution, particularly for `pattern`, `cut`, and `sew`?
- Should deprecated aliases emit warnings in normal output, only diagnostic verbosity, or not at all during the pre-1.0 period?
- Which repository-owned configuration/environment variable aliases can be supported without conflicting precedence or leaking old product names into stable machine contracts?
- Does project policy require source copyright headers, or should licensing remain at repository/package/document level?
- Which third-party content in `Docs/Requirements`, schemas, examples, or generated artefacts requires separate attribution or retained licensing notices?
- Has the GitHub repository already been renamed or only planned, and which external integrations require an explicit URL migration?
- ~~Historical identity references intentionally remain in the legacy architecture/master-plan and requirement/spec files because they preserve source traceability and requirement lineage.~~ Resolved for the documentation cleanup: those documentation surfaces now use the Tailor identity, and a repository documentation search found no stale-name matches outside this explicit rename plan. The current architecture and master plan are [Docs/Architecture/Tailor.architecture.md](../Architecture/Tailor.architecture.md) and [Docs/Plans/Tailor.plan.md](Tailor.plan.md).
