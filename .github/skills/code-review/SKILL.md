---
name: code-review
description: "Use when reviewing .NET Tailor changes or resolving review feedback (agent report or GitHub PR comments); defines checklist, finding format, verdict and resolution table."
---

# Code Review

## Checklist (diff and directly affected code only)

1. **Scope**: the change matches the WU spec. Nothing is outside scope, and no AC is left unaddressed.
2. **Correctness**: logic, edge cases, error paths through `Diagnostic`/`Result<T>`, and cancellation.
3. **Boundaries**: project references and responsibilities follow architecture §3.1. Packages are added only through CPM.
4. **Safety**: input trees are never mutated, paths are confined, reparse points are rejected, processes use no shell (`ArgumentList`), and no secrets appear in logs, arguments or artefacts.
5. **Determinism**: canonical JSON goes only through the Core writer, ordering is ordinal-ignore-case, output has LF, and there are no timestamps, GUIDs or machine paths.
6. **Tests**: WU/AC traits and naming rules hold; ACs and failure paths are covered; golden updates are intentional; no network calls by default. Tests assert current contracts, not removal of code or superseded behaviour; temporary mid-feature checks are removed or converted to durable coverage.
7. **Docs/comments**: concise, current and free of chat narrative or resolved clarification notes; planned work is labelled, stale text removed, user-facing changes documented, and architecture changes have an ADR. Required evidence and released compatibility guidance are retained.
8. **Evidence**: Test Evidence is present and matches the reviewed state.

## Severity

`Blocker` means a safety, correctness or data-loss issue, or an AC that is not met. `Major` means a boundary violation, missing tests or nondeterminism. `Minor` means maintainability. `Nit` is optional. Only Blocker and Major findings block approval.

## Report

```markdown
## Code Review — WU-<id> @ <short-sha>
**Verdict**: Approve | Request changes
| ID | Sev | File:line | Finding | Required change |
|---|---|---|---|---|
| CR-1 | Major | src/…:42 | … | … |
**Checks**: <reused/run evidence summary; full records below if run>
```

Keep IDs stable across re-reviews, and add new ones as `CR-n+1`. For PR comments, use `PR-<comment-id>` or `PR-n` in the order they were received.

## Resolving feedback (implementer)

```markdown
| ID | Resolution | Change / reason |
|---|---|---|
| CR-1 | Fixed | <file:line, one line> |
| CR-2 | Disagree | <evidence-based reason> |
| CR-3 | Deferred | <follow-up WU or issue> |
```

- User feedback takes precedence over agent findings. If they conflict, ask the user.
- A `Disagree` or `Deferred` response needs reviewer acceptance, or a user decision for a Blocker or Major finding.
- Draft PR replies in the resolution table. Post them only with user approval.
