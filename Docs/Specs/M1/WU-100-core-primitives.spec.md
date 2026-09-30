# WU-100 core-primitives

| Field | Value |
|---|---|
| ID | WU-100 |
| Title | core-primitives |
| Milestone | M1 Core Primitives & Specification Documents |
| Status | Not started |
| Depends on | WU-000 |
| Parallel with | WU-001–WU-007 |
| Target project(s)/paths | `src/Tailor.Core/`, `tests/Tailor.Core.Tests/`, `tests/Tailor.Testing/` |
| Size | L |

## Goal

Provide the dependency-free primitives every other project builds on: root-confined relative paths, the diagnostic model and code registry, failure-policy mapping, result types, deterministic ordering, canonical JSON, schema-version parsing, SHA-256 content hashing and the tree fingerprint.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Relative, portable, confined paths | [AS §23.3](../../Requirements/Application_Specification.md), [AS §24](../../Requirements/Application_Specification.md) (9), [AS §15.2](../../Requirements/Application_Specification.md), [TS §24.4](../../Requirements/Transformation_Specification.md) | [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies), [§7.1](../../Architecture/Tailor.architecture.md#71-folder-matching), [§17](../../Architecture/Tailor.architecture.md#17-security) |
| Diagnostics, policies | [TS §24](../../Requirements/Transformation_Specification.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§13](../../Architecture/Tailor.architecture.md#13-diagnostics-failure-policy-and-exit-codes) |
| Determinism, canonical JSON, fingerprint | [AS §3.6](../../Requirements/Application_Specification.md), [TS §3.6](../../Requirements/Transformation_Specification.md), [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md), [AS §5.2](../../Requirements/Application_Specification.md) | [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 22 |
| Schema version | [AS §5.3](../../Requirements/Application_Specification.md), [TS §5.3](../../Requirements/Transformation_Specification.md) | [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules) |

## Scope

**In**
- `RelativePath` + `PathPolicy` (Windows ordinal-ignore-case comparer).
- `DiagnosticDescriptor`, `Diagnostic`, `DiagnosticLocation`, `DiagnosticBag`, `DiagnosticRegistry`, category → code-range rules.
- `ConditionPolicy` and `FailureMode` mapping, including the structural-cannot-downgrade rule.
- `Result` / `Result<T>`.
- Deterministic ordering helpers and comparers.
- `JsonPointer` (RFC 6901) builder/escaper; conversion from `System.Text.Json` `$.a.b[0]` paths.
- `CanonicalJson` writer and shared reader options.
- `SchemaVersion` + `SchemaCompatibility` (kind-agnostic; used by WU-101/WU-102).
- `ContentHash` (SHA-256), `ContentHasher`, `TreeFingerprint` (single implementation; WU-305 consumes it and only adds sidecar exclusion).
- `IPackageLocator` contract (architecture §3.2): implemented by Acquisition (WU-700), consumed by Execution (WU-601, WU-703), wired by the Cli.
- `Golden` golden-file test helper in `tests/Tailor.Testing` (architecture [§16](../../Architecture/Tailor.architecture.md#16-testing-strategy)); WU-100 is its first consumer and owns canonical JSON.

**Out**
- Reparse-point / junction canonicalisation (Platform, WU-600/WU-800).
- Directory enumeration of app trees (Model, WU-300).
- Exit-code mapping (Cli, WU-105).
- Diagnostics catalogue documentation (WU-1001).
- SHA-512 package hashing (WU-700).

## Deliverables

Namespace root `Tailor.Core`. The project references no other repo project and no third-party packages.

| Namespace | Key public types / APIs |
|---|---|
| `.Paths` | `readonly struct RelativePath : IEquatable<RelativePath>`: `static Result<RelativePath> TryParse(string)`, `static RelativePath Parse(string)` (throws `FormatException`), `Root`, `Value` (always `/`, no leading/trailing `/`), `Segments`, `Name`, `Extension`, `Parent`, `IsRoot`, `Combine(RelativePath)`, `Combine(string)`, `IsSameOrUnder(RelativePath ancestor, PathPolicy)`, `ToFullPath(string rootDirectory)` (lexical confinement check). `Equals` is ordinal. `sealed class PathPolicy`: `Windows` (default), `Comparer` (`IComparer<RelativePath>` + `IEqualityComparer<RelativePath>`), `StringComparer`. |
| `.Diagnostics` | `enum DiagnosticSeverity { Info, Warning, Error }`; `enum DiagnosticCategory { Cli=0, Specification=1, Inspection=2, Model=3, Validation=4, Planning=5, Execution=6, Acquisition=7, Deployment=8, Retarget=9 }`; `sealed record DiagnosticDescriptor(string Code, DiagnosticCategory Category, DiagnosticSeverity DefaultSeverity, string MessageFormat, bool IsStructural, bool IsPolicyConfigurable)`; `sealed record DiagnosticLocation(string? Document, string? JsonPointer, RelativePath? Path)`; `sealed record Diagnostic` (`Descriptor`, `Code`, `Severity`, `Message`, `MessageArgs`, `Location`, `RelatedLocations`), `Diagnostic.Create(descriptor, location, params object[] args)` (formats with `CultureInfo.InvariantCulture`); `DiagnosticBag` (add, `HasErrors`, `ToSortedList()`); `DiagnosticRegistry` (register descriptors, rejects duplicates, bad format `^RPK\d{4}$` and out-of-range codes); `CoreDiagnostics` static descriptors. |
| `.Policies` | `enum ConditionPolicy { Error, Warning, Skip, Preserve }`; `enum FailureMode { Default, Strict, Permissive }`; `static PolicyEvaluator.Apply(Diagnostic, ConditionPolicy?, FailureMode) → PolicyOutcome` (`Severity`, `Action`: `Report`/`Skip`/`Preserve`); `PolicyEvaluator.CountsAsFailure(Diagnostic, FailureMode)`. |
| `.Results` | `Result` and `Result<T>`: `Value`, `Diagnostics`, `IsSuccess` (no error diagnostics), `Success(...)`, `Failure(...)`, `Map`, `Bind`. |
| `.Ordering` | `DeterministicOrder.ByPath(...)`, `DeterministicOrder.ByKey(...)`; `OrdinalIgnoreCaseThenOrdinalComparer` (total order: ignore-case first, ordinal tie-break); `DiagnosticComparer`. |
| `.Json` | `JsonPointer` (`Root`, `Append(string)`, `Append(int)`, `FromJsonPath(string)`, `ToString()`); `static CanonicalJson`: `ReaderOptions` (`JsonCommentHandling.Skip`, `AllowTrailingCommas`, `MaxDepth = 64`), `CreateSerializerOptions()` (camelCase, `JsonStringEnumConverter` camelCase, `WhenWritingNull`, `UnmappedMemberHandling.Disallow`, `RespectNullableAnnotations`, `RespectRequiredConstructorParameters`), `WriterOptions` (`Indented`, `IndentSize = 2`, `NewLine = "\n"`, `UnsafeRelaxedJsonEscaping`), `Serialize<T>(T, JsonSerializerOptions) → byte[]`, `WriteAsync<T>(Stream, T, …)`, `Canonicalise(JsonNode) → byte[]` (sorts object members ordinally; for artefacts built from DOM). |
| `.Versioning` | `readonly record struct SchemaVersion(int Major, int Minor)`: `TryParse("1.0")`, `ToString()`; `enum SchemaCompatibility { Supported, NewerMinor, UnsupportedMajor }`; `SchemaVersion.Evaluate(SchemaVersion supported, SchemaVersion actual)`. |
| `.Hashing` | `readonly struct ContentHash` (32 bytes, `ToString()` = 64 lowercase hex, `Parse`); `static ContentHasher`: `Compute(ReadOnlySpan<byte>)`, `ComputeAsync(Stream, CancellationToken)`, `ComputeFileAsync(string, CancellationToken)`; `sealed record TreeEntry(RelativePath Path, long Size, ContentHash Hash)`; `static TreeFingerprint.Compute(IEnumerable<TreeEntry>, PathPolicy) → ContentHash`. |
| `.Packages` | `interface IPackageLocator { Result<string> GetPackageRoot(string id, string version, string sha512); }` (contract only; no implementation in Core). |

Test support (`tests/Tailor.Testing`, namespace `Tailor.Testing`; no package or project references; not referenced by any `src` project):

| Type | API |
|---|---|
| `static class Golden` | `AssertMatches(string actual, string name, GoldenOptions? options = null, [CallerFilePath] string callerPath = "")`. Resolves `<dir of callerPath>/Golden/<caller file name without .cs>/<name>.golden<ext>`; `name` may contain `/`-separated segments (no `..`, not rooted). Update mode when `DOTNET_REPACK_UPDATE_GOLDEN=1`. Delegates to `GoldenStore`. |
| `sealed record GoldenOptions` | `Extension` (`.json` default, `.txt` for text); `Scrubbers` (ordered ordinal literal `(find, replacement)` pairs applied to `actual` only); `WithTemp(string tempRoot)` → `{TEMP}`, `WithRepoRoot()` → `{REPO}` (directory containing `Tailor.slnx`, found upward from `callerPath`); path scrubbers match both the raw and the JSON-escaped (`\\`) form. |
| `sealed class GoldenStore` | `GoldenStore(bool update)`; `AssertMatches(string actual, string goldenPath, GoldenOptions?)`. Testable without the env var. |
| `sealed class GoldenMismatchException : Exception` | Message: golden path, received path, unified diff summary (first differing hunks, capped). |

Diagnostic codes owned: `RPK0001`–`RPK0099` (core). Code sub-range convention for M1/M2 (recorded here, enforced by `DiagnosticRegistry` only at category level):

| Range | Owner |
|---|---|
| `RPK0001`–`0099` | WU-100 |
| `RPK0100`–`0199` | WU-105 |
| `RPK1001`–`1099` / `1100`–`1199` | WU-101 (shared loading / AppSpec) |
| `RPK1200`–`1299` | WU-102 |
| `RPK1300`–`1399` | WU-103 |
| `RPK1400`–`1499` | WU-104 |
| `RPK2001`–`2099` / `2100`–`2199` / `2200`–`2299` / `2300`–`2399` | WU-200 / WU-201 / WU-202 / WU-203 |

## Design Notes

- Architecture [§3.1](../../Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies), [§13](../../Architecture/Tailor.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§15](../../Architecture/Tailor.architecture.md#15-determinism) are normative.
- `RelativePath` normalisation: `\` → `/`; collapse repeated separators; drop `.` segments; strip trailing `/`; empty → `Root`. Rejected (error result, `RPK0001`–`RPK0009`): rooted (`/x`, `\x`), drive (`C:\`, `C:x`), UNC / device (`\\server`, `\\?\`, `\\.\`), any `..` segment, NUL/control chars, Windows-invalid chars `<>:"|?*`, segments ending in `.` or space, reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`, with or without extension). Reserved-name and invalid-char checks live on `PathPolicy.Windows` so a future Linux policy can differ.
- `ToFullPath` checks confinement lexically (`Path.GetFullPath` + prefix check with trailing separator). Reparse-point resolution is out of scope.
- Structural descriptors (`IsStructural = true`, [TS §24.4](../../Requirements/Transformation_Specification.md)) always remain `Error` regardless of policy or mode. `IsStructural` and `IsPolicyConfigurable` are mutually exclusive (registry rejects both set).
- Failure modes ([§13](../../Architecture/Tailor.architecture.md#13-diagnostics-failure-policy-and-exit-codes)): `Default` — warnings never count as failures. `Strict` — warnings count as failures (exit 3, mapped by WU-105). `Permissive` — warnings never count as failures, and `Error` diagnostics whose descriptor is `IsPolicyConfigurable` ([TS §24.2](../../Requirements/Transformation_Specification.md)) are downgraded to `Warning`; structural and other errors are unaffected. An explicit TransformSpec condition policy takes precedence over the mode for policy-configurable descriptors.
- Diagnostic sort order: `Location.Document`, `Location.JsonPointer`, `Location.Path` (policy comparer), `Code`, `Message` — all ordinal/total.
- Canonical JSON: UTF-8 without BOM, LF (deliberate for cross-machine byte stability, architecture [§15](../../Architecture/Tailor.architecture.md#15-determinism), [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 38), 2-space indent, trailing LF at EOF, property order = model declaration order, dictionaries serialised with ordinal key order, no timestamps. `UnsafeRelaxedJsonEscaping` is acceptable because output is never HTML-embedded; keeps `<culture>` readable.
- `TreeFingerprint`: entries sorted with `OrdinalIgnoreCaseThenOrdinalComparer`; per entry UTF-8 bytes of `{path}\0{size}\0{hash}\n`; SHA-256 over the concatenation. Duplicate paths under the policy comparer throw `ArgumentException`.
- All types are immutable and thread-safe (parallel inspection, risk R12).
- `Golden` comparison is ordinal text equality after applying only the supplied scrubbers; nothing else is normalised (a `\r\n` in `actual` fails unless the test supplies a scrubber for it). Golden files are read and written as UTF-8 without BOM, LF (`.gitattributes` forces LF under `tests/**/Golden/**`). On mismatch or missing golden: write `<name>.received<ext>` beside the golden path (git-ignored) and throw `GoldenMismatchException`. On match: delete a stale received file. Update mode writes the golden file and passes; CI never sets it (WU-002 guard).

## Acceptance Criteria

- [ ] AC-1 `Tailor.Core.csproj` has no `ProjectReference` and no `PackageReference` (test inspects the assembly's references: only framework assemblies).
- [ ] AC-2 `RelativePath.TryParse` normalises `a\\b/./c/` to `a/b/c` and `""`/`"."` to `Root` (theory test).
- [ ] AC-3 `RelativePath.TryParse` rejects each of `/a`, `\a`, `C:\a`, `C:a`, `\\s\x`, `\\?\C:\a`, `a/../b`, `..`, `a/b..` (trailing dot), `a /b` (trailing space), `a<b`, `a\u0001`, `con`, `aux.txt` with an error diagnostic carrying an `RPK00nn` code and no exception; `a/b..c` is accepted.
- [ ] AC-4 `PathPolicy.Windows.Comparer` treats `A/B.dll` and `a/b.DLL` as equal and orders a mixed list identically regardless of input order; `OrdinalIgnoreCaseThenOrdinalComparer` yields a total order (property/theory test over permutations).
- [ ] AC-5 `RelativePath.ToFullPath(root)` returns a path under `root` and `IsSameOrUnder` is correct for sibling-prefix cases (`app` vs `app2`).
- [ ] AC-6 `DiagnosticRegistry` rejects duplicate codes, codes not matching `^RPK\d{4}$`, and codes outside their category range (`RPK2xxx` registered as `Specification` fails).
- [ ] AC-7 `Diagnostic.Create` formats messages with invariant culture (test under `de-DE` current culture with a numeric argument).
- [ ] AC-8 `PolicyEvaluator` matrix test: every `ConditionPolicy` × `FailureMode` × `{structural, policy-configurable, other}` combination yields the documented outcome; structural diagnostics are always `Error`; under `Permissive` a policy-configurable `Error` becomes `Warning` and no warning counts as a failure.
- [ ] AC-9 `DiagnosticBag.ToSortedList()` is identical for any insertion order (permutation test).
- [ ] AC-10 `CanonicalJson.Serialize` output: no BOM, contains no `\r`, 2-space indentation, ends with exactly one `\n`, dictionary keys ordinal-sorted; the output for a sample model matches its committed golden file (`Golden/CanonicalJsonTests/sample-model.golden.json`) and is byte-stable across two calls.
- [ ] AC-11 `CanonicalJson.ReaderOptions` accept a document with `//` and `/* */` comments and trailing commas.
- [ ] AC-12 `JsonPointer` escapes `~` → `~0`, `/` → `~1`; `FromJsonPath("$.folders.root.folders[2].mask")` → `/folders/root/folders/2/mask`; `FromJsonPath("$['a/b']")` → `/a~1b`.
- [ ] AC-13 `SchemaVersion.TryParse` accepts `1.0`, `2.13`; rejects `1`, `1.0.0`, `v1.0`, `-1.0`, `01.0`; `Evaluate` returns `Supported`/`NewerMinor`/`UnsupportedMajor` correctly.
- [ ] AC-14 `ContentHasher` returns the known SHA-256 of `""` and `"abc"` as lowercase hex; stream and span overloads agree.
- [ ] AC-15 `TreeFingerprint.Compute` is independent of entry order, changes when any path/size/hash changes, and throws on case-insensitive duplicate paths.
- [ ] AC-16 `GoldenStore.AssertMatches` passes for identical text and leaves no `*.received.*` file (a pre-existing stale one is deleted).
- [ ] AC-17 On mismatch it throws `GoldenMismatchException` whose message contains the golden path and `-`/`+` diff lines, writes `<name>.received.json`, and leaves the golden file unchanged; a missing golden file fails the same way.
- [ ] AC-18 With `update: true` it writes the golden file (UTF-8 no BOM, no `\r`) and passes; `Golden` enables update mode only when `DOTNET_REPACK_UPDATE_GOLDEN=1`.
- [ ] AC-19 Scrubbers: `WithTemp`/`WithRepoRoot` replace raw and JSON-escaped paths with `{TEMP}`/`{REPO}`; without scrubbers, `a\r\nb` vs golden `a\nb` fails.
- [ ] AC-20 `Golden.AssertMatches(..., "x/y")` from `FooTests.cs` resolves `<test dir>/Golden/FooTests/x/y.golden.json`; `.txt` via `GoldenOptions.Extension`; names containing `..` or rooted names throw `ArgumentException`.
- [ ] AC-21 `Tailor.Testing.csproj` has no `PackageReference`/`ProjectReference`; no `src` project references it.

## Test Requirements

- xUnit v3 on MTP, project `tests/Tailor.Core.Tests/`. Golden-file comparison via `Tailor.Testing.Golden` for the canonical JSON sample only. `Golden` itself is tested in `tests/Tailor.Core.Tests/Testing/GoldenStoreTests.cs` against temp directories through `GoldenStore` (no env-var mutation).
- Names: classes `<TypeUnderTest>Tests`, methods `<Subject><Condition><ExpectedResult>` without underscores (e.g. `TryParseRejectsRootedPath`).
- Synthetic in-memory data only; no test-app matrix dependency.
- Mark tests `[Trait("WU", "100")]`.
- Run: `dotnet test --project tests/Tailor.Core.Tests --filter-trait "WU=100"` · focused: `--filter-class "*RelativePathTests"`.
- Record Test Evidence (command, commit, pass/fail counts) in the section below and in the PR.

## Definition of Done

- Build has zero warnings (`TreatWarningsAsErrors`); `dotnet test` green; `dotnet format --verify-no-changes` clean.
- All ACs ticked by the Verifier; plan status set to `Done`.
- Test Evidence recorded.

## Agent Notes

- Keep `Core` small and generic; no spec-kind, AppSpec or CLI knowledge.
- Downstream WUs register their own descriptors in static `*Diagnostics` classes within the sub-range table above; do not pre-declare their codes here.
- `SchemaVersion` lives here so WU-101 and WU-102 can proceed in parallel without sharing a Specifications helper for version checks.
- Prefer `System.Buffers`/`IncrementalHash` for streaming hashes.

## Open Questions

- **Resolved** — `--permissive` semantics: see Design Notes (architecture §13, §19 item 27).
- Whether reserved device names should be rejected for app-tree paths (they cannot exist on Windows) or only for spec-authored paths.
- Whether `Golden` tests should move to a dedicated `Tailor.Testing.Tests` project (adds a 16th test project) instead of `Tailor.Core.Tests`; proposed: stay in Core.Tests until `Tailor.Testing` grows.
- **Resolved** — `TreeFingerprint` ownership: Core (this WU); WU-305 reuses `Core.Hashing.TreeFingerprint` and adds only sidecar exclusion (architecture §3.2).

## Test Evidence

_To be completed by the implementer._
