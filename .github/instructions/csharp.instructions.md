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
