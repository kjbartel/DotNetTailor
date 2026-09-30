---
description: "Implement one approved .NET Tailor work unit from its spec with focused tests and evidence."
agent: agent
---

Implement work unit `${input:wu}`.

1. Read `Docs/Plans/Tailor.plan.md`, `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md`, cited architecture sections, and linked requirements. Use the exact spec path and slug from the plan.
2. Confirm every dependency is `Done`; if not, stop and report the blocker. Set the selected WU to `In progress` and work on `wu/<id>-<slug>`.
3. Implement only the spec's scope. Follow `AGENTS.md`, project boundaries and applicable instructions. Do not edit requirements or tick acceptance criteria.
4. Add focused tests carrying the WU trait. Run applicable formatting/lint tools before tests, then run the narrowest relevant checks and build with warnings as errors.
5. Record the complete Test Evidence block, including impact, selected and excluded checks, exact state fingerprint, environment, commands and results. Reuse evidence only when it exactly matches.
6. Review the diff for scope, safety and determinism. Set status to `In review`; leave AC checkboxes for the Verifier.

Use the current repository solution filename from `README.md` and the root `AGENTS.md` commands.
