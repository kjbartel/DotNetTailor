# Repository Guidance

Follow [the architecture](Docs/Architecture/Tailor.architecture.md) for design, [the master plan](Docs/Plans/Tailor.plan.md) for sequencing and status, and the selected work unit's spec for scope. Start with **Tailor Contributor**; agents, lanes and review flow are in [the AI workflow guide](Docs/Guides/ai-workflow.md).

## Repository Map

- `src/`: production projects; dependencies follow architecture §3.1.
- `tests/`: unit, integration, regression and shared test-support projects.
- `build/`: repository build and test-app scripts when present.
- `schemas/`: committed schemas generated from specification models.
- `Docs/Requirements/`: authoritative requirements; do not edit during implementation work.
- `Docs/Architecture/`, `Docs/Plans/`, `Docs/Specs/`: design, sequencing and work-unit contracts.
- `Docs/Epics/`, `Docs/Features/`, `Docs/Decisions/`, `Docs/Guides/`: epics, features, ADRs and contributor/user guides (hierarchy and gates: `planning-artifacts` skill).
- `spikes/` and `Docs/Spikes/`: isolated research and spike reports; spike code is never referenced from `src/`.

## Build and Test

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet build DotNetTailor.slnx -c Release -warnaserror
dotnet test --solution DotNetTailor.slnx -c Release
dotnet format DotNetTailor.slnx --verify-no-changes
```

For one WU, use `dotnet test --project <project> --filter-trait "WU=<id>"`. Follow the [plan's test conventions](Docs/Plans/Tailor.plan.md#test-conventions) for traits, MTP and categories. Tests make no network calls by default.

## Agent Tooling

- Prefer PowerShell, the .NET CLI and repository scripts. Use Python or other runtimes only when needed and confirmed available; do not install tools to satisfy assumed dependencies.
- All installation of modules, packages, scripts and other executables (PowerShell, .Net, Python, Node.js, etc.), not already recorded as dependencies in the repo, must explicitly be confirmed before download and installation.
- For YAML, use the pinned `powershell-yaml` dependency and commands in the [test-evidence skill](.github/skills/test-evidence/SKILL.md#yaml-validation). Validate customization frontmatter with `Test-YamlFrontmatter.ps1`; do not create disposable Python environments or use PyYAML for YAML checks.
- Before first use of an unfamiliar command, run `Get-Command <name> -ErrorAction SilentlyContinue`; confirm its path and relevant version. Use an available equivalent if missing.
- Linux/Bash commands and syntax require explicitly invoked, verified Git Bash and confirmed utilities; Git alone is not enough. Never use them in PowerShell. This terminal-only exception does not relax application process safety below.
- Never invent arguments. Use repo-documented commands or check the installed subcommand's help, `Get-Help`, or version-matched official docs; options vary by version and test runner.
- Keep tool paths, versions, supported arguments and failures in session context; pass them to delegates and scoped persistent memory when available. Never commit machine-specific paths. Do not repeat missing commands or rejected arguments unchanged; recheck only after environment changes or new evidence, and discard stale records.

## Documentation, Comments and Tests

- Keep documentation and comments concise and accurate for the current repository. Label planned behaviour explicitly; never describe unfinished work as implemented. Update or remove stale text with the code it describes.
- Describe contracts, constraints and durable rationale, never chat decisions, review conversations or mid-feature change history. Keep temporary clarifications and progress notes in session memory, not maintained docs or comments. ADRs and required verification evidence record technical decisions and observed results, not session narrative.
- Tests assert current observable behaviour, including required rejection and safety guarantees. Never add unit tests for deleted code/text being absent or superseded behaviour no longer occurring; retain regression coverage for the current contract.
- Before review and again at feature completion, remove temporary probes, obsolete draft checks, stale comments and resolved clarification notes, or convert useful probes into contract-focused tests. Do not discard required Test Evidence or genuine compatibility coverage for released behaviour.

## Implementation Workflow

Follow [How Agents Use This Plan](Docs/Plans/Tailor.plan.md#how-agents-use-this-plan):

1. Select a `Ready` (legacy `Not started`) WU with all dependencies `Done`; prefer the critical path.
2. Read `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md` and its cited architecture and requirements.
3. Set `In progress`; use branch `wu/<id>-<slug>`.
4. Implement only its scope and follow `## Steps` in order. Add focused tests with WU traits and `AC` traits for `(T)` criteria; run formatting, lint and relevant checks.
5. Record Test Evidence with the exact functional-state fingerprint and environment.
6. Keep changes reviewable; when using a remote, open a PR titled `WU-<id>: <title>`. Require `Approve` from Tailor Code Reviewer and any human reviewer; resolve findings with the `code-review` skill.
7. The Verifier checks evidence; only the Verifier ticks steps and criteria, adds a concise Completion note and sets `Done`. Implementers never tick their own work.

## Boundaries

Respect the ownership and dependency rules in [architecture §3.1](Docs/Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies):

- Core has no project references. Specifications and Inspection depend only on Core; Model depends on Specifications and Inspection.
- Analysis and Validation depend on Model; Planning depends on Validation.
- Transforms depend on Planning, Acquisition and Platform.Abstractions. Execution depends on Planning, Validation and Platform.Abstractions.
- Acquisition depends on Inspection. Platform.Windows depends on Platform.Abstractions and Inspection. Cli composes the higher-level services.
- Do not put heuristics in Model, classification decisions in Inspection, filesystem writes in Planning, action execution in Transforms, AppSpec semantics in Acquisition, or Windows types outside platform projects.
- Do not edit `Docs/Requirements/**` during implementation. Change architecture only for a necessary design change recorded with an ADR. Do not manually edit generated schemas.
- Add package versions only through `Directory.Packages.props` (Central Package Management).

## Safety and Determinism

- Never modify the input application tree. Confine paths to the app root and reject escaping reparse points.
- Never execute a shell to launch a child process; use `ProcessStartInfo.ArgumentList` and cancellation-aware APIs.
- Never put credentials or secrets in specs, CLI arguments, logs or artefacts.
- Write canonical artefacts only through the Core writer: stable ordering, LF, no timestamps, GUIDs or machine-specific paths. Preserve the configured CRLF source-file policy and LF exceptions.
- Keep spike code out of `src/`; keep package acquisition and other declared side effects explicit.

## Test Evidence

Follow the [`test-evidence` skill](.github/skills/test-evidence/SKILL.md); record each selected check for exact-state reuse:

```markdown
## Test Evidence
- **State**: `<fingerprint>` (`<files>` files; docs included: yes/no; exclusions: `<patterns and rationale or none>`)
- **Environment**: `<OS; runtime/interpreter; dependency environment; relevant env vars>`
- **Impact**: `<directly changed units; indirect consumers examined>`
- **Selected checks**: `<test paths/selectors and why they are relevant>`
- **Excluded checks**: `<notable suites omitted and why they cannot observe the change>`
- **Command**: `<exact command and working directory>`
- **Result**: `<pass/fail, exit code, counts, key failure>`
- **Evidence source**: `<run by this agent | reused from agent/record>`
- **Rerun reason**: `<none | precise invalidation/coverage reason>`
```

Reuse evidence only for the exact functional state and matching environment. Do not claim checks that were not run or whose evidence is outdated.
