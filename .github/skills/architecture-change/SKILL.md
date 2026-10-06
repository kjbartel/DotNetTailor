---
name: architecture-change
description: "Use when assessing or proposing .NET Tailor design changes; covers design impact, ADR approval and architecture synchronisation."
---

# Architecture Change

The [architecture](../../../Docs/Architecture/Tailor.architecture.md) owns design; [requirements](../../../Docs/Requirements/) are authoritative.

- Assess the proposed change against [project ownership and allowed dependencies (§3.1)](../../../Docs/Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies), cross-layer contracts (§3.2), artefact contracts (§5), [safety (§17)](../../../Docs/Architecture/Tailor.architecture.md#17-security) and [determinism (§15)](../../../Docs/Architecture/Tailor.architecture.md#15-determinism). Identify affected boundaries and compatibility guarantees.
- If the change does not alter design, no ADR is required. An inventory-only documentation synchronisation is not a design change.
- For an actual design change, create or use an ADR at `Docs/Decisions/ADR-NNNN-<slug>.md`. For a new ADR, inspect existing ADRs and use the next available number; start at `0001` if none exist.
- Include context, decision, alternatives and consequences, explicitly covering security and determinism. Link affected requirements and architecture sections rather than copying their rules.
- Obtain user approval of the ADR before implementing the design change ([planning-artifacts G3](../planning-artifacts/SKILL.md#phase-gates)). Update the architecture to reflect the accepted ADR in the same WU and PR, or the same WU branch when working without a PR.
- Do not edit `Docs/Requirements/**` during implementation or manually edit generated schemas. Use the [schema-change skill](../schema-change/SKILL.md) when an accepted design affects schema models.

Use [planning-artifacts](../planning-artifacts/SKILL.md) for artefact hierarchy and gates, and [work-unit-workflow](../work-unit-workflow/SKILL.md) for WU status, implementation and verification.
