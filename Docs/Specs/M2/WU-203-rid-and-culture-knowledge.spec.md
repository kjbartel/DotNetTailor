# WU-203 rid-and-culture-knowledge

| Field | Value |
|---|---|
| ID | WU-203 |
| Title | rid-and-culture-knowledge |
| Milestone | M2 Binary Inspection |
| Status | Not started |
| Depends on | WU-100 |
| Parallel with | WU-200–WU-202, M1 |
| Target project(s)/paths | `src/Tailor.Inspection/Rids/`, `src/Tailor.Inspection/Cultures/`, embedded data under `src/Tailor.Inspection/Data/`, `tests/Tailor.Inspection.Tests/Rids/`, `tests/Tailor.Inspection.Tests/Cultures/`, `tests/Tailor.IntegrationTests/Inspection/` |
| Size | M |

## Goal

Provide deterministic, host-independent knowledge of RIDs (parsing, portable graph, compatibility, `runtimes/<rid>/{lib,native}` layout conventions) and cultures (recognition, `en`/`en-*` pattern matching) for the `<rid>` and `<culture>` mask tokens and culture/RID selectors.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Platform / RIDs | [AS §8](../../Requirements/Application_Specification.md), [TS §13.3](../../Requirements/Transformation_Specification.md), [TS §20.2](../../Requirements/Transformation_Specification.md), [RQ §11](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§7.1](../../Architecture/Tailor.architecture.md#71-folder-matching) (`<rid>` token), [§12](../../Architecture/Tailor.architecture.md#12-platform-abstraction) (RID knowledge only in platform projects and Inspection) |
| Cultures | [AS §10.3](../../Requirements/Application_Specification.md), [AS §17](../../Requirements/Application_Specification.md), [TS §15.2](../../Requirements/Transformation_Specification.md) | [§7.1](../../Architecture/Tailor.architecture.md#71-folder-matching) (`<culture>` token), [§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions) (`culture`, `rid` predicates) |
| Determinism | [AS §3.6](../../Requirements/Application_Specification.md), [TS §3.6](../../Requirements/Transformation_Specification.md) | [§15](../../Architecture/Tailor.architecture.md#15-determinism) |

## Scope

**In**
- `Rid` parsing into OS / version / qualifier / architecture; portable vs non-portable (legacy) classification.
- Embedded RID graph (portable graph + documented legacy RIDs) with fallback chains and compatibility check.
- `RuntimeAssetPath` parser for `runtimes/<rid>/lib/<tfm>/…` and `runtimes/<rid>/native/…`.
- `CultureCatalogue` (embedded, deterministic) + BCP-47 syntax check + optional `CultureInfo` cross-check safe under invariant globalisation.
- `CulturePattern` (`en`, `en-*`, `*`, explicit names) matching.

**Out**
- Folder mask evaluation (WU-300); selector evaluation (WU-500).
- Default/host RID decisions (`IPlatformKnowledge`, `Platform.Windows`).

## Deliverables

| Namespace | Type | API |
|---|---|---|
| `…Inspection.Rids` | `readonly record struct Rid` | `static bool TryParse(string, out Rid)`, `Value` (lower-case), `Os`, `OsVersion?`, `Qualifier?` (e.g. `musl`), `Architecture?` (`x64`, `x86`, `arm64`, `arm`, …), `IsPortable`, `ToString()` |
| | `interface IRidGraph` / `EmbeddedRidGraph` | `bool IsKnown(Rid)`, `IReadOnlyList<Rid> GetFallbacks(Rid)` (self first, ending in `any`), `bool IsCompatible(Rid target, Rid asset)` (asset in target's fallback chain), `Rid? ToPortable(Rid)` (legacy → portable, e.g. `win10-x64` → `win-x64`) |
| | `static RuntimeAssetPath` | `bool TryParse(RelativePath, out RuntimeAsset)`; `RuntimeAsset(Rid Rid, RuntimeAssetKind Kind /* Lib, Native */, string? Tfm, RelativePath RemainingPath)`; matches `runtimes/<rid>/lib/<tfm>/…` and `runtimes/<rid>/native/…` at any depth prefix |
| `…Inspection.Cultures` | `interface ICultureCatalogue` / `EmbeddedCultureCatalogue` | `bool IsKnown(string name)`, `string? Normalise(string name)` (canonical casing, e.g. `zh-hant` → `zh-Hant`), `bool IsNeutral(string)`, `string? Parent(string)` |
| | `CultureInfoCrossCheck` | `CultureCheckResult Check(string name)` → `Known`, `Unknown`, `Unavailable` (invariant globalisation / predefined-only mode) |
| | `readonly record struct CulturePattern` | `static bool TryParse(string, out CulturePattern)`, `bool Matches(string cultureName)` |
| | `static CultureDiagnostics` | shared `TLR2300`–`TLR2399` with RIDs |

Diagnostics (minimum): `TLR2301` invalid RID syntax; `TLR2302` unknown RID (warning); `TLR2310` invalid culture pattern; `TLR2311` unknown culture name (warning).

Embedded data (`EmbeddedResource`, canonical JSON, committed):
- `Data/rid-graph.json`: portable RID graph for .NET 8+ (`any`, `base`, `win`, `win-x86`, `win-x64`, `win-arm64`, `unix`, `linux`, `linux-{x64,arm64,arm,musl-*}`, `osx`, `osx-{x64,arm64}`, …) plus legacy aliases (`win7-*`, `win8-*`, `win81-*`, `win10-*`, `alpine*`, …) mapped to portable parents. Source and licence attribution recorded in a header member (`source`, `license`).
- `Data/cultures.json`: sorted list of culture names (e.g. from .NET's ICU culture set plus `qps-ploc`, `zh-Hans`, `zh-Hant` and the SDK's satellite culture set), with neutral/parent info. Generated once by a documented script or test helper; not regenerated at runtime.

## Design Notes

- **Determinism first** ([§15](../../Architecture/Tailor.architecture.md#15-determinism)): recognition decisions use only the embedded catalogues. ICU/NLS differences between hosts and `InvariantGlobalization=true` must not change results. `CultureInfoCrossCheck` is diagnostic-only (e.g. an `inspect` hint), never a matching input; under invariant mode it returns `Unavailable` without throwing (`CultureNotFoundException` caught).
- Culture comparison is ordinal-ignore-case; output uses canonical casing from the catalogue.
- `CulturePattern`: `en` matches only `en`; `en-*` matches any culture whose name starts with `en-` and has at least one further subtag (`en-US`, `en-GB`, `en-Latn-US`), not `en` itself; `*` matches any known culture; explicit names match exactly. Patterns must be syntactically valid BCP-47 prefixes; `e*`, `en*`, `*-US` are invalid (`TLR2310`).
- `<culture>` token matching for folders (WU-300) = `IsKnown(name)`; unknown culture-looking folders are content, not resources ([AS §10.7](../../Requirements/Application_Specification.md)).
- RID parsing: `os[.version][-qualifier]-arch` per the .NET RID catalogue; lower-case normalisation; `any`, `base`, `win`, `unix`, `linux` are valid architecture-less RIDs.
- Compatibility: `win-x64` target accepts assets for `win-x64`, `win`, `any`; rejects `win-x86`, `win-arm64`, `linux-x64`. Legacy asset RIDs (`win10-x64`) are compatible with `win-x64` via `ToPortable` (NuGet packages still ship them).
- No hard-coded RID strings outside this namespace and `Platform.*` ([§12](../../Architecture/Tailor.architecture.md#12-platform-abstraction)).

## Acceptance Criteria

- [ ] AC-1 `Rid.TryParse` parses `win-x64`, `win10-x64`, `linux-musl-arm64`, `osx.13-arm64`, `any`, `win` into expected components; rejects `""`, `win_x64`, `-x64`, `win-`, `win x64`.
- [ ] AC-2 `GetFallbacks(win-x64)` is exactly `[win-x64, win, any]` (`base` is never returned as a fallback).
- [ ] AC-3 `IsCompatible(win-x64, …)` is true for `win-x64`, `win`, `any`, `win10-x64`, `win7-x64`; false for `win-x86`, `win-arm64`, `linux-x64`, `osx-arm64`, `unix`.
- [ ] AC-4 `RuntimeAssetPath.TryParse` parses `runtimes/win-x64/native/e_sqlite3.dll` (Native, no TFM), `runtimes/linux-x64/lib/net8.0/Foo.dll` (Lib, `net8.0`), `Plugins/A/runtimes/win/lib/netstandard2.0/Bar.dll` (nested); rejects `runtimes/win-x64/other/x.dll` and `runtimes/notarid!/native/x.dll`.
- [ ] AC-5 `CultureCatalogue.IsKnown` is true for `en`, `en-US`, `de`, `zh-Hans`, `zh-Hant`, `pt-BR`, `qps-ploc` (case-insensitive) and false for `xx-YY`, `runtimes`, `Plugins`, `x64`, `lib`.
- [ ] AC-6 `CulturePattern`: `en` matches `en` only; `en-*` matches `en-US`, `EN-gb`, `en-Latn-US`, not `en`, `eng`, `fr-EN`; `*` matches `de`; invalid patterns `e*`, `en*`, `*-US`, `en--US` → `TLR2310`.
- [ ] AC-7 Recognition results are identical when the test process runs with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` (child test run or `AppContext` switch in an isolated test) — the catalogue test set yields the same answers; `CultureInfoCrossCheck` returns `Unavailable` there without throwing.
- [ ] AC-8 Embedded `rid-graph.json` and `cultures.json` are canonical JSON (re-serialising yields identical bytes) and contain `source`/`license` attribution.
- [ ] AC-9 Matrix (`Category=Matrix`): every culture folder in `ConsoleApp` variants (`en`, `de`, `fr`) is `IsKnown`; every `runtimes/<rid>/…` path in the `ConsoleApp/*-fdportable-il` variants parses via `RuntimeAssetPath`.

## Test Requirements

- xUnit v3 in `tests/Tailor.Inspection.Tests/`; pure in-memory theory tests; trait `WU=203`.
- Matrix check (AC-9) in `tests/Tailor.IntegrationTests/Inspection/RidCultureMatrixTests`; traits `Category=Integration`, `Category=Matrix`, `WU=203`; skip with reason locally when the matrix is absent, CI must not skip.
- AC-7 needs an invariant-mode run: use a separate test class that sets `System.Globalization.Invariant` via `runtimeconfig.template.json` in a dedicated small test project, or spawn the test host with the env var — choose one and document it in Test Evidence.
- Run: `dotnet test --project tests/Tailor.Inspection.Tests --filter-trait "WU=203"`; `dotnet test --project tests/Tailor.IntegrationTests --filter-trait "WU=203"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green (unit + matrix in CI); `dotnet format --verify-no-changes` clean.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- Derive RID data from dotnet/runtime's `PortableRuntimeIdentifierGraph.json` (MIT) and record the upstream commit in the data header.
- Keep catalogues immutable singletons; they are hot paths in folder matching.

## Open Questions

- The request asks for "CultureInfo validation"; this spec makes the embedded catalogue authoritative and `CultureInfo` a non-authoritative cross-check, because host ICU data and invariant mode would otherwise break determinism. Confirm.
- Which culture set is authoritative (ICU full list vs the ~13 SDK satellite cultures plus common ones)? Proposed: the ICU culture list shipped with .NET 10 at the time of the WU, frozen.
- Adding a separate invariant-mode test project would change the architecture's "one test project per src project" rule ([§3](../../Architecture/Tailor.architecture.md#3-solution-layout)).

## Test Evidence

_To be completed by the implementer._
