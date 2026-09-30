# Contributing

Work is split into atomic work units (WUs). Follow the workflow in the [plan: How Agents Use This Plan](Docs/Plans/DotNetRepack.plan.md#how-agents-use-this-plan).

## Work-unit workflow

1. Pick a WU whose status is `Not started` and whose dependencies are all `Done`. Prefer WUs on the critical path.
2. Read the WU spec in `Docs/Specs/<Milestone>/`, plus the architecture and requirement sections it cites.
3. Set the WU status to `In progress` in the [plan](Docs/Plans/DotNetRepack.plan.md#work-unit-status).
4. Work on branch `wu/<id>-<slug>`, for example `wu/300-folder-matching-engine`.
5. Satisfy every acceptance criterion. Tag every test with `[Trait("WU", "<id>")]`.
6. Open a pull request titled `WU-<id>: <title>` and include a Test Evidence block.
7. The Verifier ticks the acceptance criteria in the spec and sets the plan status to `Done`.

## Required local checks

Run from the repository root before opening a pull request:

```powershell
dotnet build DotNetRepack.slnx -c Release -warnaserror
dotnet test --solution DotNetRepack.slnx -c Release
dotnet format DotNetRepack.slnx --verify-no-changes
```

Package versions live only in `Directory.Packages.props`; do not put a `Version` on a `PackageReference`.

## Architecture decisions

If a WU changes the architecture, record an ADR in `Docs/Decisions/` and update the [architecture document](Docs/Architecture/DotNetRepack.architecture.md) in the same pull request.
