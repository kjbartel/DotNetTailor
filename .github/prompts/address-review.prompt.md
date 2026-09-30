---
description: "Resolve .NET Tailor code-review findings from the agent report or GitHub PR comments, then hand back for re-review."
agent: Tailor Implementer
---

Fix mode for work unit `${input:wu}`. The findings are the review report in context, or PR comments pasted below.

Follow the `code-review` skill's resolution rules. Change only what the findings require, re-run the affected checks, and return the resolution table and updated Test Evidence. Do not post PR replies.
