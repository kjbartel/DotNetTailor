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

The script hashes tracked and untracked non-ignored files, excluding Markdown and docs/specs paths. Pass `-IncludeDocumentation` when docs affect behaviour. Use `-Exclude <glob>` only for files proven non-functional, and record why. The state is the fingerprint, the file count, docs flag and exclusions, plus the environment: OS, .NET SDK from `global.json`, and any behaviour-affecting variables such as `DOTNET_TAILOR_*`. If the tests can modify their inputs, recompute the fingerprint afterwards.

## 3. Reuse before running

Reuse a record when its command covers the check, the state and environment match exactly, and the result is unambiguous with no invalidating caveat. Re-run only when evidence is missing, the state or environment differs, the coverage is insufficient, a failure needs diagnosis, or an AC requires an independent run. Always state the rerun reason. Edits limited to docs do not invalidate evidence.

### YAML Validation

Use [PowerShellDependencies.psd1](PowerShellDependencies.psd1)'s pinned `powershell-yaml` from PSGallery for YAML checks, not Python environments or PyYAML. Setup requires PowerShell with PowerShellGet's `Save-Module` and access to PSGallery. From the repository root, explicitly download once per checkout:

```powershell
pwsh -NoProfile -File .github/skills/test-evidence/scripts/Initialize-PowerShellDependencies.ps1
```

The module is cached in ignored `artifacts/local-tools/powershell-modules`; reuse it across agents and do not commit downloaded contents. Setup does not change user/global module paths. Validation is offline, imports only the pinned cached module, and fails with the setup command when it is absent or invalid; it never installs or falls back to Python.

```powershell
pwsh -NoProfile -File .github/skills/test-evidence/scripts/Test-YamlFrontmatter.ps1
```

The validator calls `ConvertFrom-Yaml` on frontmatter in all repo instructions, prompts, skills and agents, requiring a mapping and non-empty `description`, matching skill `name`, and instruction `applyTo`. Use `-Path <file-or-directory>` for a focused check; `-ModuleCache <directory>` selects an existing cache for an isolated dependency check. It does not validate every WU contract, exact instruction glob or Markdown link. For other YAML files, explicitly import the manifest at the pinned cache path and call `ConvertFrom-Yaml`; frontmatter extraction is only for customization Markdown. Record the module version and cache source in Test Evidence. Historical parser evidence retains its original provenance and is not reusable for these new helpers.

## 4. Run the minimum

Use `dotnet test --project <project> --filter-trait "WU=<id>"` or a narrower filter. Escalate only when a focused failure points wider, a shared contract changed, or the repo gates require it. Keep failures as evidence.

## 5. Handoff

Return one record per distinct command. When you delegate, pass the records you received verbatim. A summary can accompany a record but never replaces it.
