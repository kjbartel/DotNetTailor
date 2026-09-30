---
description: "Create a reviewable .NET Tailor work-unit specification from an approved plan row."
agent: Tailor Planner
---

Create the spec for work unit `${input:wu}`.

1. Read `Docs/Plans/Tailor.plan.md`, the architecture and authoritative requirement sections related to the work.
2. Copy the WU ID, slug, title, milestone, dependencies and target paths exactly from the plan row.
3. Create `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md` using the M0 WU-000/WU-001 specs as structural references.
4. Define scope, requirement traceability, deliverables, ordered `## Steps` (`- [ ] Sn … — done when …`), test requirements, definition of done, open questions and acceptance criteria with IDs and methods (`- [ ] AC-n (T|I|A|D) …`). Add the `Feature` header row (`planning-artifacts` skill).
5. Avoid duplicating architecture; cite its sections and use relative links. Identify unresolved decisions and dependencies explicitly.
6. Do not implement code, change the plan, or mark criteria complete. Return the new spec path and any open decisions for review.
