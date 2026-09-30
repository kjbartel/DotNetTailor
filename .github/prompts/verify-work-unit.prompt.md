---
description: "Verify a .NET Tailor work unit against its acceptance criteria and record only proven completion."
agent: Tailor Verifier
---

Verify work unit `${input:wu}`.

1. Read the plan, the selected WU spec, its cited architecture and requirements, and inspect the implementation diff.
2. Check each acceptance criterion against implementation and direct evidence. Reuse inherited Test Evidence only when the exact functional-state fingerprint and environment match; pass every inherited evidence record verbatim to any verifier or probe.
3. Run only relevant uncovered checks. Do not broaden to unrelated suites. Record complete Test Evidence for checks run or reused.
4. Tick a step or AC only when its claim is proven, and run `Test-Traceability.ps1` on the spec. If all criteria and required gates pass and a code-review `Approve` covers the current HEAD, append the `## Completion` note (`work-unit-workflow` skill), set the WU status to `Done`, verify a parent feature/epic that is now complete, and tick milestone criteria only when they are also proven.
5. Do not edit production code. Do not edit files other than the selected WU spec, its parent feature/epic, and master plan status/criteria needed to record verification. Report failures, drift, assumptions and remaining work.
