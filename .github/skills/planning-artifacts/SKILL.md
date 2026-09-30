---
name: planning-artifacts
description: "Use when creating or changing .NET Tailor epics, features, milestones, WU specs, steps, acceptance criteria, ADRs or spikes; defines hierarchy, IDs, statuses, phase gates and templates."
---

# Planning Artifacts

## Hierarchy

| Level | Meaning | File | ID | Budget |
|---|---|---|---|---|
| Epic | Outcome; groups features; may span milestones | `Docs/Epics/EP-nn-<slug>.epic.md` | `EP-01` | ≤ 1 page |
| Feature | One observable capability; standalone or in one epic | `Docs/Features/FT-nnn-<slug>.feature.md` | `FT-012` | ≤ 2 pages |
| Milestone | Release grouping of features (and standalone WUs) | `### M<n>` in [the plan](../../../Docs/Plans/Tailor.plan.md) | `M4` | criteria only |
| Work unit | One reviewable change; standalone or in one feature | `Docs/Specs/M<n>/WU-xxx-<slug>.spec.md` + plan row | `WU-301` | ≤ ~150 lines |
| Step | Ordered atomic change inside a WU | `## Steps` checklist in the WU spec | `WU-301/S2` | 1 line |
| ADR / spike | Decision / timeboxed research question | `Docs/Decisions/ADR-NNNN-<slug>.md` / `Docs/Spikes/` | — | — |

- Next free number per level. WU IDs use the milestone range (M3 → `WU-3xx`, M10 → `WU-10xx`).
- Parent links are one-way up (WU → feature → epic) plus a child list in the parent; keep both in sync.
- Existing standalone plans (e.g. `Docs/Plans/DotNetTailor-Rename.plan.md`) stay valid; convert to an epic when they need WUs.

## Statuses

All levels: `Draft` → `Ready` → `In progress` → `In review` → `Done`; also `Blocked (<reason>)`, `Superseded`. In the plan's WU table, legacy `Not started` means `Ready`. `Ready` = the level's gate below passed and the user approved.

## Acceptance Criteria

One line each: `- [ ] <ID> (<M>) <criterion>`, where `<M>` is the verification method: `T` test, `I` inspection, `A` analysis, `D` demo.

| Level | ID | Style | Verified by |
|---|---|---|---|
| Epic | `EC-n` | Measurable outcome | E2E/regression matrix, user demo |
| Feature | `FC-n` | Given/When/Then; include NFRs (determinism, safety, perf) | CLI/integration/golden tests |
| WU | `AC-n` | Contract inside one project boundary | Component/integration tests |
| Step | `Sn … — done when <check>` | Local check | Unit test, build or inspection |
| Milestone | `MC-n` | Release gate | CI, pack/install smoke, docs, user testing |

- Every `(T)` criterion needs ≥ 1 test tagged `[Trait("AC", "<artifact>/<ID>")]`, e.g. `WU-301/AC-2`, `FT-012/FC-1`. Check with `scripts/Test-Traceability.ps1 <spec>`.
- Do not restate child criteria in parents; a feature criterion is proven by integration tests, not by its WUs' ACs.

## Phase Gates

| Gate | Passes when | Approver |
|---|---|---|
| G1 Epic ready | Outcome measurable; scope in/out; requirement links resolve; top risks named | User |
| G2 Feature ready | Behaviour clear; every FC has a method; requirements traced; open questions resolved or accepted | User |
| G3 Design approved (skip if no design impact) | ADRs accepted; §3.1 boundaries kept; spikes closed; security/determinism impact stated | User |
| G4 WU ready | INVEST; deps `Done` or scheduled; ordered steps with "done when"; each AC has a method; parent FCs covered by ≥ 1 WU | User |
| G5 Review | Code Reviewer `Approve` at HEAD (`code-review` skill) | Code Reviewer / PR reviewers |
| G6 WU done | Steps and ACs ticked with evidence; traceability passes; Completion note | Verifier |
| G7 Feature done | All WUs `Done`; FCs proven; user docs updated; Completion note | Verifier, then user |
| G8 Milestone released | MCs proven; CI green on tag; `releasing.md` checks; user testing sign-off | User |
| Epic close | ECs proven or remaining work re-planned; Completion note | Verifier, then user |

Right-size: a bug fix or chore is a standalone WU (G4 only). A small feature without design impact skips G3. A spike is a WU whose deliverable is a report plus ADR.

## Rules

- WUs: one owning project boundary where possible; explicit dependencies; steps in implementation order.
- Add plan rows as `Draft` (`Ready` once approved); add features to the milestone section's `Features:` line and the mermaid graph. Never change other items' statuses.
- WU specs follow the M0 WU-000/WU-001 structure and `docs-specs.instructions.md`, plus the header rows and `## Steps` below. Cite, do not copy.
- Record unresolved decisions as open questions; do not guess.

## Templates

Epic:

```markdown
# EP-nn <title>

| Field | Value |
|---|---|
| Status | Draft |
| Milestones | M<n>, … |
| Requirements | [RQ §n](../Requirements/…) |

## Outcome
<1–3 sentences>

## Scope
**In**: … **Out**: …

## Features
| Feature | Milestone | Status |
|---|---|---|
| [FT-nnn](../Features/FT-nnn-<slug>.feature.md) <title> | M<n> | Draft |

## Success Criteria
- [ ] EC-1 (D) <measurable outcome>

## Risks and Open Questions
- …
```

Feature:

```markdown
# FT-nnn <title>

| Field | Value |
|---|---|
| Status | Draft |
| Epic | [EP-nn](../Epics/EP-nn-<slug>.epic.md) \| standalone |
| Milestone | M<n> |
| Requirements | [RQ §n](../Requirements/…) |
| Design | none \| [ADR-NNNN](../Decisions/…) \| [Architecture §n](../Architecture/Tailor.architecture.md#…) |

## Behaviour
<1–5 lines: user scenario(s)>

## Acceptance Criteria
- [ ] FC-1 (T) Given …, when …, then …

## Work Units
| WU | Title | Status |
|---|---|---|
| [WU-xxx](../Specs/M<n>/WU-xxx-<slug>.spec.md) | … | Draft |

## Open Questions
- …
```

WU spec additions (header rows, then `## Steps` before `## Acceptance Criteria`):

```markdown
| Feature | [FT-nnn](../../Features/FT-nnn-<slug>.feature.md) \| standalone |

## Steps
- [ ] S1 <atomic change> — done when <check>
```

Completion note (feature/epic, appended by the Verifier; same shape as the WU note in `work-unit-workflow`).
