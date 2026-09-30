# WU-105 cli-skeleton

| Field | Value |
|---|---|
| ID | WU-105 |
| Title | cli-skeleton |
| Milestone | M1 Core Primitives & Specification Documents |
| Status | Not started |
| Depends on | WU-100 (`schema export` ACs need WU-101 and WU-102 `Done`) |
| Parallel with | WU-101–WU-104, M2 |
| Target project(s)/paths | `src/DotNetRepack.Cli/`, `tests/DotNetRepack.Cli.Tests/` |
| Size | M |

## Goal

Provide the `dotnet-repack` command host on System.CommandLine 2.0 with every v1 verb declared, global options, response files, diagnostic output and exit-code mapping; verbs are stubs except a functional `schema export`. Package the project as a `dotnet tool`.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| CLI, dotnet conventions, tool packaging | [RQ §10](../../Requirements/Repackage_tool_Requirements_v1.1.md), [RQ §11](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§1](../../Architecture/DotNetRepack.architecture.md#1-summary), [§14](../../Architecture/DotNetRepack.architecture.md#14-cli) |
| Exit codes, strict/permissive | [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md), [TS §24](../../Requirements/Transformation_Specification.md) | [§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes) |
| Response files | [CK §3.2](../../Requirements/Read_to_run_Cake.md) | [§19](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) item 6 |
| Composition | — | [§3.1](../../Architecture/DotNetRepack.architecture.md#31-project-responsibilities-and-allowed-dependencies) (DI wiring) |
| Schema export | [AS §5.3](../../Requirements/Application_Specification.md), [TS §5.3](../../Requirements/Transformation_Specification.md) | [§6.1](../../Architecture/DotNetRepack.architecture.md#61-common-rules), [§14](../../Architecture/DotNetRepack.architecture.md#14-cli) |

## Scope

**In**
- Root command, verbs `analyze` (alias `analyse`), `validate`, `plan`, `apply`, `inspect`, `schema export`, with arguments/options exactly as [§14](../../Architecture/DotNetRepack.architecture.md#14-cli).
- Global options: `--verbosity`, `--strict`, `--permissive`, `--artifacts <dir>`, `--offline`, `--var name=value` (repeatable).
- `@file` response files (System.CommandLine built-in).
- Diagnostic console rendering; exit-code mapping; unhandled-exception handling.
- DI composition root (`Microsoft.Extensions.DependencyInjection`).
- `PackAsTool` packaging.

**Out**
- Verb implementations (WU-404, WU-506, WU-603, …).
- Tool config file and `DOTNET_REPACK_*` environment variables (WU-1000).
- `schema export plan` content (WU-506).

## Deliverables

Namespace `DotNetRepack.Cli`.

| Type | Responsibility |
|---|---|
| `Program` | `static Task<int> Main(string[] args)` → `CliApplication.RunAsync(args, Console.Out, Console.Error)` |
| `CliApplication` | `static Task<int> RunAsync(string[] args, TextWriter stdout, TextWriter stderr, IServiceProvider? services = null, CancellationToken ct = default)`; builds the root, parses, maps errors, invokes |
| `RootCommandFactory` | Builds `RootCommand("dotnet-repack")`, global options with `Recursive = true`, all verbs |
| `GlobalOptions` + `GlobalSettings` | Option definitions; parsed record (`Verbosity`, `FailureMode`, `ArtifactsDirectory?`, `Offline`, `Variables` `IReadOnlyDictionary<string,string>`) |
| `Commands/*Command` | One class per verb; stub actions return `ExitCodes.NotImplemented` with `RPK0100` |
| `Commands/SchemaExportCommand` | Functional: `schema export [appspec\|transformspec\|plan] [--output <dir>]` |
| `ExitCodes` | Constants `Success=0`, `ValidationErrors=1`, `Usage=2`, `WarningsAsErrors=3`, `Environment=4`, `ExecutionFailure=5`, `InternalError=70`, `NotImplemented=70`, `Cancelled=130` |
| `ExitCodeMapper` | `int Map(IReadOnlyList<Diagnostic>, FailureMode, ExitCategory hint)` per [§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes) |
| `DiagnosticRenderer` | Writes `"{severity} {code}: {message} [{location}]"` lines to stderr; honours `--verbosity` |
| `CliDiagnostics` | `RPK0100`–`RPK0199` |

`DotNetRepack.Cli.csproj`: `OutputType=Exe`, `PackAsTool=true`, `ToolCommandName=dotnet-repack`, `PackageId=DotNetRepack.Tool`, `AssemblyName=dotnet-repack` (or keep default and rely on `ToolCommandName`), references `System.CommandLine` 2.0.x and `Microsoft.Extensions.DependencyInjection` via CPM, `ProjectReference` to Core and Specifications only at this stage.

Diagnostics (minimum):

| Code | Condition | Exit |
|---|---|---|
| `RPK0100` | Command not implemented yet | 70 |
| `RPK0101` | Invalid `--var` syntax (not `name=value`, invalid name) | 2 |
| `RPK0102` | Duplicate `--var` name | 2 |
| `RPK0103` | `--strict` and `--permissive` together | 2 |
| `RPK0104` | Unknown schema kind / kind not yet available (`plan`) | 2 |
| `RPK0105` | Parse error from System.CommandLine (wraps its message) | 2 |
| `RPK0199` | Unhandled exception (message + type; stack only at `diagnostic` verbosity) | 70 |

## Design Notes

- System.CommandLine 2.0 API: `RootCommand`, `Command`, `Option<T>`, `Argument<T>`, `SetAction`, `Parse(args)`, `ParseResult.Errors`, `InvokeAsync(InvocationConfiguration)` with `Output`/`Error` writers for in-process tests. Response-file token replacement is on by default; keep it enabled.
- Parse errors: check `ParseResult.Errors` before invoking; render as `RPK0105`, print usage hint, return **2** (System.CommandLine's default is 1).
- `--verbosity` values follow dotnet: `q[uiet]`, `m[inimal]`, `n[ormal]` (default), `d[etailed]`, `diag[nostic]`.
- `--strict`/`--permissive` map to `Core.Policies.FailureMode` (semantics in WU-100 and architecture §13); `ExitCodeMapper` returns 3 when `Strict` and only warnings are present, 1 for validation/structural errors.
- `--var` accepts repeated `name=value`; value may contain `=`; names validated with WU-104's rule (`^[A-Za-z_][A-Za-z0-9_]*$`, duplicated locally if WU-104 is not merged, then switched to `VariableSyntax.IsValidName`).
- `schema export`: without a kind, exports all available kinds; `--output` default = current directory; writes `<dir>/<kind>/v1/<kind>.schema.json`, byte-identical to the committed `schemas/` files (same `SchemaGenerator`). `plan` → `RPK0104` until WU-506.
- `analyze` alias `analyse` via `Command.Aliases.Add("analyse")`.
- No Windows-specific types in Cli beyond composition ([§12](../../Architecture/DotNetRepack.architecture.md#12-platform-abstraction)); `Platform.Windows` registration is added when that project has content.
- Exceptions: catch at `CliApplication`, render `RPK0199`, return 70; `OperationCanceledException` from Ctrl+C returns 130 (`ExitCodes.Cancelled`) with a cancellation message.

## Acceptance Criteria

- [ ] AC-1 `dotnet-repack --help` exits 0 and lists `analyze`, `validate`, `plan`, `apply`, `inspect`, `schema` and all global options (golden file of help text).
- [ ] AC-2 `analyse --help` resolves to the `analyze` command.
- [ ] AC-3 Each verb's `--help` shows the arguments/options from [§14](../../Architecture/DotNetRepack.architecture.md#14-cli) (golden file per verb).
- [ ] AC-4 Invoking `analyze`, `validate`, `plan`, `apply`, `inspect` with valid arguments writes `RPK0100` to stderr and returns `ExitCodes.NotImplemented`.
- [ ] AC-5 Unknown option, missing required argument, unknown verb each return exit code 2 with `RPK0105` on stderr.
- [ ] AC-6 `--strict --permissive` → 2 (`RPK0103`); `--var 1x=a` and `--var novalue` → 2 (`RPK0101`); `--var a=1 --var a=2` → 2 (`RPK0102`); `--var a=b=c` parses to `a` → `b=c`.
- [ ] AC-7 A response file `@args.rsp` containing `validate app --spec s.json --var x=1` produces the same `GlobalSettings` and command as the inline arguments (test compares parsed results).
- [ ] AC-8 `schema export --output <tmp>` writes `appspec/v1/appspec.schema.json` and `transformspec/v1/transformspec.schema.json` byte-identical to `schemas/…` in the repo; `schema export plan` → 2 (`RPK0104`). *(requires WU-101, WU-102 Done)*
- [ ] AC-9 `ExitCodeMapper` unit tests cover: no diagnostics → 0; warnings + `Default` → 0; warnings + `Strict` → 3; error → 1; category hints `Environment` → 4, `Execution` → 5.
- [ ] AC-10 An injected throwing command returns 70 with `RPK0199`; no stack trace at `normal` verbosity, stack trace at `diagnostic`. An injected command that observes a cancelled token returns 130.
- [ ] AC-11 `dotnet pack src/DotNetRepack.Cli` produces `DotNetRepack.Tool.<version>.nupkg` whose `DotnetToolSettings.xml` declares command `dotnet-repack` (test or scripted check recorded in Test Evidence).
- [ ] AC-12 All CLI tests run in-process through `CliApplication.RunAsync` with captured writers; no child process is spawned.

## Test Requirements

- xUnit v3 + golden files (`DotNetRepack.Testing.Golden`, `.golden.txt` for help text) in `tests/DotNetRepack.Cli.Tests/`; in-process invocation only; temp directories via a per-test disposable helper. Trait `WU=105`.
- Run: `dotnet test --project tests/DotNetRepack.Cli.Tests --filter-trait "WU=105"` · focused: `--filter-class "*SchemaExportTests"`.
- Record Test Evidence (including the AC-11 pack check) below and in the PR.

## Definition of Done

- Zero warnings; tests green; `dotnet format --verify-no-changes` clean.
- ACs ticked by the Verifier (AC-8 only after WU-101/WU-102 are `Done`; WU-105 is not `Done` before that); plan status `Done`; Test Evidence recorded.

## Agent Notes

- Help-text golden files will churn as later WUs add options; keep them in one test class so updates are cheap (`DOTNET_REPACK_UPDATE_GOLDEN=1` locally). Captured console text uses `Environment.NewLine` (CRLF on Windows) while golden files are LF: capture with an LF writer or supply a `\r\n` → `\n` scrubber.
- Keep verb option definitions in their command classes so WU-404/WU-506/WU-603 only replace the action.
- If WU-101/WU-102 are not merged, implement everything else and leave AC-8 with the status `Blocked (WU-101/WU-102)`.

## Open Questions

- **Resolved** — "not implemented" exit code: 70 until the verb is implemented (architecture §13).
- **Resolved** — cancellation exit code: 130, output rolled back (architecture §13).
- `AssemblyName=dotnet-repack` vs default `DotNetRepack.Cli` for the tool DLL name.

## Test Evidence

_To be completed by the implementer._
