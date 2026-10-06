# WU-001 ai-enablement

| Field | Value |
| --- | --- |
| ID | WU-001 |
| Title | ai-enablement |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) |
| Status | In review |
| Depends on | WU-000 |
| Parallel with | WU-002, WU-003, WU-006, WU-007, WU-100 |
| Target paths | `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/`, `.github/prompts/`, `.github/skills/`, `.github/agents/`, `Docs/Architecture/Tailor.architecture.md` (§18 inventory only) |
| Size | M |
| Branch | `wu/001-ai-enablement` |

## Goal

Give AI coding agents a single, consistent source of repo conventions and a repeatable WU workflow (implement → verify → new spec). The files must reference the architecture and plan rather than duplicate them.

## Requirement Traceability

| Source | Section | Relevance |
| --- | --- | --- |
| [Architecture](../../Architecture/Tailor.architecture.md#18-repository-ai-enablement) | §18 Repository AI Enablement | File list |
| [Architecture](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies) | §3.1 | "Must not" boundaries to encode |
| [Architecture](../../Architecture/Tailor.architecture.md#15-determinism) | §15 Determinism, [§17 Security](../../Architecture/Tailor.architecture.md#17-security) | Rules to encode |
| [Plan](../../Plans/Tailor.plan.md#how-agents-use-this-plan) | How Agents Use This Plan; M0 criterion 5 | Workflow, spec path convention |
| [RQ](../../Requirements/Repackage_tool_Requirements_v1.1.md) | §2.2 Out of Scope, §9 Input/Output Rules, §12 Non-Functional | Safety boundaries |

## Scope

**In**: the files in Deliverables, including the `architecture-change` skill, with valid frontmatter, cross-linked to architecture, plan and specs; minimal architecture §18 skill inventory synchronisation.

**Out**: code, CI (WU-002), test apps (WU-003), architecture design changes or edits beyond the §18 skill inventory, editing plan content, MCP server configuration.

## Deliverables

| Path | Frontmatter | Content (concise bullets, link instead of copy) |
| --- | --- | --- |
| `AGENTS.md` | none | Repo map (`src/`, `tests/`, `build/`, `schemas/`, `Docs/*`, `spikes/`); build/test/format commands; conventions summary; WU workflow (plan steps 1–7); boundaries (§3.1 "Must not" column); determinism rules (§15); safety rules (never mutate input tree, no shell execution, no secrets in logs/artefacts, path confinement); Test Evidence block format; what agents must not edit |
| `.github/copilot-instructions.md` | none | Short pointer to `AGENTS.md` plus the 10 most important rules |
| `.github/instructions/csharp.instructions.md` | `applyTo: "**/*.cs"`, `description` | File-scoped namespaces, nullable, no `#pragma` suppressions without justification, `RelativePath`/`Diagnostic` usage, ordinal-ignore-case sorting, canonical JSON only through Core writer, `ArgumentList` for processes, async + `CancellationToken`, project boundary rules, line endings (CRLF source files; tool-generated artefacts always LF via the Core writer, architecture §15) |
| `.github/instructions/tests.instructions.md` | `applyTo: "tests/**"`, `description` | xUnit v3 + MTP, naming: classes `<TypeUnderTest>Tests`, methods PascalCase `<Subject><Condition><ExpectedResult>` with no underscores (e.g. `ParseRejectsAbsolutePath`), CA1707 never suppressed; golden-file rules (`Golden.AssertMatches` from `tests/Tailor.Testing`, `Golden/<TestClass>/<name>.golden.json` committed and LF, `*.received.*` git-ignored, scrubbers for temp/repo paths, `DOTNET_TAILOR_UPDATE_GOLDEN=1` only locally, never in CI), fixtures from `artifacts/testapps` via manifest, no network in unit tests, `dotnet test` filter syntax, Test Evidence recording |
| `.github/instructions/docs-specs.instructions.md` | `applyTo: "Docs/**/*.md"`, `description` | Spec template (header table → Open questions), AC as `- [ ] AC-n` (only the Verifier ticks), relative links, requirement citation style (`[RQ §n](…)`), ADR format and `Docs/Decisions/ADR-NNNN-<slug>.md` naming, spike report template |
| `.github/prompts/plan-work.prompt.md` | `description`, `agent: agent`, input for the planning request | Route planning requests through the planning-artifacts skill and stop new artefacts at `Draft` |
| `.github/prompts/implement-work-unit.prompt.md` | `description`, `agent: agent`, input `${input:wu}` | Load plan + `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md` + cited sections; check deps `Done`; set status `In progress`; branch; implement; tests; zero warnings; format; Test Evidence; set `In review` |
| `.github/prompts/review-changes.prompt.md` | `description`, `agent: agent`, input for the branch or change set | Review a WU against its spec and report findings with the code-review skill |
| `.github/prompts/address-review.prompt.md` | `description`, `agent: agent`, input for the WU and findings | Resolve review findings in the WU scope and provide a resolution table |
| `.github/prompts/verify-work-unit.prompt.md` | `description`, `agent: agent`, input `${input:wu}` | Re-check every AC with evidence (reuse valid Test Evidence), tick `- [x]` only when proven, update plan status to `Done`, tick milestone criteria when met; report failed ACs |
| `.github/prompts/new-work-unit-spec.prompt.md` | `description`, `agent: agent`, input `${input:wu}` | Create the spec from the plan row using the M0 spec template; path and slug exactly as in the status table |
| `.github/skills/work-unit-workflow/SKILL.md` | `name: work-unit-workflow`, `description` | Status transitions, branch/PR naming, DoD checklist, when to write an ADR, Test Evidence protocol summary |
| `.github/skills/planning-artifacts/SKILL.md` | `name: planning-artifacts`, `description` | Artefact hierarchy, status transitions, phase gates, and templates for planning documents |
| `.github/skills/architecture-change/SKILL.md` | `name: architecture-change`, `description` | Design-impact assessment; ADR numbering, content and approval; architecture synchronisation; requirements authority and generated schema rules |
| `.github/skills/code-review/SKILL.md` | `name: code-review`, `description` | Review checklist, findings format, verdict, and resolution table |
| `.github/skills/test-evidence/SKILL.md` | `name: test-evidence`, `description` | Select checks, fingerprint functional state, and record or reuse Test Evidence |
| `.github/skills/devops-pipelines/SKILL.md` | `name: devops-pipelines`, `description` | Conventions for CI, nightly, Dependabot, pack and publish workflow changes |
| `.github/skills/schema-change/SKILL.md` | `name: schema-change`, `description` | Model change → regenerate `schemas/*/v1/` via `schema export` → drift test → `schemaVersion` minor/major rules ([§6.1](../../Architecture/Tailor.architecture.md#61-common-rules)) → update golden files and docs |
| `.github/skills/test-apps/SKILL.md` | `name: test-apps`, `description` | How to run `build/Build-TestApps.ps1`, matrix and folder naming, `manifest.json` use, adding a new test app, cache key impact |
| `.github/agents/contributor.agent.md` | `description`, `tools`, agent list and handoffs | Entry point that routes contributions through the plan, implement, devops, docs, review and fix lanes |
| `.github/agents/planner.agent.md` | `description`, `tools`, agent list and handoff | Creates planning artefacts; uses Research and Probe; edits planning documents only |
| `.github/agents/implementer.agent.md` | `description`, `tools`, agent list and handoff | Implements one WU; must not tick ACs; must not edit architecture except with an ADR |
| `.github/agents/code-reviewer.agent.md` | `description`, `tools`, agent list and handoffs | Read-only code review with `Approve` or `Request changes` verdict |
| `.github/agents/verifier.agent.md` | `description`, `tools`, agent list | Verifier role; read-only on production code; ticks proven criteria and plan status |
| `.github/agents/probe.agent.md` | `description`, `tools` | Answers one concrete behaviour question with Test Evidence; does not implement features |
| `.github/agents/research.agent.md` | `description`, `tools` | Read-only research; reports sourced options and trade-offs without deciding |

## Design Notes

- Single source of truth: architecture = design, plan = sequencing/status, spec = WU contract. Instruction files state rules and link sections. No duplicated tables.
- Where WU-003 details (script parameters) do not exist yet, the `test-apps` skill cites [WU-003 spec](WU-003-test-app-suite.spec.md) and marks the section "update when WU-003 lands".
- The `test-evidence` skill is vendored in the repo. `AGENTS.md` and `work-unit-workflow` also include the Test Evidence block format so contributors can follow the protocol without depending on user-level assets.
- Keep `copilot-instructions.md` under ~60 lines. Put longer guidance in scoped instruction files.
- Commands use `DotNetTailor.slnx` and match WU-000 and root `AGENTS.md`.
- Boundaries to state: never modify `Docs/Requirements/**`; spikes code is never referenced from `src/`; no new package without CPM entry; no timestamps/GUIDs/machine paths in canonical output.

## Acceptance Criteria

- [x] AC-1 All 26 files in Deliverables exist at the exact paths.
- [x] AC-2 Every `*.instructions.md` has YAML frontmatter with `applyTo` and `description`; globs are `**/*.cs`, `tests/**`, `Docs/**/*.md`.
- [x] AC-3 Every `SKILL.md` has frontmatter `name` equal to its folder name and a non-empty `description`.
- [x] AC-4 Every `*.prompt.md` and `*.agent.md` has frontmatter with `description`; YAML parses. **Verified:** PyYAML parsed all 24 customization frontmatters (7 agents, 6 prompts, 3 instructions, 8 skills); all parsed as mappings with required fields.
- [x] AC-5 `implement-work-unit.prompt.md` references `Docs/Plans/Tailor.plan.md` and the path convention `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md` (M0 milestone criterion 5).
- [x] AC-6 `AGENTS.md` and each instruction file link to `Docs/Architecture/Tailor.architecture.md`; all relative links resolve. **Verified:** all 19 relative Markdown links resolve.
- [x] AC-7 Commands in `AGENTS.md` run successfully as written. **Verified:** all three commands passed using `DotNetTailor.slnx`.
- [x] AC-8 Reviewer confirms no statement contradicts the architecture (project boundaries §3.1, exit codes §13, artefact rules §5, determinism §15). **Verified:** Tailor Code Reviewer approved the reviewed working tree at `305184562314784bc508d145defd0d9c8edb40db`; see Completion.
- [x] AC-9 `verifier.agent.md` tool list excludes file-editing tools other than those needed to tick ACs and plan status. **Verified:** `edit` is available for that purpose and the role instructions limit its use to the selected WU spec and plan status; production code and tests remain read-only.

## Test Requirements

- No code tests. Validation is by commands in AC-4, AC-6, AC-7.
- Record a Test Evidence block for AC-7 (reuse WU-000/CI evidence if the functional-state fingerprint matches; Markdown-only changes do not invalidate it).
- The Verifier uses available CI evidence first, then relevant tests for uncovered checks; run the full test-app matrix only when required by the affected checks.

## Test Evidence

### Build

- **State**: `8992f57dc164983696445f4659641868de9804c1c1bc89f7b13cb31ab534ee39` (`54` files; docs included: no; exclusions: none)
- **Environment**: `Windows; .NET SDK 10.0.401; xUnit v3/Microsoft Testing Platform dependencies restored; CI/GITHUB_ACTIONS unset`
- **Impact**: `new AGENTS.md and 12 .github customization files; no production code, tests, fixtures, dependency manifests or build configuration changed`
- **Selected checks**: `complete Release build and full solution test, required by WU-001 AC-7; all solution projects/test assemblies`
- **Excluded checks**: `none — AC-7 requires the full solution build and test`
- **Command**: `dotnet build DotNetTailor.slnx -c Release -warnaserror` (repo root)
- **Result**: `pass — exit 0; all 29 projects built; 0 warnings, 0 errors`
- **Evidence source**: `run by this agent`
- **Rerun reason**: `none`

### Test

- **State**: `8992f57dc164983696445f4659641868de9804c1c1bc89f7b13cb31ab534ee39` (`54` files; docs included: no; exclusions: none)
- **Environment**: `Windows; .NET SDK 10.0.401; xUnit v3/Microsoft Testing Platform; CI/GITHUB_ACTIONS unset`
- **Impact**: `new AGENTS.md and 12 .github customization files; full solution test command is required by WU-001 AC-7`
- **Selected checks**: `all 15 solution test assemblies (MTP)`
- **Excluded checks**: `none — the full suite is the specified AC-7 command`
- **Command**: `dotnet test --solution DotNetTailor.slnx -c Release` (repo root)
- **Result**: `pass — exit 0; 15 succeeded, 0 failed, 0 skipped`
- **Evidence source**: `run by this agent`
- **Rerun reason**: `none`

### Format

- **State**: `8992f57dc164983696445f4659641868de9804c1c1bc89f7b13cb31ab534ee39` (`54` files; docs included: no; exclusions: none)
- **Environment**: `Windows; .NET SDK 10.0.401; CI/GITHUB_ACTIONS unset`
- **Impact**: `solution source formatting gate; Markdown customization files are not covered by dotnet format`
- **Selected checks**: `repository-defined dotnet format verify command required by WU-001 AC-7`
- **Excluded checks**: `no Markdown formatter/linter is configured or installed`
- **Command**: `dotnet format DotNetTailor.slnx --verify-no-changes` (repo root)
- **Result**: `pass — exit 0, no output`
- **Evidence source**: `run by this agent`
- **Rerun reason**: `none`

The Build, Test and Format passes above are preserved historical results, not reruns for this follow-up. The follow-up changes only Markdown; those edits cannot affect solution build, runtime tests or source formatting. The current no-documentation fingerprint (`e6e86d713d8ac527fd63c37673d6cbe94c86ff8bcb70988b393e0e0ffddf32e3`, 60 files) differs from their recorded state, so those results are not claimed as exact-current-state reuse.

### Documentation follow-up validation

- **State**: `4f211ded1dddc048212c536130b20263eb1d2f22755c812fcd0f1a379fec47a8` (`164` files; docs included: yes; exclusions: none)
- **Environment**: `Windows; PowerShell 7.6.6; Python 3.14.7; PyYAML 6.0.3 in an isolated, subsequently removed session environment`
- **Impact**: `Agent guidance, WU-001 contract and architecture inventory; no runtime changes`
- **Selected checks**: `exact three-file scope; deliverable count/existence; links and anchors; unchanged checkboxes/status; inventory-only architecture; CRLF/whitespace; real YAML parsing across all customization frontmatters`
- **Excluded checks**: `Build, runtime tests and test-app matrix cannot observe this Markdown-only follow-up; historical build/test/format passes above are preserved without claiming a rerun`
- **Command**: `& '<session>\files\Validate-WU001Followup.ps1'`; `& '<session>\files\yaml-validation-env\Scripts\python.exe' '<session>\files\Validate-Frontmatter.py'`; `git diff --check` (repository root; `<session>` denotes the temporary session directory supplied for validation)
- **Result**: `pass — exit 0; exactly three changed files; 26 deliverables; nine skill links/anchors resolved; 24 YAML frontmatters parsed (7 agents, 6 prompts, 3 instructions, 8 skills)`
- **Evidence source**: `follow-up checks run by implementer; scoped validator rerun by verifier; YAML parser result reused from implementer's record`
- **Rerun reason**: `new documentation checks and the reviewer's initial request uncovered the need for real YAML parser evidence; the recorded fingerprint identifies the implementation tree validated before verifier-only WU/plan status and evidence edits, which did not alter delivered customization guidance`

## Definition of Done

- All AC ticked by the Verifier; solution still builds with zero warnings and tests green; format clean.
- Plan status updated (`In review` → `Done`).

## Completion
- **Summary**: Added the `architecture-change` skill, synchronized architecture §18 inventory, and verified WU-001 deliverables.
- **Commits/PR**: None (branch-only completion requested; no commit or PR).
- **Code review**: Approve @ `305184562314784bc508d145defd0d9c8edb40db`; CR-1 and CR-2 resolved; deferred none.
- **Evidence**: See Test Evidence above.
- **Follow-ups**: None.

## Agent Notes

- Load: [architecture §3, §3.1, §5, §13, §15–§18](../../Architecture/Tailor.architecture.md), [plan](../../Plans/Tailor.plan.md), this spec and [WU-000 spec](WU-000-repository-scaffold.spec.md).
- Use the `agent-customization` skill for frontmatter syntax if available.
- Do not touch `src/`, `tests/`, build props or workflows.

## Open Questions

None.
