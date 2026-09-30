---
name: work-unit-workflow
description: "Use when selecting, implementing, verifying or creating a .NET Tailor work unit; covers status transitions, branches, evidence and definition of done."
---

# Work-Unit Workflow

The [master plan](../../../Docs/Plans/Tailor.plan.md) owns sequencing and status. The WU spec owns scope and acceptance criteria; the [architecture](../../../Docs/Architecture/Tailor.architecture.md) owns design.

- Start only when all dependencies are `Done`; select a `Ready` (legacy `Not started`) WU and use its exact spec path.
- Transition `Ready` → `In progress` on branch `wu/<id>-<slug>` → `In review` after implementation → code review (`code-review` skill) until `Approve` → `Done` only after Verifier evidence.
- Implement `## Steps` in order; commit per step or per WU. The Implementer never ticks steps or ACs; it returns a step status table (`| Step | Implemented/Partial | Evidence |`) for the Verifier.
- Implement only the agreed WU. Keep commits reviewable; do not commit to the default branch.
- Run formatting/lint before relevant tests. Tests use WU and AC traits and MTP conventions; no network by default.
- Record and reuse Test Evidence only for the matching functional-state fingerprint and environment (`test-evidence` skill).
- The Implementer does not tick steps or acceptance criteria. The Verifier checks each, records results, then updates spec and plan status; when the last WU of a feature (or feature of an epic) is `Done`, it verifies the parent's criteria too (`planning-artifacts` gates G7/epic close).
- Write an ADR and update the architecture when an implementation requires a design change.

## Definition of Done

- Every step and acceptance criterion has evidence and is ticked by the Verifier.
- `.github/skills/planning-artifacts/scripts/Test-Traceability.ps1 <spec>` passes (every `(T)` AC has an `AC` trait; no orphans).
- Tailor Code Reviewer verdict is `Approve` at the verified HEAD (or a user waiver is recorded).
- Required focused tests, build, formatting and lint checks pass; failures and exclusions are documented.
- The diff stays within scope, preserves safety and determinism, and includes any necessary user-facing documentation.

## Completion Note

The Verifier appends this to the WU spec. Keep it to 5 lines or fewer, and link rather than repeat:

```markdown
## Completion
- **Summary**: <≤2 lines: what changed>
- **Commits/PR**: `<sha>`…`<sha>` | #<pr>
- **Code review**: Approve @ `<sha>`; resolved CR-…; deferred <none | WU/issue>
- **Evidence**: see Test Evidence above
- **Follow-ups**: <none | WU/issue links>
```
