---
description: "Read-only code review of a .NET Tailor WU branch or PR with an Approve/Request changes verdict."
agent: Tailor Code Reviewer
---

Review work unit `${input:wu}`. For a re-review, the prior findings and the resolution table are in context.

Diff from the merge base with `main`, including uncommitted changes. Apply the `code-review` skill and return its report format.
