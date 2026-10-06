---
applyTo: "tests/**"
description: "Test conventions for xUnit v3, Microsoft Testing Platform, fixtures and golden files."
---

# Test Conventions

See [the architecture](../../Docs/Architecture/Tailor.architecture.md#16-testing-strategy) and [the plan's test conventions](../../Docs/Plans/Tailor.plan.md#test-conventions).

- Use xUnit v3 on Microsoft Testing Platform. Every test has `[Trait("WU", "<id>")]` and applicable `Category` traits.
- Tests proving a `(T)` criterion add `[Trait("AC", "<artifact>/<criterion>")]` (e.g. `WU-301/AC-2`, `FT-012/FC-1`); repeat the attribute for several criteria.
- Name test classes `<TypeUnderTest>Tests` and methods `<Subject><Condition><ExpectedResult>` in PascalCase, with no underscores. Never suppress CA1707.
- Assert current observable contracts, including required rejection and safety invariants. Never test for deleted symbols/code/text being absent or old behaviour no longer occurring; name scenarios by the required behaviour, not the change history. Regression tests reproduce a defect and assert the current contract, not a retired implementation.
- Mid-feature probes and checks for superseded drafts are temporary. Before review and feature completion, remove them or convert useful cases into contract-focused tests; keep transient verification and reminders in session memory. Preserve genuine released compatibility coverage and required Test Evidence.
- Use `Golden.AssertMatches` from `tests/Tailor.Testing`; store committed golden files under `Golden/<TestClass>/` with LF endings.
- Scrub only intentionally variable values such as temporary paths or repository roots. Mismatch `.received.*` files are ignored and must not be committed.
- Set `DOTNET_TAILOR_UPDATE_GOLDEN=1` only for intentional local golden updates; never set it in CI.
- Use published test apps under `artifacts/testapps/` through their manifest when a test needs the matrix. Matrix tests explain local skips and do not silently skip in CI.
- Unit tests do not access the network. Network tests require the explicit opt-in documented by the plan.
- Run one WU with `dotnet test --project <project> --filter-trait "WU=<id>"`; use MTP mode without a `--` separator.
- Record Test Evidence with state fingerprint, environment, impact, selected and excluded checks, exact command and result.
