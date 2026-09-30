# .NET Tailor Agent Instructions

Use the repository guide in [`AGENTS.md`](../AGENTS.md) for workflow and detailed conventions. The architecture and plan are the sources of truth; do not duplicate them here.

1. Read the relevant WU spec, cited architecture and requirements, and confirm dependencies are `Done`.
2. Work only within the selected WU; use branch `wu/<id>-<slug>`.
3. Preserve project boundaries in architecture §3.1; do not add unapproved dependencies.
4. Never mutate an input application tree; confine paths and reject unsafe reparse points.
5. Never launch processes through a shell; use `ArgumentList`.
6. Keep credentials out of specs, CLI arguments, logs and artefacts.
7. Use Core's canonical JSON writer; generated artefacts are deterministic and LF.
8. Follow the WU test traits, MTP commands, naming and golden-file rules.
9. Run formatting and focused checks, then record reusable Test Evidence.
10. Do not edit requirements or generated schemas; only the Verifier ticks acceptance criteria and completes plan status.
