---
name: schema-change
description: "Use when changing AppSpec, TransformSpec or Plan models, JSON serialization, schema generation or schema drift tests."
---

# Schema Change

Follow [architecture §6.1](../../../Docs/Architecture/Tailor.architecture.md#61-common-rules) and the schema-related work-unit spec.

1. Update the owning specification model and serialization in `Tailor.Specifications`; preserve canonical property and collection ordering through Core.
2. Regenerate committed schemas with `dotnet-tailor schema export` using the current CLI workflow. Do not edit generated schemas by hand.
3. Run the schema drift test and focused serialization/schema-validation tests; include golden updates only when intentional.
4. Increment the independent schema version according to the contract: compatible additions use a minor version; breaking changes require a major version and rejection of unknown majors.
5. Update relevant docs and examples, then inspect the generated diff and record Test Evidence.
