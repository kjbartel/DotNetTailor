# DotNetRepack

> Working names: product `DotNetRepack`, command `dotnet-repack`, package `DotNetRepack.Tool`. All are placeholders until the final name is decided.

DotNetRepack is a binary-first .NET command-line tool that analyses, validates and transforms compiled .NET application folder trees. It never modifies its input: it produces a new tree plus a new Application Specification (AppSpec) describing it. Planned transformations include filtering and layout, ReadyToRun compilation, framework-dependent ⇄ self-contained conversion, retargeting and patching, for Windows `win-x64` in v1.

## Status

Pre-release. The repository contains the empty solution scaffold (WU-000); no features are implemented yet. Progress is tracked per work unit in the [plan](Docs/Plans/DotNetRepack.plan.md#work-unit-status).

## Prerequisites

- .NET 10 SDK (pinned by [global.json](global.json); later 10.0 feature bands roll forward).
- .NET 8 runtime, later, to build and run the sample test apps.

## Build, test and format

Run from the repository root:

```powershell
dotnet build DotNetRepack.slnx -c Release -warnaserror
dotnet test --solution DotNetRepack.slnx -c Release
dotnet format DotNetRepack.slnx --verify-no-changes
```

Tests use xUnit v3 on Microsoft Testing Platform (`dotnet test` MTP mode, configured in [global.json](global.json)). Run one work unit's tests with:

```powershell
dotnet test --project tests/DotNetRepack.Core.Tests --filter-trait "WU=000"
```

## Repository map

| Path | Content |
|---|---|
| `src/DotNetRepack.*` | Production projects (see [architecture §3](Docs/Architecture/DotNetRepack.architecture.md#3-solution-layout)) |
| `tests/DotNetRepack.*` | One xUnit v3 project per `src` project, plus integration and regression tests |
| [Docs/Requirements](Docs/Requirements/Repackage_tool_Requirements_v1.1.md) | Source requirements |
| [Docs/Architecture](Docs/Architecture/DotNetRepack.architecture.md) | Architecture |
| [Docs/Plans](Docs/Plans/DotNetRepack.plan.md) | Master plan and work-unit status |
| `Docs/Specs` | Work-unit specifications, one per WU |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Licence

Not yet chosen. See [LICENSE](LICENSE).
