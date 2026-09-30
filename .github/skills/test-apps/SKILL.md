---
name: test-apps
description: "Use when building the published test-app matrix, adding a sample app, or consuming matrix fixtures and their manifest."
---

# Test Apps

The test-app suite is introduced by [WU-003](../../../Docs/Specs/M0/WU-003-test-app-suite.spec.md); update this guidance when that WU defines the script's final parameters. The planned matrix and acceptance criteria are in [the plan](../../../Docs/Plans/Tailor.plan.md#m0-foundation--repo-bootstrap) and [architecture §16](../../../Docs/Architecture/Tailor.architecture.md#16-testing-strategy).

- Build the matrix with `build/Build-TestApps.ps1`; published outputs and `manifest.json` belong under ignored `artifacts/testapps/`.
- Consume fixtures through the manifest, including its `expectedInvalid` diagnostics. Do not infer validity from folder names.
- The planned matrix is `{net8.0, net10.0} × {FD, SC} × {R2R off, on}` and includes console, WinForms, WPF and plugin scenarios.
- Keep sample app sources under `tests/TestApps/`; update the matrix manifest and CI cache key when source inputs change.
- Do not add test-app project references to production projects or commit published binaries.
