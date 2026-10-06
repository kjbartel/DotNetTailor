# WU-101 appspec-model-and-schema

| Field | Value |
|---|---|
| ID | WU-101 |
| Title | appspec-model-and-schema |
| Milestone | M1 Core Primitives & Specification Documents |
| Status | Ready |
| Depends on | WU-100, WU-007 |
| Parallel with | WU-102, WU-105, M2 |
| Target project(s)/paths | `src/Tailor.Specifications/` (`Common/`, `AppSpec/`), `schemas/appspec/v1/appspec.schema.json`, `tests/Tailor.Specifications.Tests/` (`Common/`, `AppSpec/`, `Fixtures/AppSpec/`) |
| Size | L |

## Goal

Define the v1 AppSpec object model, its tolerant read and canonical write, the generated and committed JSON Schema, and the shared, kind-agnostic document loading infrastructure (header check, schema validation, diagnostics) that WU-102 reuses.

## Requirement Traceability

| Area | Requirements | Architecture |
|---|---|---|
| Identity, schema, versioning | [AS §5](../../Requirements/Application_Specification.md), [AS §24](../../Requirements/Application_Specification.md) (11) | [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules) |
| Model sections | [AS §6](../../Requirements/Application_Specification.md)–[§18](../../Requirements/Application_Specification.md), [AS §21](../../Requirements/Application_Specification.md) (`includes` as data), [AS §23.3](../../Requirements/Application_Specification.md) | [§6.2](../../Architecture/Tailor.architecture.md#62-appspec-shape-illustrative-the-wu-101-schema-is-normative), [§7](../../Architecture/Tailor.architecture.md#7-effective-application-model-semantics) |
| State-only, exclusions | [AS §1](../../Requirements/Application_Specification.md), [AS §19.3](../../Requirements/Application_Specification.md), [AS §20](../../Requirements/Application_Specification.md), [RQ §8](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) items 1, 17, 19, 22 |
| Canonical output, determinism | [AS §3.6](../../Requirements/Application_Specification.md), [AS §3.5](../../Requirements/Application_Specification.md) | [§5](../../Architecture/Tailor.architecture.md#5-artefacts), [§15](../../Architecture/Tailor.architecture.md#15-determinism) |
| Validator library | [RQ §12](../../Requirements/Repackage_tool_Requirements_v1.1.md) | [§20](../../Architecture/Tailor.architecture.md#20-open-questions), [§21](../../Architecture/Tailor.architecture.md#21-spikes-feeding-this-document) (WU-007 ADR) |

## Scope

**In**
- Shared infrastructure in `Tailor.Specifications.Common` (used by WU-102/WU-103).
- AppSpec v1 model covering every AS section below, serialisation, per-document structural checks.
- Schema generation with `JsonSchemaExporter`, committed schema, drift test.
- Sample fixtures, including the `folderspec.json`-equivalent AppSpec.

**Out**
- Include resolution / merging (WU-103). `includes` is modelled as data only.
- Validation against a tree, cross-reference checks (`idRef` targets, `folder:<id>` targets) (WU-300, WU-403).
- Capability assessment and validation state (not in the AppSpec, [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) items 1, 17).
- `knownUnresolved` references (open, [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 20).
- CLI `schema export` wiring (WU-105).

## Deliverables

### Shared (`Tailor.Specifications.Common`)

| Type | Responsibility |
|---|---|
| `enum SpecificationKind { AppSpec, TransformSpec, Plan }` | `kind` values |
| `sealed record GeneratorInfo(string Tool, string Version)` | `generator` |
| `abstract record SpecificationDocument` | `Schema` (`$schema`), `Kind`, `SchemaVersion` (string in JSON, parsed with `Core.Versioning.SchemaVersion`), `Generator?`, `Includes` (`IReadOnlyList<string>`) |
| `sealed class SpecificationFormat<TDocument>` | Per-kind descriptor: kind, supported `SchemaVersion`, `JsonSerializerOptions`, schema id |
| `static SpecificationReader` | `Result<TDocument> Read<TDocument>(ReadOnlyMemory<byte> utf8, string documentName, SpecificationFormat<TDocument>)`; pipeline below |
| `static SpecificationWriter` | `byte[] Write<TDocument>(TDocument, SpecificationFormat<TDocument>)` via `CanonicalJson` |
| `interface ISchemaValidator` + one adapter for the WU-007 library | `IReadOnlyList<SchemaViolation> Validate(JsonNode, JsonNode schema)` with instance JSON pointer + message |
| `static SchemaGenerator` | `JsonNode Generate(Type, JsonSerializerOptions, SchemaMetadata)`; post-processing below; `byte[] GenerateCanonical(...)` |
| `static SpecificationDiagnostics` | `TLR1001`–`TLR1099` |

Reader pipeline (stop at first failing stage, except that stage 3 reports all violations):
1. Parse with `CanonicalJson.ReaderOptions` → `TLR1001` malformed JSON (line/column in message).
2. Header: missing/invalid `kind` (`TLR1002`), kind mismatch (`TLR1003`), missing/unparseable `schemaVersion` (`TLR1004`), unsupported major (`TLR1005`, error), newer minor (`TLR1006`, warning).
3. Schema validation against the generated schema → `TLR1010` per violation, location = document + JSON pointer.
4. Deserialise (`UnmappedMemberHandling.Disallow`) → `TLR1011` with pointer from `JsonException.Path`.
5. Per-document structural checks supplied by the format (`TLR11xx`/`TLR12xx`).

### AppSpec (`Tailor.Specifications.AppSpec`)

Root `sealed record AppSpecDocument : SpecificationDocument`. All top-level sections are optional in the schema (needed for subsidiary documents, WU-103); completeness is checked later by WU-403.

| JSON member | Model | AS ref |
|---|---|---|
| `application` | `ApplicationInfo`: `id`, `displayName?`, `version?`, `publisher?`, `product?`, `entryPoints[]` (`EntryPoint`: `id?`, `host?`, `assembly?`, `subsystem` `console\|gui`, `kind?` `application\|utility\|service\|pluginHost\|tool`), `confidence?` | §6, §7.3 |
| `execution` | `ExecutionInfo`: `deploymentModel` `frameworkDependent\|selfContained`, `runtimeConfig[]?`, `depsJson[]?` (root-relative paths), `confidence?` | §7 |
| `platform` | `PlatformInfo`: `os[]?`, `rids[]`, `architectures[]?`, `confidence?` | §8 |
| `frameworkContexts[]` | `FrameworkContext`: `id`, `tfm`, `frameworks[]` (`name`, `version`), `appliesTo?` (`folderIds[]`, `entryPoints[]`), `confidence?` | §9 |
| `classifications` | `ClassificationSet`: `catchAll` (group id, default `content`), `groups[]` (`id`, `priority`, `match`: `glob[]`, `excludeGlob[]?`, `is[]?` from `managed\|native\|mixed\|r2r\|satellite\|referenceAssembly`, `files[]?` explicit exceptions) | §11, §13, §14 |
| `associations[]` | `AssociationRule`: `type` (`symbols\|xmlDoc\|assemblyConfig\|runtimeConfig\|depsJson\|resources\|<extensible string>`), `pattern` (`{name}`, `{file}`, `<culture>` tokens), `primary?` (classification group ids), `required` | §12, §17, §18 |
| `folders` | `FolderModel`: `definitions[]` (`FolderNode`), `root` (`FolderNode`) | §10 |
| `FolderNode` | `id?`, `idRef?`, `mask?`, `role?` (extensible string; built-ins `applicationRoot\|component\|plugin\|runtime\|platformAssets\|resources\|content`), `recurse?`, `classifications[]?` (applicable group ids), `references[]?` (`current\|parent\|root\|folder:<id>\|<root-relative path>`), `duplicates?` `error\|first\|highestVersion`, `folders[]?`, `confidence?` | §10, §15, §16 |
| `confidence` | `enum Confidence { Explicit, Derived, Inferred, Unknown }` | §9.4 |

Per-document structural checks (`TLR1100`–`1199`): duplicate `id` among `folders.definitions`, among siblings of any `folders` array, among `classifications.groups`, among `frameworkContexts`; a non-root `FolderNode` with neither `idRef` nor `mask`; path-form `references` entries that fail `RelativePath` parsing; `mask`/`glob` patterns that are rooted or contain a `..` segment (patterns are not parsed as `RelativePath` because `*`, `<culture>`, `<rid>` are allowed); `catchAll` not naming a defined group when `groups` is present in the same document is **not** checked here (may come from an include).

### Schema

- `schemas/appspec/v1/appspec.schema.json` generated by `SchemaGenerator`, committed, canonical JSON.
- Post-processing: `$schema` = JSON Schema 2020-12, `$id` (placeholder URI constant, see Open Questions), `title`, `description` from `[Description]`, `additionalProperties: false` on every object, `required` from `required` members, `kind` as `const`, `schemaVersion` `pattern ^\d+\.\d+$`, string-or-array members as `oneOf`.
- `AppSpecFormat.Instance` exposes supported version `1.0`.

### Fixtures (`tests/Tailor.Specifications.Tests/Fixtures/AppSpec/`)

- `minimal.appspec.json` (header only).
- `architecture-example.appspec.json` (the [§6.2](../../Architecture/Tailor.architecture.md#62-appspec-shape-illustrative-the-wu-101-schema-is-normative) example, completed to be schema-valid).
- `folderspec-equivalent.appspec.json`: same intent as [folderspec.json](../../Requirements/folderspec.json) in canonical v1 form per [§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 19 — `<lang_spec>` → `<culture>`, `\` → `/`, `duplicate_references: false` → `duplicates: "error"`, snake_case → camelCase, `file_types` → `classifications`, `id_ref` → `idRef`, recursive `plugins` nesting via `idRef` with `mask: "**"` kept, `reference_paths` mapped to `references` (see Open Questions for `current/runtimes`).
- `commented.appspec.json` (comments + trailing commas), and invalid fixtures: `unknown-member`, `wrong-kind`, `bad-version`, `major-2`, `minor-newer-known-members`, `minor-newer-unknown-member`, `escaping-reference`, `duplicate-sibling-id`.

## Design Notes

- Header and version rules: [§6.1](../../Architecture/Tailor.architecture.md#61-common-rules). "Newer minor accepted with a warning only if every member is known" = stage 2 emits `TLR1006` warning, stage 3/4 still reject unknown members.
- Comments are not preserved; round-trip is semantic (read → write → read equal) and canonical output is byte-stable ([§5](../../Architecture/Tailor.architecture.md#5-artefacts)).
- String-or-array members (e.g. `glob`, `is`) use a `StringList` type with a custom converter; `JsonSchemaExporter` cannot describe custom converters, so `SchemaGenerator` patches those nodes via `TransformSchemaNode`. Canonical write always emits arrays.
- Extensible vocabularies (`role`, association `type`, group ids) are strings in the schema with built-in constants in code ([AS §10.6](../../Requirements/Application_Specification.md), [AS §11.2](../../Requirements/Application_Specification.md)).
- `generator.version` is written as supplied; tests set it explicitly so golden files do not depend on the build's informational version.
- No creation timestamps in the model ([§19](../../Architecture/Tailor.architecture.md#19-resolved--open-inconsistencies) item 22).

## Acceptance Criteria

- [ ] AC-1 `AppSpecDocument` models every row of the member table above; a reflection test asserts each JSON member name exists in the generated schema.
- [ ] AC-2 Round-trip over every valid fixture: `Read → Write → Read` yields an equal model and `Write` output is byte-identical on the second pass (golden file per fixture).
- [ ] AC-3 `commented.appspec.json` reads successfully and its canonical output contains no comments and no trailing commas.
- [ ] AC-4 Canonical output: UTF-8 no BOM, LF only, 2-space indent, trailing LF, no timestamp-like members.
- [ ] AC-5 `wrong-kind` → `TLR1003`; `bad-version` → `TLR1004`; `major-2` → `TLR1005` error; `minor-newer-known-members` → success with `TLR1006` warning; `minor-newer-unknown-member` → `TLR1006` warning plus an error.
- [ ] AC-6 `unknown-member` yields `TLR1010` whose location has the document name and a JSON pointer to the offending member (e.g. `/folders/root/folders/1/masks`).
- [ ] AC-7 `escaping-reference` (`references: ["../x"]`) and an absolute `mask` yield `TLR11xx` errors with pointers; `duplicate-sibling-id` yields `TLR11xx` naming the id.
- [ ] AC-8 `folderspec-equivalent.appspec.json` is schema-valid and its golden file shows `<culture>`, `/` separators, `duplicates: "error"` and the recursive `plugins` `idRef`.
- [ ] AC-9 `SchemaDriftTests` regenerates the schema and fails with a diff message if it differs byte-wise from `schemas/appspec/v1/appspec.schema.json`; an env var `DOTNET_TAILOR_UPDATE_SCHEMAS=1` rewrites the file locally instead (never set in CI).
- [ ] AC-10 The committed schema declares `additionalProperties: false` on all object schemas, `kind` const `AppSpec`, and the `schemaVersion` pattern.
- [ ] AC-11 `ISchemaValidator` is implemented only by the adapter for the library chosen in the WU-007 ADR; no other project references that library.
- [ ] AC-12 Reading any fixture never throws; all failures surface as diagnostics (test iterates all invalid fixtures).

## Test Requirements

- xUnit v3 + golden files (`Tailor.Testing.Golden`) in `tests/Tailor.Specifications.Tests/`; fixtures copied to output (`<None Include="Fixtures/**" CopyToOutputDirectory="PreserveNewest" />`).
- No test-app matrix dependency; synthetic and fixture documents only. Trait `WU=101`.
- Run: `dotnet test --project tests/Tailor.Specifications.Tests --filter-trait "WU=101"` · focused: `--filter-class "*SchemaDriftTests"`.
- Record Test Evidence below and in the PR.

## Definition of Done

- Zero warnings; tests green; `dotnet format --verify-no-changes` clean.
- Schema committed and drift test passing.
- ACs ticked by the Verifier; plan status `Done`; Test Evidence recorded.

## Agent Notes

- Read the WU-007 ADR in `Docs/Decisions/` before choosing the validator package; add it to `Directory.Packages.props`.
- Build `Common/` first and keep it kind-agnostic: WU-102 is scheduled in parallel and consumes it. If WU-102 is already in progress, coordinate on the `Common/` API rather than duplicating it.
- Do not model `capabilities`, `validationState` or history fields.
- Follow the `schema-change` skill (WU-001) for committing schemas.

## Open Questions

- `$schema`/`$id` URI hosting ([§20](../../Architecture/Tailor.architecture.md#20-open-questions)); use a placeholder constant until decided.
- Grammar for compound reference entries such as FS `current\runtimes` / `root\runtimes`: proposed `current/runtimes` = path relative to the named root. The architecture lists only `current`, `parent`, `root`, `folder:<id>` and root-relative paths.
- `frameworkContexts[].appliesTo` is proposed to satisfy [AS §9.3](../../Requirements/Application_Specification.md); the architecture example has no scoping member.
- Explicit file exceptions (`match.files`) shape per [AS §11.3](../../Requirements/Application_Specification.md)/[§11.5](../../Requirements/Application_Specification.md) is proposed, not specified.
- **Resolved** — `schemas/**` is checked out LF (WU-000 D4), so AC-9's byte-wise drift check stands as written.

## Test Evidence

_To be completed by the implementer._
