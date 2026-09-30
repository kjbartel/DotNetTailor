# .NET Tailor
The .NET Tailor tool customises and alters .NET applications to fit your operational needs.

> Repository status: product `Tailor`, command `dotnet-tailor`, root namespace `Tailor`, package id `dotnet-tailor`.

.NET Tailor is a binary-first .NET command-line tool that analyses, validates and transforms compiled .NET application folder trees. It never modifies its input: it produces a new tree plus a new Application Specification (AppSpec) describing it. Planned transformations include filtering and layout, ReadyToRun compilation, framework-dependent ⇄ self-contained conversion, retargeting and patching, for Windows `win-x64` in v1.

## Status

Pre-release. The repository contains the empty solution scaffold (WU-000); no features are implemented yet. Progress is tracked per work unit in the [plan](Docs/Plans/Tailor.plan.md#work-unit-status).

## Prerequisites

- .NET 10 SDK (pinned by [global.json](global.json); later 10.0 feature bands roll forward).
- .NET 8 runtime, later, to build and run the sample test apps.

## Build, test and format

Run from the repository root:

```powershell
dotnet build DotNetTailor.slnx -c Release -warnaserror
dotnet test --solution DotNetTailor.slnx -c Release
dotnet format DotNetTailor.slnx --verify-no-changes
```

Tests use xUnit v3 on Microsoft Testing Platform (`dotnet test` MTP mode, configured in [global.json](global.json)). Run one work unit's tests with:

```powershell
dotnet test --project tests/Tailor.Core.Tests --filter-trait "WU=000"
```

## Repository map

| Path | Content |
|---|---|
| `src/Tailor.*` | Production projects |
| `tests/Tailor.*` | One xUnit v3 project per `src` project, plus integration and regression tests |
| [Docs/Requirements](Docs/Requirements/Repackage_tool_Requirements_v1.1.md) | Source requirements |
| [Docs/Architecture](Docs/Architecture/Tailor.architecture.md) | Architecture |
| [Docs/Plans](Docs/Plans/Tailor.plan.md) | Master plan and work-unit status |
| `Docs/Specs` | Work-unit specifications, one per WU |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

This project is licensed under the [Apache License 2.0](LICENSE). Copyright 2026 Kuan Bartel.
