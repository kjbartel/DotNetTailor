---
applyTo: "**/*.cs"
description: "C# implementation conventions for .NET Tailor; see the architecture for ownership and dependency rules."
---

# C# Conventions

See [the architecture](../../Docs/Architecture/Tailor.architecture.md), especially [project boundaries](../../Docs/Architecture/Tailor.architecture.md#31-project-responsibilities-and-allowed-dependencies) and [determinism](../../Docs/Architecture/Tailor.architecture.md#15-determinism).

- Use file-scoped namespaces, nullable reference types, and the repository's existing naming and formatting conventions.
- Do not add `#pragma` suppressions without a specific documented justification. Fix analyzer warnings at their source where practical.
- Use `RelativePath` for app-relative paths and `Diagnostic`/`Result<T>` for reported failures; do not duplicate their policies.
- Sort deterministic collections with the established ordinal-ignore-case policy; do not rely on filesystem or dictionary enumeration order.
- Serialize canonical JSON only through the Core writer. Keep canonical output free of timestamps, GUIDs and machine paths, with LF line endings.
- Start child processes with `ProcessStartInfo.ArgumentList`, never a shell or concatenated command string.
- Prefer async APIs for I/O and pass `CancellationToken` through asynchronous call chains.
- Keep types in their owning project and obey the dependency and "Must not" rules in architecture §3.1.
- Source files use CRLF in the working tree; generated tool artefacts use canonical LF, as described in architecture §15.

## Comments

Follow [the naming plan](../../Docs/Plans/Tailor-naming.plan.md) for spelling and voice.

- Write a comment only for what the code cannot show: a non-obvious constraint, an external quirk, or a reason a simpler approach fails. Never restate the next line.
- Never narrate a chat session or draft revision. No "changed to…", "as discussed", "previously we…", "TODO from review", dates, agent names or work-unit numbers in code. Keep local rationale in concise comments or specs; architectural decisions require an ADR.
- Keep comments short, usually one line, and true of the code as it stands now, not of how it got there.
- Update or remove comments and XML docs with the implementation they describe. Keep temporary clarifications and mid-feature reminders in session memory; remove them before review and feature completion.
- Write comments that survive refactoring: describe intent and invariants, not line counts, call order, type names or file paths that a rename would falsify.
- Do not restate a symbol name in prose where a code reference works. Use `<see cref="..."/>` and `<paramref name="..."/>` in XML docs and `<see cref="..."/>` in comments so rename and find-references keep them correct.
- XML docs are for public and protected API. One summary sentence is usually enough; add `<param>`, `<returns>` or `<exception>` only where the behaviour is not evident from the signature.
- Delete a comment rather than update it when the code it described is gone.

## Voice

Tailoring and sewing metaphors are encouraged in progress messages, log text and comments where they stay unambiguous, in the spirit of Copilot's own baking messages. Clarity wins every time:

- Never let the metaphor replace the fact a reader needs. "Taking in the output tree: dropped 412 unused files" is fine; "Taking in the output tree" alone is not.
- Never use a metaphor in a machine-readable field: diagnostic codes and messages, canonical JSON, schema text, exception messages, exit-code semantics and option names stay plain and literal.
- Never coin a metaphor that could be read as a real operation. Nothing is "cut" unless files are deleted.
- Use it sparingly, and drop it whenever the plain word is clearer.
