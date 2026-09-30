# WU-001 ai-enablement

| Field | Value |
|---|---|
| ID | WU-001 |
| Title | ai-enablement |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/DotNetRepack.plan.md#m0-foundation--repo-bootstrap) |
| Status | Not started |
| Depends on | WU-000 |
| Parallel with | WU-002, WU-003, WU-006, WU-007, WU-100 |
| Target paths | `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/`, `.github/prompts/`, `.github/skills/`, `.github/agents/` |
| Size | M |
| Branch | `wu/001-ai-enablement` |

## Goal

Give AI coding agents a single, consistent source of repo conventions and a repeatable WU workflow (implement → verify → new spec). The files must reference the architecture and plan rather than duplicate them.

## Requirement Traceability

| Source | Section | Relevance |
|---|---|---|
| [Architecture](../../Architecture/DotNetRepack.architecture.md#18-repository-ai-enablement) | §18 Repository AI Enablement | File list |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#31-project-responsibilities-and-allowed-dependencies) | §3.1 | "Must not" boundaries to encode |
| [Architecture](../../Architecture/DotNetRepack.architecture.md#15-determinism) | §15 Determinism, [§17 Security](../../Architecture/DotNetRepack.architecture.md#17-security) | Rules to encode |
| [Plan](../../Plans/DotNetRepack.plan.md#how-agents-use-this-plan) | How Agents Use This Plan; M0 criterion 5 | Workflow, spec path convention |
| [RQ](../../Requirements/Repackage_tool_Requirements_v1.1.md) | §2.2 Out of Scope, §9 Input/Output Rules, §12 Non-Functional | Safety boundaries |

## Scope

**In**: the files in Deliverables, with valid frontmatter, cross-linked to architecture, plan and specs.

**Out**: code, CI (WU-002), test apps (WU-003), editing architecture/plan content, MCP server configuration.

## Deliverables

| Path | Frontmatter | Content (concise bullets, link instead of copy) |
|---|---|---|
| `AGENTS.md` | none | Repo map (`src/`, `tests/`, `build/`, `schemas/`, `Docs/*`, `spikes/`); build/test/format commands; conventions summary; WU workflow (plan steps 1–7); boundaries (§3.1 "Must not" column); determinism rules (§15); safety rules (never mutate input tree, no shell execution, no secrets in logs/artefacts, path confinement); Test Evidence block format; what agents must not edit |
| `.github/copilot-instructions.md` | none | Short pointer to `AGENTS.md` plus the 10 most important rules |
| `.github/instructions/csharp.instructions.md` | `applyTo: "**/*.cs"`, `description` | File-scoped namespaces, nullable, no `#pragma` suppressions without justification, `RelativePath`/`Diagnostic` usage, ordinal-ignore-case sorting, canonical JSON only through Core writer, `ArgumentList` for processes, async + `CancellationToken`, project boundary rules |
| `.github/instructions/tests.instructions.md` | `applyTo: "tests/**"`, `description` | xUnit v3 + MTP, naming `Method_condition_result`, Verify snapshot rules (`*.verified.*` committed, scrub paths), fixtures from `artifacts/testapps` via manifest, no network in unit tests, `dotnet test` filter syntax, Test Evidence recording |
| `.github/instructions/docs-specs.instructions.md` | `applyTo: "Docs/**/*.md"`, `description` | Spec template (header table → Open questions), AC as `- [ ] AC-n` (only the Verifier ticks), relative links, requirement citation style (`[RQ §n](…)`), ADR format and `Docs/Decisions/ADR-NNNN-<slug>.md` naming, spike report template |
| `.github/prompts/implement-work-unit.prompt.md` | `description`, `agent: agent`, input `${input:wu}` | Load plan + `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md` + cited sections; check deps `Done`; set status `In progress`; branch; implement; tests; zero warnings; format; Test Evidence; set `In review` |
| `.github/prompts/verify-work-unit.prompt.md` | `description`, `agent: agent`, input `${input:wu}` | Re-check every AC with evidence (reuse valid Test Evidence), tick `- [x]` only when proven, update plan status to `Done`, tick milestone criteria when met; report failed ACs |
| `.github/prompts/new-work-unit-spec.prompt.md` | `description`, `agent: agent`, input `${input:wu}` | Create the spec from the plan row using the M0 spec template; path and slug exactly as in the status table |
| `.github/skills/work-unit-workflow/SKILL.md` | `name: work-unit-workflow`, `description` | Status transitions, branch/PR naming, DoD checklist, when to write an ADR, Test Evidence protocol summary |
| `.github/skills/schema-change/SKILL.md` | `name: schema-change`, `description` | Model change → regenerate `schemas/*/v1/` via `schema export` → drift test → `schemaVersion` minor/major rules ([§6.1](../../Architecture/DotNetRepack.architecture.md#61-common-rules)) → update snapshots and docs |
| `.github/skills/test-apps/SKILL.md` | `name: test-apps`, `description` | How to run `build/Build-TestApps.ps1`, matrix and folder naming, `manifest.json` use, adding a new test app, cache key impact |
| `.github/agents/implementer.agent.md` | `description`, `tools` | Role: implement one WU using the implement prompt; must not tick ACs; must not edit architecture except with an ADR |
| `.github/agents/reviewer.agent.md` | `description`, `tools` (read/search/test only) | Role: Verifier; read-only on production code; ticks ACs and plan status |

## Design Notes

- Single source of truth: architecture = design, plan = sequencing/status, spec = WU contract. Instruction files state rules and link sections. No duplicated tables.
- Where WU-003 details (script parameters) do not exist yet, the `test-apps` skill cites [WU-003 spec](WU-003-test-app-suite.spec.md) and marks the section "update when WU-003 lands".
- The test-evidence protocol is a user-level skill that other contributors may not have. `AGENTS.md` and `work-unit-workflow` must include the Test Evidence block format so the repo is self-contained.
- Keep `copilot-instructions.md` under ~60 lines. Put longer guidance in scoped instruction files.
- Commands must match WU-000 exactly (`DotNetRepack.slnx`, `dotnet test --solution …`).
- Boundaries to state: never modify `Docs/Requirements/**`; spikes code is never referenced from `src/`; no new package without CPM entry; no timestamps/GUIDs/machine paths in canonical output.

## Acceptance Criteria

- [ ] AC-1 All 13 files in Deliverables exist at the exact paths.
- [ ] AC-2 Every `*.instructions.md` has YAML frontmatter with `applyTo` and `description`; globs are `**/*.cs`, `tests/**`, `Docs/**/*.md`.
- [ ] AC-3 Every `SKILL.md` has frontmatter `name` equal to its folder name and a non-empty `description`.
- [ ] AC-4 Every `*.prompt.md` and `*.agent.md` has frontmatter with `description`; YAML parses (e.g. `ConvertFrom-Yaml` or any YAML linter).
- [ ] AC-5 `implement-work-unit.prompt.md` references `Docs/Plans/DotNetRepack.plan.md` and the path convention `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md` (M0 milestone criterion 5).
- [ ] AC-6 `AGENTS.md` and each instruction file link to `Docs/Architecture/DotNetRepack.architecture.md`; all relative links resolve (link check script or `markdown-link-check`).
- [ ] AC-7 Commands in `AGENTS.md` run successfully as written (`dotnet build DotNetRepack.slnx -c Release -warnaserror`, `dotnet test --solution DotNetRepack.slnx -c Release`, `dotnet format DotNetRepack.slnx --verify-no-changes`).
- [ ] AC-8 Reviewer confirms no statement contradicts the architecture (project boundaries §3.1, exit codes §13, artefact rules §5, determinism §15). Contradictions found = 0, recorded in the PR.
- [ ] AC-9 `reviewer.agent.md` tool list excludes file-editing tools other than those needed to tick ACs and plan status.

## Test Requirements

- No code tests. Validation is by commands in AC-4, AC-6, AC-7.
- Record a Test Evidence block for AC-7 (reuse WU-000/CI evidence if the functional-state fingerprint matches; Markdown-only changes do not invalidate it).

## Definition of Done

- All AC ticked by the Verifier; solution still builds with zero warnings and tests green; format clean.
- Plan status updated (`In review` → `Done`).

## Agent Notes

- Load: [architecture §3, §3.1, §5, §13, §15–§18](../../Architecture/DotNetRepack.architecture.md), [plan](../../Plans/DotNetRepack.plan.md), this spec and [WU-000 spec](WU-000-repository-scaffold.spec.md).
- Use the `agent-customization` skill for frontmatter syntax if available.
- Do not touch `src/`, `tests/`, build props or workflows.

## Open Questions

- Should `reviewer.agent.md` be allowed to run the full test-app matrix, or only reuse CI evidence?
- Whether to add an `architecture-change` skill (ADR + architecture edit) now or when the first ADR lands (spikes).
