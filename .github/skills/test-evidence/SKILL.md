---
name: test-evidence
description: "Select relevant tests, fingerprint functional state, and record or reuse Test Evidence across agents. Use whenever an agent plans, runs, delegates, receives or verifies tests or behaviour probes."
user-invocable: false
---

# Test Evidence

Aim for focused coverage, reuse of evidence for the exact same state, and lossless handoffs. Use the record format in `AGENTS.md` § Test Evidence.

## 1. Impact

- Inspect every functional change in scope: committed, staged, unstaged and untracked.
- List the directly changed projects, tests, fixtures, configuration and package manifests. Follow project references and call sites to indirect consumers. Stop at boundaries that cannot observe the change, and say why.
- Select the tests for directly changed code, for consumers across affected boundaries, and the focused integration tests. Exclude unrelated suites, with a one-line reason. Do not default to the full solution.

## 2. Fingerprint

Run from the repository root before testing:

```powershell
.github/skills/test-evidence/scripts/Get-FunctionalState.ps1
```

The script hashes tracked and untracked non-ignored files, excluding Markdown and docs/specs paths. Pass `-IncludeDocumentation` when docs affect behaviour. Use `-Exclude <glob>` only for files proven non-functional, and record why. The state is the fingerprint, the file count, docs flag and exclusions, plus the environment: OS, .NET SDK from `global.json`, and any behaviour-affecting variables such as `DOTNET_REPACK_*`. If the tests can modify their inputs, recompute the fingerprint afterwards.

## 3. Reuse before running

Reuse a record when its command covers the check, the state and environment match exactly, and the result is unambiguous with no invalidating caveat. Re-run only when evidence is missing, the state or environment differs, the coverage is insufficient, a failure needs diagnosis, or an AC requires an independent run. Always state the rerun reason. Edits limited to docs do not invalidate evidence.

## 4. Run the minimum

Use `dotnet test --project <project> --filter-trait "WU=<id>"` or a narrower filter. Escalate only when a focused failure points wider, a shared contract changed, or the repo gates require it. Keep failures as evidence.

## 5. Handoff

Return one record per distinct command. When you delegate, pass the records you received verbatim. A summary can accompany a record but never replaces it.
