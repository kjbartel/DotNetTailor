# WU-507 additions-handler

| Field | Value |
|---|---|
| ID | WU-507 |
| Title | additions-handler |
| Milestone | M5 Transformation Planning & Dry-run |
| Status | Not started |
| Depends on | WU-503 |
| Parallel with | WU-504, WU-505, WU-601, WU-702 |
| Target project(s)/paths | `src/Tailor.Transforms/Additions/`, `tests/Tailor.Transforms.Tests/Additions/` |
| Size | S |
| Branch / PR | `wu/507-additions-handler` / `WU-507: additions-handler` |

## Goal

Implement the `additions` handler for user-provided files ([TS §19](../../Requirements/Transformation_Specification.md#19-addition-rules)): each `additions[]` entry produces an `Add` action with a deterministic destination, full provenance and a pinned content hash. Collisions with existing or other added files are errors, never implicitly resolved.

## Requirement Traceability

| Requirement | Coverage |
|---|---|
| [TS §19](../../Requirements/Transformation_Specification.md#19-addition-rules) | Addition sources, deterministic destination, provenance |
| [TS §18.5](../../Requirements/Transformation_Specification.md#18-file-and-folder-layout-rules), [TS §22.4](../../Requirements/Transformation_Specification.md#22-rule-precedence-and-conflict-resolution) | Output-path collisions are errors |
| [TS §29](../../Requirements/Transformation_Specification.md#29-external-sources-and-credentials), [TS §3.6](../../Requirements/Transformation_Specification.md#3-design-principles) | Pinned identities, determinism |
| Architecture [§8](../../Architecture/Tailor.architecture.md#8-selectors-precedence-and-actions), [§4](../../Architecture/Tailor.architecture.md#4-processing-pipeline), [§17](../../Architecture/Tailor.architecture.md#17-security) | `Add` action with `UserFile` source, phase 6, path confinement |
| Plan M5 criterion "user-provided file additions" | AC-1–AC-8 |

## Scope

**In**
- `AdditionsHandler : ITransformationHandler` (`Category = "additions"`, `Phase = FilteringLayout`).
- `additions[].source.file`: spec-relative path, resolved relative to the TransformSpec document that declares it (WU-103 provenance), confined to that document's directory tree.
- `destination` (root-relative folder or file path, WU-102 `RelativePath`) → `Add` action with `ActionSource.UserFile {Document, Path, Hash}` and `ExpectedHash`.
- Collision checks against the projected state and other additions (reported by the WU-503 `OutputCollisionCheck`, `RPK5301`); a user file that would silently replace an input file is an error unless a rule explicitly removes that input file.
- Classification of added files in the projected state via the AppSpec catch-all (no new AppSpec rules).

**Out**
- `additions[].source.package` (package-sourced additions need acquisition; they are rejected with a "not supported before M7" error in v0.2.0 and delegated to the package-aware handlers later — see Open Questions).
- Execution of `Add` actions (WU-601 `UserFile` executor). Projected AppSpec derivation (WU-505).

## Deliverables

- `Additions.AdditionsHandler`, `Additions.UserFileResolver` (document-relative resolution, confinement, SHA-256 via `ContentHasher`).
- DI registration by category.
- Diagnostics (proposed `RPK5601`–`RPK5699`, Planning/Transforms category, unused so far): `RPK5601` source file not found, `RPK5602` source path escapes the declaring document's directory, `RPK5603` destination escapes the output root, `RPK5604` addition would replace an input file without an explicit remove, `RPK5605` package-sourced addition not supported yet.

## Design Notes

- Destination rule: when `destination` ends with `/` or names an existing projected folder, the file keeps its source file name; otherwise `destination` is the full target path. The rule is deterministic and recorded in the action.
- Provenance: `Provenance {Handler = "additions", RuleId = additions[].id, Document, JsonPointer}` from WU-103.
- Hashing at plan time pins the content; WU-601 verifies it at execution (`RPK6101` on change).
- Additions are applied at the end of phase 6, so later phases (R2R, ConfigGeneration) see them. Filtering rules do not remove files added in the same phase.
- No network, no package access in this WU.

## Acceptance Criteria

- [ ] AC-1 An addition of `extras/readme.txt` (spec-relative) to `docs/` yields one `Add` action with destination `docs/readme.txt`, `UserFile` source, SHA-256 and full provenance.
- [ ] AC-2 An addition with a full destination path (`config/app.override.json`) uses exactly that path.
- [ ] AC-3 Source paths are resolved relative to the declaring document; an addition declared in an included document resolves relative to that document (WU-103 fixture).
- [ ] AC-4 A missing source yields `RPK5601`; a source escaping the declaring document's directory yields `RPK5602`; a destination escaping the root yields `RPK5603`.
- [ ] AC-5 Two additions to the same destination, or an addition onto a preserved input file, yield `RPK5301` / `RPK5604`; with an explicit exclude rule for the input file, the replacement is planned.
- [ ] AC-6 A `package` source yields `RPK5605` (error) in v0.2.0.
- [ ] AC-7 The added file appears in `ProjectedState` and is classified by the AppSpec catch-all.
- [ ] AC-8 Two plans with the same inputs are byte-identical.

## Test Requirements

- Unit: `tests/Tailor.Transforms.Tests/Additions/` with synthetic EAMs, temp spec directories and a real `Planner` with only this handler registered. Trait `WU=507`.
- Run: `dotnet test --project tests/Tailor.Transforms.Tests --filter-trait "WU=507"`.
- Record Test Evidence in the PR.

## Definition of Done

- All ACs ticked by the Verifier; CI green; `RPK56xx` codes listed for WU-1001; changes limited to target paths (plus the plan status row).

## Agent Notes

- Transforms references Planning (architecture §3.1). Do not reference Execution.
- Add one scenario TransformSpec `additions` under `tests/Tailor.IntegrationTests/TransformSpecs/` for WU-506/WU-603 reuse.

## Open Questions

- Which WU implements package-sourced additions (`additions[].source.package`) once acquisition exists (M7)? Proposed: extend this handler after WU-700 with a follow-up WU; until then `RPK5605`.
