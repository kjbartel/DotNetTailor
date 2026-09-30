---
applyTo: "Docs/**/*.md"
description: "Documentation, work-unit specification, architecture decision record and spike-report conventions."
---

# Documentation and Specs

Link to [the architecture](../../Docs/Architecture/Tailor.architecture.md) and [the plan](../../Docs/Plans/Tailor.plan.md) rather than copying their full rules. Requirements in `Docs/Requirements/` are authoritative and must not be changed as part of implementation work.

- Work-unit specs use a header table, requirement traceability, scope, deliverables, acceptance criteria, test requirements, definition of done and open questions. Use `- [ ] AC-n` checkboxes; only the Verifier ticks them.
- Store specs at `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md`, matching the plan's status table exactly.
- Use relative Markdown links for requirement citations (for example, an `RQ §n` link to the source document and section); verify link targets exist.
- Record architecture changes in `Docs/Decisions/ADR-NNNN-<slug>.md` and update the architecture only with that decision.
- Spike reports in `Docs/Spikes/` state the question, method, findings, recommendation and decision/ADR; keep spike code isolated from `src/`.
- Keep generated artefacts and source line-ending policies from architecture §15; committed golden files and generated schema files use LF.
