# Repository Guidance

This file is the repository-wide agent guide. The design authority is [the architecture](Docs/Architecture/Tailor.architecture.md); sequencing and work-unit status live in [the master plan](Docs/Plans/Tailor.plan.md). A work unit's spec is its implementation contract. Agents, lanes and review flow are in [the AI workflow guide](Docs/Guides/ai-workflow.md); start with the **Tailor Contributor** agent.

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

Run one work unit's tests with `dotnet test --project <project> --filter-trait "WU=<id>"`. Use the WU trait, MTP conventions and test categories defined in the [plan](Docs/Plans/Tailor.plan.md#test-conventions). Tests make no network calls by default.

## Implementation Workflow

Follow [How Agents Use This Plan](Docs/Plans/Tailor.plan.md#how-agents-use-this-plan):

1. Select a `Ready` (legacy `Not started`) WU whose dependencies are all `Done`; prefer the critical path.
2. Read its `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md`, cited architecture sections and requirements.
3. Set the WU `In progress` and work on `wu/<id>-<slug>`.
4. Implement only its scope, following its `## Steps` in order; add focused tests (WU trait, plus `AC` trait for `(T)` criteria) and run formatting, lint and relevant checks.
5. Record the required Test Evidence block with the exact functional-state fingerprint and environment.
6. Keep changes reviewable and open a PR titled `WU-<id>: <title>` when using a remote. The Tailor Code Reviewer (and any human PR review) must `Approve`; resolve findings with the `code-review` skill.
7. The Verifier checks evidence, ticks steps and acceptance criteria, appends a concise Completion note and sets status to `Done`. Implementers never tick their own work.

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

For selected checks, follow the [`test-evidence` skill](.github/skills/test-evidence/SKILL.md) and record the state fingerprint and all context needed to reuse the result:

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
