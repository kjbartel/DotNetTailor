# Contributing

Work is split into atomic work units (WUs). Follow the workflow in the [plan: How Agents Use This Plan](Docs/Plans/Tailor.plan.md#how-agents-use-this-plan). AI-assisted contributions use the agents in the [AI workflow guide](Docs/Guides/ai-workflow.md).

## Work-unit workflow

1. Pick a WU whose status is `Ready` (legacy `Not started`) and whose dependencies are all `Done`. Prefer WUs on the critical path.
2. Read the WU spec in `Docs/Specs/<Milestone>/`, plus the architecture and requirement sections it cites.
3. Set the WU status to `In progress` in the [plan](Docs/Plans/Tailor.plan.md#work-unit-status).
4. Work on branch `wu/<id>-<slug>`, for example `wu/300-folder-matching-engine`.
5. Implement the spec's steps in order and satisfy every acceptance criterion. Tag every test with `[Trait("WU", "<id>")]`, and tests proving a `(T)` criterion with `[Trait("AC", "WU-<id>/AC-<n>")]`.
6. Open a pull request titled `WU-<id>: <title>` and include a Test Evidence block. Code review (agent and/or human) must approve; answer each finding as Fixed, Disagree or Deferred.
7. The Verifier ticks the steps and acceptance criteria in the spec, adds a short Completion note and sets the plan status to `Done`.

## Required local checks

Run from the repository root before opening a pull request:

```powershell
dotnet build DotNetTailor.slnx -c Release -warnaserror
dotnet test --solution DotNetTailor.slnx -c Release
dotnet format DotNetTailor.slnx --verify-no-changes
```

Package versions live only in `Directory.Packages.props`; do not put a `Version` on a `PackageReference`.

## Architecture decisions

If a WU changes the architecture, record an ADR in `Docs/Decisions/` and update the [architecture document](Docs/Architecture/Tailor.architecture.md) in the same pull request.
