# WU-001 ai-enablement

| Field | Value |
| --- | --- |
| ID | WU-001 |
| Title | ai-enablement |
| Milestone | [M0 Foundation & Repo Bootstrap](../../Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) |
| Status | Done |
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
- [x] AC-4 Every `*.prompt.md` and `*.agent.md` has frontmatter with `description`; YAML parses. **Verified:** pinned `powershell-yaml` 0.4.12 parsed all 24 customization mappings, including six prompts and seven agents.
- [x] AC-5 `implement-work-unit.prompt.md` references `Docs/Plans/Tailor.plan.md` and the path convention `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md` (M0 milestone criterion 5).
- [x] AC-6 `AGENTS.md` and each instruction file link to `Docs/Architecture/Tailor.architecture.md`; all relative links resolve. **Verified:** all 17 relative links and heading anchors resolve.
- [x] AC-7 Commands in `AGENTS.md` run successfully as written. **Verified:** exact solution format, Release build and full test commands passed at the current functional state; see evidence below.
- [x] AC-8 Reviewer confirms no statement contradicts the architecture (project boundaries §3.1, exit codes §13, artefact rules §5, determinism §15). **Verified:** the full current-HEAD review's sole finding, CR-3, was withdrawn by an approving targeted re-review at the same HEAD; the combined review evidence establishes no outstanding architecture-statement contradiction. See the review chain below.
- [x] AC-9 `verifier.agent.md` tool list excludes file-editing tools other than those needed to tick ACs and plan status. **Verified:** `edit` is the only editing tool; explicit constraints restrict it to verification documents and keep implementation read-only.

## Test Requirements

- No code tests. Validation is by commands in AC-4, AC-6, AC-7.
- Record a Test Evidence block for AC-7 (reuse WU-000/CI evidence if the functional-state fingerprint matches; Markdown-only changes do not invalidate it).
- The Verifier uses available CI evidence first, then relevant tests for uncovered checks; run the full test-app matrix only when required by the affected checks.

## Test Evidence

### Current-HEAD verification at `800cc9d`

Verified HEAD: `800cc9d328489e22231aecdad37c6a00614eb30e`, branch `wu/001-ai-enablement`; local `main` merge-base: `d3e90c08ee8e7a96df86480fb88ec4104753c92d`. No remote-tracking baseline or PR was used. WU-000 is `Done`. The checks below include the existing dirty WU spec; the documentation fingerprint identifies the checked tree before this verification-only evidence/checkbox append. Deliverables, guidance and scripts were not changed.

All nine ACs pass. AC-1 through AC-7 and AC-9 have executable/inspection evidence below; AC-8 and the current-HEAD review gate are satisfied by the combined review reports, not by an older approval or a review-gate waiver.

#### Review chain and scope

- **Earlier range**: the original review range was `6ba61cd3..02797682`. Earlier approval through `027976822c978d28e7e1337b600e40c16b5f5661` is preserved historically and is not treated as approval of later HEAD.
- **Full current-HEAD report**: at `800cc9d328489e22231aecdad37c6a00614eb30e`, Tailor Code Reviewer returned **Request changes** with the sole finding **CR-3 BLOCKER**. The finding concerned [the setup script](../../../.github/skills/test-evidence/scripts/Initialize-PowerShellDependencies.ps1) creating/downloading into its cache without rejecting reparse-point components or verifying resolved repository confinement before writes.
- **User disposition**: user-controlled symlink/junction relocation of the git-ignored `artifacts/local-tools` cache was accepted as non-blocking. This disposition did not waive the overall review gate.
- **Targeted current-HEAD report**: at the same `800cc9d328489e22231aecdad37c6a00614eb30e`, the reviewer returned **"Approve for this targeted re-review - CR-3 is withdrawn"**, with no outstanding finding from that targeted review. Rationale: a fixed per-checkout cache path with no cache-path argument; user-created links are user-controlled relocation, not arbitrary traversal; the architecture confinement rule concerns spec paths; WU-001 has no link-rejection AC.
- **Combined result**: the full current-HEAD report plus the same-HEAD withdrawal/targeted approval clears its sole finding and satisfies WU-001's review gate and AC-8. No single full-report `Approve` beyond these two reports is claimed. No code fix, dependency download or safety exploit probe was performed.

The branch at later HEAD `800cc9d` also contains unrelated changes, including `Directory.Packages.props`, CLI/tests, other WU specs and plans. These are outside WU-001's deliverable contract and were neither implemented nor certified by this verification. Their presence in the broader branch is retained as a scope distinction, not an unmet WU-001 AC or a remaining finding in the supplied review chain. Completion is for WU-001 only, not approval of all branch work. No PR exists.

WU-001 is `Done` in this spec and the master plan. All nine criteria are ticked; verification/status prose is the only new change, so the tested functional and customization inputs remain unchanged. The legacy spec has no `## Steps` checklist or `(T)` criteria; traceability requires no AC test traits for it.

#### Final customization-contract checks

- **State**: `d008b6dc99f60e5013a32069ad29e5a8d344638e72b363ec6d6b6211a7ccdbe9` (`167` files; docs included: yes; exclusions: none; before completion-only edits)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; .NET SDK 10.0.401; Git 2.55.0.windows.5; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; root E:\DotNetTailor; powershell-yaml 0.4.12 from the existing ignored repo cache; cache SHA256 73778fd1212c55da127454c923064da91a04dd248da50500cae05a84c6bc97ef (14 files)`
- **Impact**: `WU-001 deliverables, frontmatter consumers, instruction links and verifier constraints; selected WU traceability`
- **Selected checks**: `AC-1/AC-2/AC-3/AC-4/AC-5/AC-6/AC-9, traceability and whitespace`
- **Excluded checks**: `setup/download and unrelated branch work; solution checks reused from the exact matching 63-file functional state below`
- **Command**: repository root; execute the unchanged full contract-validation block recorded below:

```powershell
$ErrorActionPreference = 'Stop'
$spec = Get-Content -LiteralPath Docs\Specs\M0\WU-001-ai-enablement.spec.md -Raw
$heading = '#### AC-1/AC-2/AC-3/AC-4/AC-5/AC-6/AC-9, traceability and whitespace'
$section = ($spec -split [regex]::Escape($heading),2)[1] -split '#### Post-edit persistence checks',2
$block = [regex]::Match($section[0], '```powershell\r?\n(.*?)\r?\n```', 'Singleline')
if (-not $block.Success) { throw 'Recorded customization validation command missing.' }
& ([scriptblock]::Create($block.Groups[1].Value))
```

- **Result**: `pass; exit 0; 26 exact paths; three exact globs; eight skill names/descriptions; 24 YAML mappings; 17 relative links/anchors; prompt convention and verifier tools confirmed; traceability reports nine legacy ACs and no missing test traits or orphans; whitespace clean; documentation fingerprint unchanged after checks`
- **Evidence source**: `run by verifier; AC-7 format/build/test records below reused after freshly matching functional fingerprint 30ddc8a382266149c7fe641c87731515b620539a30a7de7519b47607f4ace802 (63 files, docs excluded, no exclusions) and environment`
- **Rerun reason**: `establish final contract checks against the current documentation fingerprint; no solution rerun because exact-state evidence already covers it`

#### Completion persistence

- **State**: `a62ea1c352193d697110c40b301028088c8146286c69660ecee5517010fc9d12` (`167` files; docs included: yes; exclusions: none; before this evidence-only append); functional fingerprint unchanged at `30ddc8a382266149c7fe641c87731515b620539a30a7de7519b47607f4ace802` (`63` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; .NET SDK 10.0.401; Git 2.55.0.windows.5; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; root E:\DotNetTailor`
- **Impact**: `selected WU's verification records/checkboxes and master-plan WU-001 status row`
- **Selected checks**: `all nine ticks; both Done statuses; Completion; unchanged reviewed HEAD; traceability; whitespace; CRLF/final newline`
- **Excluded checks**: `functional and customization suites already covered above; completion-only prose/status edits do not alter their inputs; unrelated branch work not certified`
- **Command**: repository root:

```powershell
$ErrorActionPreference = 'Stop'
& .github\skills\test-evidence\scripts\Get-FunctionalState.ps1
& .github\skills\test-evidence\scripts\Get-FunctionalState.ps1 -IncludeDocumentation
& .github\skills\planning-artifacts\scripts\Test-Traceability.ps1 Docs\Specs\M0\WU-001-ai-enablement.spec.md
if ($LASTEXITCODE -ne 0) { throw 'Completion traceability failed.' }
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Completion whitespace failed.' }
$spec = Get-Content Docs\Specs\M0\WU-001-ai-enablement.spec.md -Raw
$ac = ($spec -split '## Acceptance Criteria',2)[1] -split '## Test Requirements',2
$checked = @([regex]::Matches($ac[0], '(?m)^- \[x\] (AC-\d+)') | ForEach-Object { $_.Groups[1].Value })
if (($checked -join ',') -ne 'AC-1,AC-2,AC-3,AC-4,AC-5,AC-6,AC-7,AC-8,AC-9' -or $ac[0] -match '(?m)^- \[ \] AC-') { throw 'All nine criteria must be ticked.' }
if ($spec -notmatch '\| Status \| Done \|' -or $spec -notmatch '(?m)^## Completion\r?$') { throw 'Completion/status missing.' }
$plan = Get-Content Docs\Plans\Tailor.plan.md -Raw
if ($plan -notmatch '(?m)^\| WU-001 \|.*\| Done \|\r?$') { throw 'Master-plan status mismatch.' }
foreach ($text in @($spec,$plan)) { if ($text -match '(?<!\r)\n' -or -not $text.EndsWith("`r`n")) { throw 'CRLF/final newline failed.' } }
$head = git rev-parse HEAD
if ($head -ne '800cc9d328489e22231aecdad37c6a00614eb30e') { throw 'Reviewed HEAD changed.' }
'PASS: all nine ACs ticked; spec/plan Done; current review chain and Completion saved; CRLF and final newline preserved.'
git --no-pager status --short
git --no-pager diff -- Docs/Plans/Tailor.plan.md
```

- **Result**: `pass; exit 0; all nine ACs ticked; both statuses Done; Completion saved; traceability passes for nine legacy ACs; whitespace and CRLF/final newline pass; reviewed HEAD unchanged; only selected spec and master plan dirty; master-plan diff changes only WU-001 status`
- **Evidence source**: `run by verifier`
- **Rerun reason**: `validate the newly recorded completion state after AC-8/status edits`

#### AC-7 format

- **State**: `30ddc8a382266149c7fe641c87731515b620539a30a7de7519b47607f4ace802` (`63` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; .NET SDK 10.0.401; PowerShell 7.6.6; repository dependencies already restored; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; root E:\DotNetTailor`
- **Impact**: `documented solution formatting command; current functional inputs include three PowerShell dependency/validation files absent from the older 60-file state`
- **Selected checks**: `exact solution-format command required by AC-7`
- **Excluded checks**: `Markdown/PowerShell formatting and test-app matrix; dotnet format cannot observe those file formats`
- **Command**: `dotnet format DotNetTailor.slnx --verify-no-changes` (repository root; preceded and followed by `& .github\skills\test-evidence\scripts\Get-FunctionalState.ps1`)
- **Result**: `pass; exit 0; no formatter output; functional fingerprint unchanged after the complete format/build/test sequence`
- **Evidence source**: `run by verifier at current HEAD`
- **Rerun reason**: `older AC-7 state e6e86d713d8ac527fd63c37673d6cbe94c86ff8bcb70988b393e0e0ffddf32e3 (60 files) does not match the current 63-file state`

#### AC-7 build

- **State**: `30ddc8a382266149c7fe641c87731515b620539a30a7de7519b47607f4ace802` (`63` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; .NET SDK 10.0.401; PowerShell 7.6.6; repository dependencies already restored; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; root E:\DotNetTailor`
- **Impact**: `documented Release solution command; all current solution projects`
- **Selected checks**: `all 29 projects; AC-7 requires the full solution build with warnings as errors`
- **Excluded checks**: `test-app publication and network checks; not required by this criterion`
- **Command**: `dotnet build DotNetTailor.slnx -c Release -warnaserror` (repository root; within the fingerprinted format/build/test sequence)
- **Result**: `pass; exit 0; 29 projects built; 0 warnings, 0 errors; restore reported all projects up to date; post-sequence functional fingerprint unchanged`
- **Evidence source**: `run by verifier at current HEAD`
- **Rerun reason**: `older AC-7 functional fingerprint does not match`

#### AC-7 tests

- **State**: `30ddc8a382266149c7fe641c87731515b620539a30a7de7519b47607f4ace802` (`63` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; .NET SDK 10.0.401; xUnit v3/Microsoft Testing Platform; PowerShell 7.6.6; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; root E:\DotNetTailor`
- **Impact**: `documented solution test command; current tests and their project dependencies`
- **Selected checks**: `all 15 solution test assemblies; AC-7 explicitly requires the exact full test command`
- **Excluded checks**: `test-app matrix and opt-in network checks; not required by this criterion`
- **Command**: `dotnet test --solution DotNetTailor.slnx -c Release` (repository root; within the fingerprinted format/build/test sequence)
- **Result**: `pass; exit 0; 33 succeeded, 0 failed, 0 skipped; all 15 assemblies passed; post-sequence functional fingerprint unchanged`
- **Evidence source**: `run by verifier at current HEAD`
- **Rerun reason**: `older AC-7 functional fingerprint does not match`

#### AC-1/AC-2/AC-3/AC-4/AC-5/AC-6/AC-9, traceability and whitespace

- **State**: `0abddeeddbd431b1d1b4af150247404c1d83fb057e5f0b22362433668c251ba3` (`167` files; docs included: yes; exclusions: none; before verification-only edits)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; .NET SDK 10.0.401; Git 2.55.0.windows.5; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; root E:\DotNetTailor; powershell-yaml 0.4.12 imported offline from ignored artifacts/local-tools/powershell-modules/powershell-yaml/0.4.12; cache SHA256 73778fd1212c55da127454c923064da91a04dd248da50500cae05a84c6bc97ef (14 files)`
- **Impact**: `26 delivered guidance files; customization YAML consumers; instruction links and target heading anchors; selected WU's legacy traceability contract`
- **Selected checks**: `exact delivered paths/globs; all eight skill names; all 24 parsed frontmatters; prompt convention; every relative instruction/AGENTS link; constrained verifier tools; traceability and whitespace`
- **Excluded checks**: `setup/download and unsafe-destination probes; CR-3 remains open without a code fix; Python YAML and historical parser evidence not used; runtime/test-app suites cannot validate these documentation contracts`
- **Command**: repository root, the following PowerShell block:

```powershell
$ErrorActionPreference = 'Stop'
& .github\skills\test-evidence\scripts\Get-FunctionalState.ps1 -IncludeDocumentation
$root = (Get-Location).Path
$cache = Join-Path $root 'artifacts\local-tools\powershell-modules\powershell-yaml\0.4.12'
for ($item = Get-Item -LiteralPath $cache; $null -ne $item; $item = $item.Parent) { if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse point in existing cache ancestry: $($item.FullName)" } }
$cacheFiles = @(Get-ChildItem -LiteralPath $cache -Recurse -File | Sort-Object FullName)
$cacheManifest = ($cacheFiles | ForEach-Object { '{0}`t{1}' -f [IO.Path]::GetRelativePath($cache, $_.FullName), (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }) -join "`n"
$cacheHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($cacheManifest))).ToLowerInvariant()
"Cache SHA256: $cacheHash; files: $($cacheFiles.Count); cache ancestry has no reparse points."
pwsh -NoProfile -File .github\skills\test-evidence\scripts\Test-YamlFrontmatter.ps1
if ($LASTEXITCODE -ne 0) { throw 'Pinned YAML validation failed.' }
Import-Module (Join-Path $cache 'powershell-yaml.psd1') -Force -ErrorAction Stop
$spec = Get-Content -LiteralPath Docs\Specs\M0\WU-001-ai-enablement.spec.md -Raw
$deliverables = ($spec -split '## Deliverables',2)[1] -split '## Design Notes',2
$paths = @([regex]::Matches($deliverables[0], '(?m)^\| `([^`]+)` \|') | ForEach-Object { $_.Groups[1].Value })
if ($paths.Count -ne 26) { throw 'Deliverable count must be 26.' }
foreach ($path in $paths) { if (-not (Test-Path -LiteralPath ($path.Replace('/', '\')) -PathType Leaf)) { throw "Missing deliverable: $path" } }
'AC-1 PASS: 26 exact delivered paths.'
$metadata = @{}
$files = @(Get-ChildItem .github\instructions,.github\prompts,.github\skills,.github\agents -Recurse -File | Where-Object { $_.Name -eq 'SKILL.md' -or $_.Name -match '\.(instructions|prompt|agent)\.md$' })
foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $front = [regex]::Match($content, '\A---\r?\n(.*?)\r?\n---(?:\r?\n|$)', 'Singleline')
    if (-not $front.Success) { throw "Missing frontmatter: $file" }
    $metadata[$file.FullName] = powershell-yaml\ConvertFrom-Yaml $front.Groups[1].Value
}
$globs = @{ 'csharp.instructions.md' = '**/*.cs'; 'tests.instructions.md' = 'tests/**'; 'docs-specs.instructions.md' = 'Docs/**/*.md' }
$instructions = @($files | Where-Object Name -Like '*.instructions.md')
if ($instructions.Count -ne 3) { throw 'Expected three instructions.' }
foreach ($file in $instructions) { if ($metadata[$file.FullName]['applyTo'] -cne $globs[$file.Name]) { throw "Incorrect glob: $file" } }
'AC-2 PASS: three exact globs and parsed descriptions.'
$skills = @($files | Where-Object Name -EQ 'SKILL.md')
if ($skills.Count -ne 8) { throw 'Expected eight skills.' }
'AC-3 PASS: eight parsed skill names/descriptions validated by pinned helper.'
$prompts = @($files | Where-Object Name -Like '*.prompt.md')
$agents = @($files | Where-Object Name -Like '*.agent.md')
if ($prompts.Count -ne 6 -or $agents.Count -ne 7) { throw 'Expected six prompts and seven agents.' }
'AC-4 PASS: six prompts and seven agents; all 24 mappings parsed offline.'
$prompt = Get-Content .github\prompts\implement-work-unit.prompt.md -Raw
if (-not $prompt.Contains('Docs/Plans/Tailor.plan.md') -or -not $prompt.Contains('Docs/Specs/<Milestone>/<ID>-<slug>.spec.md')) { throw 'AC-5 convention mismatch.' }
'AC-5 PASS: plan reference and exact spec convention.'
$linkCount = 0
foreach ($file in @((Get-Item AGENTS.md)) + $instructions) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if (-not $content.Contains('Docs/Architecture/Tailor.architecture.md')) { throw "Architecture reference absent: $file" }
    foreach ($match in [regex]::Matches($content, '\[[^\]]*\]\(([^)\r\n]+)\)')) {
        $target = $match.Groups[1].Value.Trim().Trim('<','>')
        if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:') { continue }
        $parts = $target.Split('#',2)
        $path = [Uri]::UnescapeDataString($parts[0]).Replace('/', '\')
        $destination = if ($path) { [IO.Path]::GetFullPath((Join-Path $file.DirectoryName $path)) } else { $file.FullName }
        if (-not (Test-Path -LiteralPath $destination)) { throw "Broken link: $file -> $target" }
        if ($parts.Count -eq 2) {
            $anchors = @(); $counts = @{}
            foreach ($heading in [regex]::Matches((Get-Content -LiteralPath $destination -Raw), '(?m)^#{1,6}\s+(.+?)\s*#*\s*$')) {
                $slug = ([regex]::Replace($heading.Groups[1].Value.ToLowerInvariant(), '[^\w\- ]', '')).Replace(' ', '-')
                $count = if ($counts.ContainsKey($slug)) { $counts[$slug] } else { 0 }
                $counts[$slug] = $count + 1
                $anchors += if ($count) { "$slug-$count" } else { $slug }
            }
            if ([Uri]::UnescapeDataString($parts[1]) -cnotin $anchors) { throw "Broken anchor: $file -> $target" }
        }
        $linkCount++
    }
}
"AC-6 PASS: $linkCount relative links and anchors."
$verifier = Get-Item .github\agents\verifier.agent.md
$tools = @($metadata[$verifier.FullName]['tools'])
if (($tools -join ',') -cne 'read,search,execute,edit,agent') { throw 'AC-9 unexpected tools.' }
$content = Get-Content -LiteralPath $verifier.FullName -Raw
if (-not $content.Contains('never edit production code, tests, fixtures or build configuration') -or -not $content.Contains('Use editing only to tick proven steps and criteria')) { throw 'AC-9 edit boundary absent.' }
'AC-9 PASS: one editing tool, constrained to verification documents; implementation read-only.'
& .github\skills\planning-artifacts\scripts\Test-Traceability.ps1 Docs\Specs\M0\WU-001-ai-enablement.spec.md
if ($LASTEXITCODE -ne 0) { throw 'Traceability failed.' }
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Whitespace failed.' }
& .github\skills\test-evidence\scripts\Get-FunctionalState.ps1 -IncludeDocumentation
```

- **Result**: `pass; exit 0; 26 exact paths; three exact instruction globs; eight skill names/descriptions; 24 YAML mappings (six prompts, seven agents, three instructions, eight skills); 17 relative links/anchors; AC-5 convention and AC-9 tool restrictions confirmed; traceability reports nine legacy criteria with no (T) obligations or orphans; git diff --check passed; documentation fingerprint unchanged after checks; existing cache ancestry has no reparse points`
- **Evidence source**: `run by verifier at current HEAD; no inherited executable evidence reused`
- **Rerun reason**: `current documentation state differs from historical and bounded YAML fingerprints; current HEAD needs individual AC evidence, and older link counts are not current results`

#### Post-edit persistence checks

The record below predates the user disposition and reviewer withdrawal of CR-3. Its assertions about an open blocker and `In review` describe that tested documentation state, not current completion.

- **State**: `3890cddabf39a529623648c8e8e0407f3f44c785c952047be723b7b0308372d8` (`167` files; docs included: yes; exclusions: none; before this evidence-only append); functional state remains `30ddc8a382266149c7fe641c87731515b620539a30a7de7519b47607f4ace802` (`63` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; .NET SDK 10.0.401; Git 2.55.0.windows.5; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; root E:\DotNetTailor`
- **Impact**: `verification-only WU spec edits; master-plan status inspected without modification`
- **Selected checks**: `persistent individual AC states, both In review statuses, open CR-3, historical completion annotation, traceability, whitespace and source line endings`
- **Excluded checks**: `solution and YAML checks were not repeated after checkbox/evidence edits; their tested functional/customization inputs are unchanged`
- **Command**: repository root:

```powershell
$ErrorActionPreference = 'Stop'
& .github\skills\test-evidence\scripts\Get-FunctionalState.ps1
& .github\skills\test-evidence\scripts\Get-FunctionalState.ps1 -IncludeDocumentation
& .github\skills\planning-artifacts\scripts\Test-Traceability.ps1 Docs\Specs\M0\WU-001-ai-enablement.spec.md
if ($LASTEXITCODE -ne 0) { throw 'Post-edit traceability failed.' }
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Post-edit whitespace failed.' }
$text = Get-Content Docs\Specs\M0\WU-001-ai-enablement.spec.md -Raw
$acSection = ($text -split '## Acceptance Criteria',2)[1] -split '## Test Requirements',2
$checked = @([regex]::Matches($acSection[0], '(?m)^- \[x\] (AC-\d+)') | ForEach-Object { $_.Groups[1].Value })
$unchecked = @([regex]::Matches($acSection[0], '(?m)^- \[ \] (AC-\d+)') | ForEach-Object { $_.Groups[1].Value })
if (($checked -join ',') -ne 'AC-1,AC-2,AC-3,AC-4,AC-5,AC-6,AC-7,AC-9' -or ($unchecked -join ',') -ne 'AC-8') { throw 'Unexpected AC states.' }
if ($text -notmatch '\| Status \| In review \|') { throw 'Spec status changed.' }
$plan = Get-Content Docs\Plans\Tailor.plan.md -Raw
if ($plan -notmatch '(?m)^\| WU-001 \|.*\| In review \|\r?$') { throw 'Plan status changed.' }
if (-not $text.Contains('CR-3 BLOCKER') -or -not $text.Contains('## Historical Completion')) { throw 'Missing open blocker or historical completion annotation.' }
if ($text -match '(?<!\r)\n') { throw 'Working-tree CRLF policy failed.' }
if (-not $text.EndsWith("`r`n")) { throw 'Missing final CRLF.' }
"PASS: checked $($checked -join ', '); unchecked $($unchecked -join ', '); both statuses In review; CR-3 open; CRLF and final newline preserved."
git --no-pager status --short
git --no-pager diff --stat
git rev-parse HEAD
```

- **Result**: `pass; exit 0; eight ACs checked and only AC-8 unchecked; both statuses In review; CR-3 open; traceability/whitespace pass; CRLF/final newline preserved; only the WU spec is dirty; HEAD unchanged. An initial inline status check failed (exit 1) because its end-of-line regex omitted CRLF support; the master-plan status was unchanged. The corrected CRLF-aware check above passed without intervening repository edits.`
- **Evidence source**: `run by verifier`
- **Rerun reason**: `confirm persistence after verification edits; correct the verifier's CRLF-unaware status probe, not a repository defect`

### Historical verification at `02797682`

These records retain their original parser provenance and do not verify the current PowerShell YAML tooling. All nine criteria were reassessed without relying on their previous ticks. The review baseline was the merge-base with local `main`, `d3e90c08`; remote-tracking branches were excluded. Documentation fingerprints below identify the checked implementation tree before verification-only evidence/status edits.

#### AC-7 build

- **State**: `e6e86d713d8ac527fd63c37673d6cbe94c86ff8bcb70988b393e0e0ffddf32e3` (`60` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64; .NET SDK 10.0.401; PowerShell 7.6.6; dependencies restored; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset`
- **Impact**: `AI guidance and helper scripts; documented solution command`
- **Selected checks**: `all 29 solution projects; AC-7 requires the exact documented build`
- **Excluded checks**: `test-app publication and network checks; neither is required by WU-001`
- **Command**: `dotnet build DotNetTailor.slnx -c Release -warnaserror` (repository root)
- **Result**: `pass; exit 0; 29 projects built; 0 warnings, 0 errors`
- **Evidence source**: `run by verifier`
- **Rerun reason**: `independent verification requested; historical fingerprint does not match`

#### AC-7 tests

- **State**: `e6e86d713d8ac527fd63c37673d6cbe94c86ff8bcb70988b393e0e0ffddf32e3` (`60` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64; .NET SDK 10.0.401; xUnit v3/Microsoft Testing Platform; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset`
- **Impact**: `documented solution test command; no test or production changes`
- **Selected checks**: `all 15 solution test assemblies; exact AC-7 command`
- **Excluded checks**: `test-app matrix and opt-in network checks; unrelated to customization guidance`
- **Command**: `dotnet test --solution DotNetTailor.slnx -c Release` (repository root)
- **Result**: `pass; exit 0; 33 succeeded, 0 failed, 0 skipped; all 15 assemblies passed`
- **Evidence source**: `run by verifier; VS Code runTests independently also reported 33 passed, 0 failed`
- **Rerun reason**: `independent verification requested; VS Code test results alone do not prove the documented CLI command`

#### AC-7 format

- **State**: `e6e86d713d8ac527fd63c37673d6cbe94c86ff8bcb70988b393e0e0ffddf32e3` (`60` files; docs included: no; exclusions: none)
- **Environment**: `Windows x64; .NET SDK 10.0.401; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset`
- **Impact**: `documented source-formatting gate`
- **Selected checks**: `exact AC-7 solution formatting command`
- **Excluded checks**: `Markdown formatting; not covered by dotnet format and no Markdown formatter is configured`
- **Command**: `dotnet format DotNetTailor.slnx --verify-no-changes` (repository root)
- **Result**: `pass; exit 0; no output`
- **Evidence source**: `run by verifier before build/tests`
- **Rerun reason**: `independent verification requested; no exact-state historical format evidence`

#### Customization contracts

- **State**: `b25b6d58b638dcb7c3bf3c2250ca440990da0b053e44e48d2808d45efa33744f` (`164` files; docs included: yes; exclusions: none)
- **Environment**: `Windows; PowerShell 7.6.6; Python 3.14.7; PyYAML 6.0.3 in an isolated session environment`
- **Impact**: `all WU-001 deliverables and instruction links; verifier tool restrictions`
- **Selected checks**: `AC-1 exact 26 paths; AC-2 exact three instruction globs; AC-3 eight skill names; AC-4 real YAML parser for 24 frontmatters; AC-5 plan/spec convention; AC-6 relative targets and heading anchors; AC-9 tools and edit restrictions`
- **Excluded checks**: `runtime behaviour cannot validate Markdown frontmatter or link contracts`
- **Command**: `& '<session>\files\wu001-yaml-env\Scripts\python.exe' '<session>\files\Verify-WU001.py'` (repository root; session-local validator SHA256 `16d9b94d402628893b8c1ee28ea3944fc35aa010b2977a9d37fd43a8f9434bf1`)
- **Result**: `pass; exit 0; 26 deliverables, 24 parsed mappings (7 agents, 6 prompts, 3 instructions, 8 skills), 16 relative links and anchors resolved; verifier tools and explicit read-only implementation boundary confirmed`
- **Evidence source**: `run by verifier; temporary parser environment is not a repository dependency`
- **Rerun reason**: `all criteria reset; older documentation checks did not cover this exact tree`

#### Traceability and whitespace

- **State**: `b25b6d58b638dcb7c3bf3c2250ca440990da0b053e44e48d2808d45efa33744f` (`164` files; docs included: yes; exclusions: none)
- **Environment**: `Windows; PowerShell 7.6.6; Git`
- **Impact**: `WU-001 acceptance criteria and verification-only Markdown edits`
- **Selected checks**: `required traceability gate and whitespace validation`
- **Excluded checks**: `AC test traits are not required: this legacy WU explicitly specifies inspection/command validation and no code tests`
- **Command**: `.github\skills\planning-artifacts\scripts\Test-Traceability.ps1 Docs\Specs\M0\WU-001-ai-enablement.spec.md`; `git diff --check` (repository root)
- **Result**: `pass; both exit 0; nine legacy criteria, no missing test-method traits or orphans`
- **Evidence source**: `run by verifier`
- **Rerun reason**: `fresh verification requested`

### Historical evidence

The following records are retained for provenance only; they do not verify the current PowerShell YAML tooling.

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

### Historical documentation follow-up validation

- **State**: `4f211ded1dddc048212c536130b20263eb1d2f22755c812fcd0f1a379fec47a8` (`164` files; docs included: yes; exclusions: none)
- **Environment**: `Windows; PowerShell 7.6.6; Python 3.14.7; PyYAML 6.0.3 in an isolated, subsequently removed session environment`
- **Impact**: `Agent guidance, WU-001 contract and architecture inventory; no runtime changes`
- **Selected checks**: `exact three-file scope; deliverable count/existence; links and anchors; unchanged checkboxes/status; inventory-only architecture; CRLF/whitespace; real YAML parsing across all customization frontmatters`
- **Excluded checks**: `Build, runtime tests and test-app matrix cannot observe this Markdown-only follow-up; historical build/test/format passes above are preserved without claiming a rerun`
- **Command**: `& '<session>\files\Validate-WU001Followup.ps1'`; `& '<session>\files\yaml-validation-env\Scripts\python.exe' '<session>\files\Validate-Frontmatter.py'`; `git diff --check` (repository root; `<session>` denotes the temporary session directory supplied for validation)
- **Result**: `pass — exit 0; exactly three changed files; 26 deliverables; nine skill links/anchors resolved; 24 YAML frontmatters parsed (7 agents, 6 prompts, 3 instructions, 8 skills)`
- **Evidence source**: `follow-up checks run by implementer; scoped validator rerun by verifier; YAML parser result reused from implementer's record`
- **Rerun reason**: `new documentation checks and the reviewer's initial request uncovered the need for real YAML parser evidence; the recorded fingerprint identifies the implementation tree validated before verifier-only WU/plan status and evidence edits, which did not alter delivered customization guidance`

### PowerShell YAML tooling evidence

The dependency is `powershell-yaml` `0.4.12`, resolved with `Find-Module -Name powershell-yaml -Repository PSGallery` from `https://www.powershellgallery.com/api/v2`; package source: `https://www.powershellgallery.com/packages/powershell-yaml/0.4.12`. Downloaded contents reside only in ignored `artifacts/local-tools/powershell-modules`. Guidance and executable helpers are in the [test-evidence skill](../../../.github/skills/test-evidence/SKILL.md#yaml-validation).

The records below identify the tested implementation tree before this evidence-only append. No inherited evidence is reused for these helpers. HEAD: `027976822c978d28e7e1337b600e40c16b5f5661`; working branch: `wu/001-ai-enablement`. This is an implementation handoff, not reviewer approval or verification. WU-001 remains `In review`; acceptance criteria remain unticked.

#### Initial dependency download and import

- **State**: `6283d3a2d283c790464c861b38b928233a230875cddc9f697080f1dac0025f4c` (`166` files; docs included: yes; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; PowerShellGet Save-Module; PSGallery; .NET SDK 10.0.401; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset`
- **Impact**: `new pinned PowerShell dependency manifest and explicit setup script; no runtime projects or CI dependencies`
- **Selected checks**: `real download, pinned cache import and ConvertFrom-Yaml mapping parse`
- **Excluded checks**: `.NET build/tests/format and test-app matrix cannot observe PowerShell YAML tooling`
- **Command**: repository root:

```powershell
& .github/skills/test-evidence/scripts/Get-FunctionalState.ps1 -IncludeDocumentation
& .github/skills/test-evidence/scripts/Initialize-PowerShellDependencies.ps1
Get-Command ConvertFrom-Yaml | Select-Object Name, Source, Version
ConvertFrom-Yaml 'description: valid'
```

- **Result**: `pass; exit 0; powershell-yaml 0.4.12 downloaded/imported; ConvertFrom-Yaml returned a mapping`
- **Evidence source**: `run by implementer`
- **Rerun reason**: `new executable helpers; no reusable inherited evidence`

#### Final YAML behaviour checks

- **State**: `e52355b0595a98e9ef207b43beb498ac4a0c98e042ecfd21e71aefd775ca8792` (`167` files; docs included: yes; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; powershell-yaml 0.4.12 from repo-local PSGallery cache; .NET SDK 10.0.401; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset; each validator invocation uses a fresh -NoProfile host`
- **Impact**: `dependency setup and frontmatter validator; AGENTS.md and test-evidence guidance; all four customization directories are indirect consumers`
- **Selected checks**: `cached setup/import; current 24 frontmatters; valid mapping; malformed YAML rejection; actionable missing dependency with no cache creation`
- **Excluded checks**: `.NET runtime suites, solution build/format and matrix cannot observe these helpers; full WU link/glob verification remains with the Verifier`
- **Command**: repository root; ignored throwaway inputs were `valid.prompt.md` with frontmatter `description: "Probe: valid YAML"` and `tools: [read, search]`, and `malformed.prompt.md` with `description: [unterminated`, each enclosed by `---`:

```powershell
& .github/skills/test-evidence/scripts/Get-FunctionalState.ps1 -IncludeDocumentation
pwsh -NoProfile -File .github/skills/test-evidence/scripts/Initialize-PowerShellDependencies.ps1
if ($LASTEXITCODE -ne 0) { throw 'Cached setup failed.' }
pwsh -NoProfile -File .github/skills/test-evidence/scripts/Test-YamlFrontmatter.ps1
if ($LASTEXITCODE -ne 0) { throw 'Repository frontmatter validation failed.' }
pwsh -NoProfile -File .github/skills/test-evidence/scripts/Test-YamlFrontmatter.ps1 -Path artifacts/local-tools/yaml-probes/valid.prompt.md
if ($LASTEXITCODE -ne 0) { throw 'Valid probe failed.' }
$malformed = pwsh -NoProfile -File .github/skills/test-evidence/scripts/Test-YamlFrontmatter.ps1 -Path artifacts/local-tools/yaml-probes/malformed.prompt.md 2>&1
$malformedExit = $LASTEXITCODE
if ($malformedExit -ne 1 -or ($malformed | Out-String) -notmatch 'invalid YAML frontmatter') { throw 'Malformed probe was not rejected clearly.' }
$missing = pwsh -NoProfile -File .github/skills/test-evidence/scripts/Test-YamlFrontmatter.ps1 -ModuleCache artifacts/local-tools/yaml-probes/missing 2>&1
$missingExit = $LASTEXITCODE
if ($missingExit -ne 1 -or ($missing | Out-String) -notmatch 'Initialize-PowerShellDependencies.ps1' -or (Test-Path artifacts/local-tools/yaml-probes/missing)) { throw 'Missing dependency probe failed.' }
"PASS: cached setup, 24 repository frontmatters, 1 valid probe; malformed exit $malformedExit; missing dependency exit $missingExit with setup and no cache creation."
```

- **Result**: `pass; overall exit 0; 24 repo frontmatters (7 agents, 6 prompts, 3 instructions, 8 skills) and 1 valid probe parsed; malformed YAML exit 1 with file-specific parser diagnostic; missing module exit 1 with exact setup command and no cache creation; temporary inputs removed after validation`
- **Evidence source**: `run by implementer; earlier probes also passed at eda1ef8d32c27290b5ddc40f0b57d1ab496e032d07ab76b8db7cec64714cbe5a (167 files)`
- **Rerun reason**: `final source formatting changed bytes after the initial behaviour checks`

#### PowerShell syntax, formatting and traceability

- **State**: `e52355b0595a98e9ef207b43beb498ac4a0c98e042ecfd21e71aefd775ca8792` (`167` files; docs included: yes; exclusions: none)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; Git 2.55.0.windows.5; .NET SDK 10.0.401; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset`
- **Impact**: `three PowerShell files, shared guidance and evidence; WU-001 traceability contract`
- **Selected checks**: `PowerShell parser; CRLF/final newline on six changed files; trailing whitespace including untracked helpers; git diff --check; existing Test-Traceability.ps1`
- **Excluded checks**: `dotnet format cannot format PowerShell/Markdown; no runtime code changed`
- **Command**: repository root (formatting followed immediately by checks):

```powershell
$changedFiles = @('AGENTS.md', '.github/skills/test-evidence/SKILL.md', '.github/skills/test-evidence/PowerShellDependencies.psd1', '.github/skills/test-evidence/scripts/Initialize-PowerShellDependencies.ps1', '.github/skills/test-evidence/scripts/Test-YamlFrontmatter.ps1', 'Docs/Specs/M0/WU-001-ai-enablement.spec.md')
foreach ($file in $changedFiles) { $text = [System.IO.File]::ReadAllText((Join-Path $PWD $file)); [System.IO.File]::WriteAllText((Join-Path $PWD $file), (($text -replace '\r?\n', "`r`n").TrimEnd("`r", "`n") + "`r`n"), [System.Text.UTF8Encoding]::new($false)) }
& .github/skills/test-evidence/scripts/Get-FunctionalState.ps1 -IncludeDocumentation
foreach ($file in $changedFiles) { $text = [System.IO.File]::ReadAllText((Join-Path $PWD $file)); if ($text -match '(?<!\r)\n' -or -not $text.EndsWith("`r`n")) { throw "Line-ending policy failed: $file" }; if ($file -match '\.ps(d)?1$') { $tokens = $null; $parseErrors = $null; $null = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PWD $file), [ref]$tokens, [ref]$parseErrors); if ($parseErrors.Count) { throw ($parseErrors | Out-String) } } }
'PASS: 3 PowerShell files parse; 6 changed files have CRLF and final newline.'
& .github/skills/planning-artifacts/scripts/Test-Traceability.ps1 Docs/Specs/M0/WU-001-ai-enablement.spec.md
if ($LASTEXITCODE -ne 0) { throw 'Traceability failed.' }
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Whitespace check failed.' }
foreach ($file in $changedFiles) { if (Select-String -LiteralPath $file -Pattern '[\t ]+$' -Quiet) { throw "Trailing whitespace: $file" } }
'PASS: traceability, Git whitespace and all new/modified file whitespace.'
```

- **Result**: `pass; exit 0; 3 PowerShell files parse; 6 changed files have CRLF/final newline and no trailing whitespace; traceability reports 9 legacy ACs and no orphans. Initial formatting check failed (exit 1) at state 2a93a43dc914c15f3635cd7b219f857b3b42759519938de9478bb78291082d71 (167 files): dependency manifest lacked a final newline; normalization was repaired and the same focused checks passed.`
- **Evidence source**: `run by implementer`
- **Rerun reason**: `new scripts; formatting failure repaired before final behaviour checks`

Repository-wide text search and `git grep -n -i -E 'PyYAML|python|venv' -- .` found no maintained Python YAML script. The two original WU-001 Python/PyYAML evidence records above remain truthful historical exceptions. Other remaining mentions are prohibitions in shared YAML guidance, optional non-YAML Python use in AGENTS.md, and the existing PTVS ignore comment. No plan, status or checkbox changes are part of this tooling update.

#### Historical bounded YAML tooling verification

This bounded result and its approval apply only to the earlier HEAD identified below. They are not current-HEAD approval; CR-3 was reported and subsequently withdrawn at `800cc9d328489e22231aecdad37c6a00614eb30e` as recorded in the review chain above.

The six-file YAML tooling follow-up passes verification. Tailor Code Reviewer approval covers HEAD `027976822c978d28e7e1337b600e40c16b5f5661` and reviewed state `2b9c6b35f13c20fe5cb801b43708ef3374afd674e9ab1248aed2ba2391fe592f`, not unrelated WU-001 changes. The complete Final YAML behaviour checks and PowerShell syntax, formatting and traceability records above are reused after exact reconstruction of their tested state and environment. Initial dependency download evidence remains historical, not exact-state reuse. No YAML suite, setup/download or .NET suite was rerun. WU-001 remains `In review`; all checkbox states are unchanged. Whole-WU verification and its nine-criterion completion remain outside this bounded result.

- **State**: `2b9c6b35f13c20fe5cb801b43708ef3374afd674e9ab1248aed2ba2391fe592f` (`167` files; docs included: yes; exclusions: none; before this verification-only append)
- **Environment**: `Windows x64 10.0.22631; PowerShell 7.6.6; Git 2.55.0.windows.5; .NET SDK 10.0.401; DOTNET_TAILOR_*, CI and GITHUB_ACTIONS unset`
- **Impact**: `six scoped YAML follow-up files; customization directories consume the validator; no production or test changes`
- **Selected checks**: `approved HEAD/current fingerprint; evidence-only reconstruction; matching environment; maintained Python references; ignored dependency cache; scoped source/diff inspection`
- **Excluded checks**: `YAML behaviour, syntax and traceability reused from the complete records above; .NET suites cannot observe this tooling; all-nine-AC verification is outside scope`
- **Command**: repository root:

```powershell
& {
$ErrorActionPreference = 'Stop'
Set-Location E:\DotNetTailor
Get-Command pwsh, git, dotnet -ErrorAction Stop | Select-Object Name, Source
$head = git rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $head -ne '027976822c978d28e7e1337b600e40c16b5f5661') { throw 'Approved HEAD mismatch.' }
git status --short
& .github/skills/test-evidence/scripts/Get-FunctionalState.ps1 -IncludeDocumentation
$spec = 'Docs/Specs/M0/WU-001-ai-enablement.spec.md'
$text = [System.IO.File]::ReadAllText((Join-Path $PWD $spec))
$start = $text.IndexOf('### PowerShell YAML tooling evidence')
$end = $text.IndexOf('## Definition of Done', $start)
if ($start -lt 0 -or $end -le $start) { throw 'Cannot identify evidence-only append.' }
$sha = [System.Security.Cryptography.SHA256]::Create()
try {
$previousBytes = [System.Text.Encoding]::UTF8.GetBytes($text.Remove($start, $end - $start))
$previousSpecHash = [Convert]::ToHexString($sha.ComputeHash($previousBytes)).ToLowerInvariant()
$entries = @(git ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate state.' }
$manifest = @($entries | ForEach-Object {
$relative = $_.Replace('\', '/').TrimStart('./')
$hash = if ($relative -eq $spec) { $previousSpecHash } elseif (Test-Path -LiteralPath $_ -PathType Leaf) { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() } else { '<missing>' }
"{0}`t{1}" -f $relative, $hash
} | Sort-Object)
$manifestBytes = [System.Text.Encoding]::UTF8.GetBytes(($manifest -join "`n"))
$previousFingerprint = [Convert]::ToHexString($sha.ComputeHash($manifestBytes)).ToLowerInvariant()
"Evidence-only pre-append state: $previousFingerprint; files: $($manifest.Count)"
if ($previousFingerprint -ne 'e52355b0595a98e9ef207b43beb498ac4a0c98e042ecfd21e71aefd775ca8792') { throw 'Inherited state mismatch exceeds evidence append.' }
} finally { $sha.Dispose() }
"HEAD: $head; PowerShell: $($PSVersionTable.PSVersion); OS: $([Environment]::OSVersion.VersionString); Architecture: $([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)"
$variables = @(Get-ChildItem Env: | Where-Object { $_.Name -like 'DOTNET_TAILOR_*' -or $_.Name -in @('CI', 'GITHUB_ACTIONS') })
if ($variables.Count) { throw 'Relevant environment variables are set.' }
'Relevant environment variables: unset'
dotnet --version
git --version
git grep -n -i -E 'PyYAML|python|venv' -- .
if ($LASTEXITCODE -gt 1) { throw 'Text search failed.' }
git check-ignore artifacts/local-tools/powershell-modules
if ($LASTEXITCODE -ne 0) { throw 'Dependency cache is not ignored.' }
}
```

- **Result**: `pass; exit 0; exact approved current fingerprint and reconstructed e52355b0595a98e9ef207b43beb498ac4a0c98e042ecfd21e71aefd775ca8792 (167 files); environment matches; maintained scripts/instructions do not depend on Python YAML; cache ignored. An initial verifier command had an extra closing parenthesis and failed to parse before execution; the corrected block above passed without file changes.`
- **Evidence source**: `run by verifier; full inherited YAML/syntax records read before reuse; reviewer approval supplied for this exact HEAD and state`
- **Rerun reason**: `independently establish exact-state/environment reuse and scoped dependency contract; corrected verifier-command parser error, not a repository defect`

## Definition of Done

- All AC ticked by the Verifier; solution still builds with zero warnings and tests green; format clean.
- Plan status updated (`In review` → `Done`).

## Historical Completion

The earlier completion record below is retained for provenance, not current evidence. Its approval does not cover current HEAD; current completion relies on the exact-state checks and combined current-HEAD review chain above.

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

None remaining for WU-001. Previously recorded gates are resolved as follows:

- **CR-3 (withdrawn)**: ~~A code fix rejecting reparse points and confining cache writes is required.~~ User-controlled cache relocation is accepted; the same-HEAD reviewer withdrew the finding. The original report is preserved in the review chain; no code fix or review-gate waiver was needed.
- **AC-8 and review evidence (resolved)**: ~~Current-HEAD confirmation/approval is missing.~~ The full current-HEAD report's sole finding was withdrawn by the same-HEAD targeted approving report. Together they satisfy AC-8 and the WU review gate; neither report is misrepresented as a single full `Approve`.
- **Branch scope distinction (retained)**: ~~Unrelated branch changes must be resolved before WU-001 can complete.~~ Unrelated files remain outside this WU's verification and approval claim. No supplied outstanding finding or failed AC attributes them to this WU's deliverables; WU-001 completion does not certify the entire branch.

## Completion
- **Summary**: Verified all 26 AI-enablement deliverables and all nine acceptance criteria.
- **Commits/PR**: Verified HEAD `800cc9d328489e22231aecdad37c6a00614eb30e`; original review range `6ba61cd3..02797682`; no PR or verification commit.
- **Code review**: Full current-HEAD report plus same-HEAD targeted `Approve`; sole finding CR-3 withdrawn, consistent with the recorded user cache disposition; see review chain above.
- **Evidence**: Exact-state format/build/test records and final pinned PowerShell YAML/contract checks above.
- **Follow-ups**: None for WU-001; unrelated branch work is not certified.
