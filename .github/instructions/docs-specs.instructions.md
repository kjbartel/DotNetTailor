---
applyTo: "Docs/**/*.md"
description: "Documentation, work-unit specification, architecture decision record and spike-report conventions."
---

# Documentation and Specs

Link to [the architecture](../../Docs/Architecture/Tailor.architecture.md) and [the plan](../../Docs/Plans/Tailor.plan.md) rather than copying their full rules. Requirements in `Docs/Requirements/` are authoritative and must not be changed as part of implementation work.

- Work-unit specs use a header table, requirement traceability, scope, deliverables, acceptance criteria, test requirements, definition of done and open questions. Use `- [ ] AC-n` checkboxes; only the Verifier ticks them.
- Store specs at `Docs/Specs/<Milestone>/<ID>-<slug>.spec.md`, matching the plan's status table exactly.
- Epics live at `Docs/Epics/EP-nn-<slug>.epic.md` and features at `Docs/Features/FT-nnn-<slug>.feature.md`; follow the `planning-artifacts` skill (criteria format `- [ ] <ID> (<T|I|A|D>) …`, WU `## Steps`). Only the Verifier ticks steps and criteria. Verified artifacts end with a Completion note.
- Use relative Markdown links for requirement citations (for example, an `RQ §n` link to the source document and section); verify link targets exist.
- Record architecture changes in `Docs/Decisions/ADR-NNNN-<slug>.md` and update the architecture only with that decision.
- Spike reports in `Docs/Spikes/` state the question, method, findings, recommendation and decision/ADR; keep spike code isolated from `src/`.
- Keep generated artefacts and source line-ending policies from architecture §15; committed golden files and generated schema files use LF.
- Follow [the naming plan](../../Docs/Plans/Tailor-naming.plan.md) for product, command, option and package names, and for Australian English spelling.
- Tailoring and sewing metaphors are welcome in prose to keep the tone light, provided they never replace a fact the reader needs and never appear in command names, option names, schema text, diagnostic codes or other machine-readable identifiers. Drop the metaphor whenever the plain word is clearer.
- Document the current repository concisely; label planned work explicitly. Update or remove stale descriptions whenever their subject changes, and link to authoritative rules rather than repeating them.
- Never record chat decisions, follow-up clarifications or session narrative ("as agreed", "changed after review"), including in history, drift or completion notes. Keep concise local rationale in specs or comments; use ADRs for architectural decisions. Ground both in repository facts and requirements, not conversations.
- Keep mid-feature reminders and transient verification in session memory. Before review and feature completion, remove resolved clarification notes and superseded draft descriptions; retain genuine open questions, required Test Evidence and concise factual Completion notes.
- Historical release/migration guidance is only for actual released compatibility obligations, not revisions of an unfinished feature. Do not claim completion before implementation, testing and verification.
