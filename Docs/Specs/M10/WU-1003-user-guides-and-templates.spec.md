# WU-1003 user-guides-and-templates

| Field | Value |
|---|---|
| ID | WU-1003 |
| Title | user-guides-and-templates |
| Milestone | M10 Configuration, Hardening & Release → v1.0.0 |
| Status | Ready |
| Depends on | WU-903, WU-1000, WU-1001 |
| Parallel with | WU-1004 |
| Target project(s)/paths | `Docs/Guides/` (guides), `templates/` (TransformSpec templates), `src/Tailor.Cli/Tailor.Cli.csproj` (pack templates as package content), `tests/Tailor.IntegrationTests/` (template + example validation), `tests/Tailor.Specifications.Tests/` (doc example schema validation) |
| Size | M |

## Goal

Ship user guides and reusable TransformSpec templates, including `enterprise-win-x64.transform.json`. Tests validate every template and every documentation example, so docs cannot drift from the schemas or from tool behaviour.

## Requirement Traceability

| Ref | Topic |
|---|---|
| [TS §9.5](../../Requirements/Transformation_Specification.md), [TS §21.4](../../Requirements/Transformation_Specification.md) | Preservation by default. Destructive policies are explicit (template, not default) |
| [TS §15](../../Requirements/Transformation_Specification.md), [TS §16](../../Requirements/Transformation_Specification.md), [TS §17](../../Requirements/Transformation_Specification.md) | Resources (`en`, `en-*`), symbols separate, docs exclusion |
| [TS §14.3](../../Requirements/Transformation_Specification.md), [TS §28](../../Requirements/Transformation_Specification.md), [TS §33](../../Requirements/Transformation_Specification.md) | R2R scope, template variables, conceptual example |
| [CK §6.2](../../Requirements/Read_to_run_Cake.md), [CK §7.1](../../Requirements/Read_to_run_Cake.md), [CK §7.3](../../Requirements/Read_to_run_Cake.md), [CK §8](../../Requirements/Read_to_run_Cake.md) | Historical enterprise defaults: win-x64 only, English-only, symbols package |
| [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md) | Interactive and CI usage |
| [Architecture §6, §14, §19 item 4](../../Architecture/Tailor.architecture.md) | Spec shapes, CLI, template decision |

## Scope

**In**
- Guides in `Docs/Guides/`:
  - `getting-started.md`: install, `analyse` → `validate` → `plan` → `apply`, artefacts, exit codes.
  - `appspec-authoring.md`: folders, masks, `idRef`, classification, associations, references, includes, validation workflow.
  - `transformspec-authoring.md`: operations, selectors, precedence, policies, variables, output assertions, dry-run. Includes retarget compatibility limits (plan risk R6).
  - `recipes.md`: filtering-only, symbols separate, English-only, R2R app+plugins, FD→SC, SC runtime patch, retarget net8→net10, library patch, the TS §33 combination.
  - An index `Docs/Guides/README.md` linking these and the existing `diagnostics.md` (WU-1001), `configuration.md` (WU-1000) and `releasing.md` (WU-1002).
- Templates in `templates/`:
  - `enterprise-win-x64.transform.json`: remove runtime assets for RIDs other than `${targetRid}` (default `win-x64`), keep `en`/`en-*` resources only, exclude XML docs, symbols `separate` (zip), R2R for `assemblyRole` application + plugin, input/output assertions for RID. No deployment-model, retarget or patch operations.
  - Smaller single-purpose templates used by `recipes.md` (e.g. `symbols-separate`, `english-only`, `r2r-app-plugins`, `fd-to-sc-win-x64` (states `runtimeVersion` explicitly; architecture §10 forbids an implicit runtime version)).
- Distribution: templates are packed into the tool package (`content/templates/` or `tools/…/templates/`) and attached to GitHub releases.
- Validation tests:
  - Every template validates against the committed TransformSpec schema and passes semantic validation against at least one matrix AppSpec.
  - `enterprise-win-x64.transform.json`: `plan` succeeds for every matrix app, and `apply` + output assertions + launch smoke pass for console, WinForms and WPF (net8 + net10, FD + SC).
  - Every fenced ```` ```json ```` block in the guides that declares `"kind"` validates against its schema. Fragments are marked with an info string (e.g. ```` ```jsonc fragment ````) and skipped.
  - Relative links in `Docs/Guides/*.md` resolve.

**Out**
- A `template` CLI verb (see Open Questions). Localised docs. Docs website.

## Deliverables

- The guides, templates, packing change and tests listed above.

## Design Notes

- [Architecture §19 item 4](../../Architecture/Tailor.architecture.md): English-only and other-RID removal are **not** defaults; they live in this template.
- Reuse the WU-903 TS §33 fixture for the combination recipe. Do not maintain two copies: the recipe links to or includes the same file, verified by test.
- Use Australian spelling for canonical CLI terms (`analyse`, `--artefacts`); mention `analyze` and `--artifacts` only as permanent aliases ([Architecture §14](../../Architecture/Tailor.architecture.md), [naming plan](../../Plans/Tailor-naming.plan.md)).
- Keep the schemas normative. Guides show examples and explain, but do not redefine members.

## Acceptance Criteria

- [ ] AC-1 The five guide files exist and are linked from `Docs/Guides/README.md`. The link-check test passes.
- [ ] AC-2 `templates/enterprise-win-x64.transform.json` exists with the five policies listed in Scope and no per-file enumeration.
- [ ] AC-3 Every template validates against the TransformSpec schema and semantically against a matrix AppSpec (test).
- [ ] AC-4 The enterprise template plans successfully for every matrix app. `apply` output for console, WinForms and WPF (net8/net10 × FD/SC) passes output assertions and launches (harness).
- [ ] AC-5 Every full JSON example in the guides validates against its schema (test extracts code blocks).
- [ ] AC-6 Every recipe references a template or fixture that the tests exercise.
- [ ] AC-7 The packed tool package contains the templates. The GitHub release workflow attaches them (if WU-1002 is `Done`).

## Test Requirements

- xUnit v3 on MTP. Integration tier over `artifacts/testapps/` with the offline local package feed (crossgen2 packs for R2R).
- Launch smoke only in the harness.
- Integration tests carry `Category=Integration`, `Category=Matrix`, `Category=Launch` as applicable; doc-example tests carry no category.
- Run: `dotnet test --project tests/Tailor.Specifications.Tests --filter-trait "WU=1003"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=1003"`.
- Record Test Evidence: commands, TRX and matrix results table.

## Definition of Done

- All AC verified. CI green. The M10 "guides and template shipped" criterion is ticked by the Verifier.
- The plan status is updated. Test Evidence is recorded.

## Agent Notes

- Write guides from actual CLI behaviour: run the commands and paste real (normalised) output.
- Keep each guide focused. Cross-link instead of repeating.

## Open Questions

- How users get templates from an installed tool: package content only, release assets, or a `dotnet-tailor template export <name>` verb (new CLI surface, not planned).
