# WU-100 core-primitives

| Field | Value |
|---|---|
| ID | WU-100 |
| Title | core-primitives |
| Milestone | M1 Core Primitives & Specification Documents |
| Status | Not started |
| Depends on | WU-000 |
| Parallel with | WU-001–WU-007 |
| Target project(s)/paths | `src/DotNetRepack.Core/`, `tests/DotNetRepack.Core.Tests/` |
| Size | L |

## Goal

Provide the dependency-free primitives every other project builds on: root-confined relative paths, the diagnostic model and code registry, failure-policy mapping, result types, deterministic ordering, canonical JSON, schema-version parsing, SHA-256 content hashing and the tree fingerprint.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Relative, portable, confined paths | [AS §23.3](../../Requirements/Application_Specification.md), [AS §24](../../Requirements/Application_Specification.md) (9), [AS §15.2](../../Requirements/Application_Specification.md), [TS §24.4](../../Requirements/Transformation_Specification.md) | [§3.1](../../Architecture/DotNetRepack.architecture.md#31-project-responsibilities-and-allowed-dependencies), [§7.1](../../Architecture/DotNetRepack.architecture.md#71-folder-matching), [§17](../../Architecture/DotNetRepack.architecture.md#17-security) |
| Diagnostics, policies | [TS §24](../../Requirements/Transformation_Specification.md), [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes) |
| Determinism, canonical JSON, fingerprint | [AS §3.6](../../Requirements/Application_Specification.md), [TS §3.6](../../Requirements/Transformation_Specification.md), [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md), [AS §5.2](../../Requirements/Application_Specification.md) | [§5](../../Architecture/DotNetRepack.architecture.md#5-artefacts), [§15](../../Architecture/DotNetRepack.architecture.md#15-determinism), [§19](../../Architecture/DotNetRepack.architecture.md#19-resolved--open-inconsistencies) item 22 |
| Schema version | [AS §5.3](../../Requirements/Application_Specification.md), [TS §5.3](../../Requirements/Transformation_Specification.md) | [§6.1](../../Architecture/DotNetRepack.architecture.md#61-common-rules) |

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

**Out**
- Reparse-point / junction canonicalisation (Platform, WU-600/WU-800).
- Directory enumeration of app trees (Model, WU-300).
- Exit-code mapping (Cli, WU-105).
- Diagnostics catalogue documentation (WU-1001).
- SHA-512 package hashing (WU-700).

## Deliverables

Namespace root `DotNetRepack.Core`. The project references no other repo project and no third-party packages.

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

- Architecture [§3.1](../../Architecture/DotNetRepack.architecture.md#31-project-responsibilities-and-allowed-dependencies), [§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes), [§15](../../Architecture/DotNetRepack.architecture.md#15-determinism) are normative.
- `RelativePath` normalisation: `\` → `/`; collapse repeated separators; drop `.` segments; strip trailing `/`; empty → `Root`. Rejected (error result, `RPK0001`–`RPK0009`): rooted (`/x`, `\x`), drive (`C:\`, `C:x`), UNC / device (`\\server`, `\\?\`, `\\.\`), any `..` segment, NUL/control chars, Windows-invalid chars `<>:"|?*`, segments ending in `.` or space, reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`, with or without extension). Reserved-name and invalid-char checks live on `PathPolicy.Windows` so a future Linux policy can differ.
- `ToFullPath` checks confinement lexically (`Path.GetFullPath` + prefix check with trailing separator). Reparse-point resolution is out of scope.
- Structural descriptors (`IsStructural = true`, [TS §24.4](../../Requirements/Transformation_Specification.md)) always remain `Error` regardless of policy or mode. `IsStructural` and `IsPolicyConfigurable` are mutually exclusive (registry rejects both set).
- Failure modes ([§13](../../Architecture/DotNetRepack.architecture.md#13-diagnostics-failure-policy-and-exit-codes)): `Default` — warnings never count as failures. `Strict` — warnings count as failures (exit 3, mapped by WU-105). `Permissive` — warnings never count as failures, and `Error` diagnostics whose descriptor is `IsPolicyConfigurable` ([TS §24.2](../../Requirements/Transformation_Specification.md)) are downgraded to `Warning`; structural and other errors are unaffected. An explicit TransformSpec condition policy takes precedence over the mode for policy-configurable descriptors.
- Diagnostic sort order: `Location.Document`, `Location.JsonPointer`, `Location.Path` (policy comparer), `Code`, `Message` — all ordinal/total.
- Canonical JSON: UTF-8 without BOM, LF, 2-space indent, trailing LF at EOF, property order = model declaration order, dictionaries serialised with ordinal key order, no timestamps. `UnsafeRelaxedJsonEscaping` is acceptable because output is never HTML-embedded; keeps `<culture>` readable.
- `TreeFingerprint`: entries sorted with `OrdinalIgnoreCaseThenOrdinalComparer`; per entry UTF-8 bytes of `{path}\0{size}\0{hash}\n`; SHA-256 over the concatenation. Duplicate paths under the policy comparer throw `ArgumentException`.
- All types are immutable and thread-safe (parallel inspection, risk R12).

## Acceptance Criteria

- [ ] AC-1 `DotNetRepack.Core.csproj` has no `ProjectReference` and no `PackageReference` (test inspects the assembly's references: only framework assemblies).
- [ ] AC-2 `RelativePath.TryParse` normalises `a\\b/./c/` to `a/b/c` and `""`/`"."` to `Root` (theory test).
- [ ] AC-3 `RelativePath.TryParse` rejects each of `/a`, `\a`, `C:\a`, `C:a`, `\\s\x`, `\\?\C:\a`, `a/../b`, `..`, `a/b..` (trailing dot), `a /b` (trailing space), `a<b`, `a\u0001`, `con`, `aux.txt` with an error diagnostic carrying an `RPK00nn` code and no exception; `a/b..c` is accepted.
- [ ] AC-4 `PathPolicy.Windows.Comparer` treats `A/B.dll` and `a/b.DLL` as equal and orders a mixed list identically regardless of input order; `OrdinalIgnoreCaseThenOrdinalComparer` yields a total order (property/theory test over permutations).
- [ ] AC-5 `RelativePath.ToFullPath(root)` returns a path under `root` and `IsSameOrUnder` is correct for sibling-prefix cases (`app` vs `app2`).
- [ ] AC-6 `DiagnosticRegistry` rejects duplicate codes, codes not matching `^RPK\d{4}$`, and codes outside their category range (`RPK2xxx` registered as `Specification` fails).
- [ ] AC-7 `Diagnostic.Create` formats messages with invariant culture (test under `de-DE` current culture with a numeric argument).
- [ ] AC-8 `PolicyEvaluator` matrix test: every `ConditionPolicy` × `FailureMode` × `{structural, policy-configurable, other}` combination yields the documented outcome; structural diagnostics are always `Error`; under `Permissive` a policy-configurable `Error` becomes `Warning` and no warning counts as a failure.
- [ ] AC-9 `DiagnosticBag.ToSortedList()` is identical for any insertion order (permutation test).
- [ ] AC-10 `CanonicalJson.Serialize` output: no BOM, contains no `\r`, 2-space indentation, ends with exactly one `\n`, dictionary keys ordinal-sorted; Verify snapshot of a sample model is byte-stable across two calls.
- [ ] AC-11 `CanonicalJson.ReaderOptions` accept a document with `//` and `/* */` comments and trailing commas.
- [ ] AC-12 `JsonPointer` escapes `~` → `~0`, `/` → `~1`; `FromJsonPath("$.folders.root.folders[2].mask")` → `/folders/root/folders/2/mask`; `FromJsonPath("$['a/b']")` → `/a~1b`.
- [ ] AC-13 `SchemaVersion.TryParse` accepts `1.0`, `2.13`; rejects `1`, `1.0.0`, `v1.0`, `-1.0`, `01.0`; `Evaluate` returns `Supported`/`NewerMinor`/`UnsupportedMajor` correctly.
- [ ] AC-14 `ContentHasher` returns the known SHA-256 of `""` and `"abc"` as lowercase hex; stream and span overloads agree.
- [ ] AC-15 `TreeFingerprint.Compute` is independent of entry order, changes when any path/size/hash changes, and throws on case-insensitive duplicate paths.

## Test Requirements

- xUnit v3 on MTP, project `tests/DotNetRepack.Core.Tests/`. Verify.XunitV3 for the canonical JSON snapshot only.
- Synthetic in-memory data only; no test-app matrix dependency.
- Mark tests `[Trait("WU", "100")]`.
- Run: `dotnet test --project tests/DotNetRepack.Core.Tests --filter-trait "WU=100"` · focused: `--filter-class "*RelativePathTests"`.
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
- **Resolved** — `TreeFingerprint` ownership: Core (this WU); WU-305 reuses `Core.Hashing.TreeFingerprint` and adds only sidecar exclusion (architecture §3.2).

## Test Evidence

_To be completed by the implementer._
