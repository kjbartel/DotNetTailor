---
name: work-unit-workflow
description: "Use when selecting, implementing, verifying or creating a .NET Tailor work unit; covers status transitions, branches, evidence and definition of done."
---

# Work-Unit Workflow

The [master plan](../../../Docs/Plans/Tailor.plan.md) owns sequencing and status. The WU spec owns scope and acceptance criteria; the [architecture](../../../Docs/Architecture/Tailor.architecture.md) owns design.

- Start only when all dependencies are `Done`; select a `Not started` WU and use its exact spec path.
- Transition `Not started` → `In progress` on branch `wu/<id>-<slug>` → `In review` after implementation → `Done` only after Verifier evidence.
- Implement only the agreed WU. Keep commits reviewable; do not commit to the default branch.
- Run formatting/lint before relevant tests. Tests use WU traits and MTP conventions; no network by default.
- Record and reuse Test Evidence only for the matching functional-state fingerprint and environment.
- The Implementer does not tick acceptance criteria. The Verifier checks each criterion, records results, then updates spec and plan status.
- Write an ADR and update the architecture when an implementation requires a design change.

## Definition of Done

- Every WU acceptance criterion has evidence and is ticked by the Verifier.
- Required focused tests, build, formatting and lint checks pass; failures and exclusions are documented.
- The diff stays within scope, preserves safety and determinism, and includes any necessary user-facing documentation.
