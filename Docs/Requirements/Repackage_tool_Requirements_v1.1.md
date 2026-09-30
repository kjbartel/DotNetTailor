# Requirements Specification
## Binary‑First .NET Application Analysis and Repackaging Tool

---

## 1. Purpose

The purpose of this tool is to **analyse, validate, and transform existing .NET application deployments** in a safe, deterministic, and auditable manner.

The tool operates on **compiled application folder trees (binary‑first)** and supports transforming them through one or more **repackaging operations**, such as:

- Retargeting to a different Target Framework Moniker (TFM)
- Patching runtimes and libraries
- Changing deployment / packaging model (framework‑dependent vs self‑contained)
- Applying optional optimisations (e.g. Ready‑to‑Run)

The tool is **not limited to performance optimisation** and is explicitly **not an R2R‑only tool**. R2R is an optional optimisation capability.

---

## 2. Scope

### 2.1 In Scope

- Binary‑only analysis of existing application folder trees
- Generation of machine‑readable metadata describing application state
- Explicit validation of metadata against the real application layout
- Deterministic, declarative transformation of application deployments
- Production of new application deployments in separate output locations
- Production of updated metadata representing the new application state
- Optional dry‑run / planning mode
- Operation entirely in **user scope**
- Distribution as a versioned **.NET CLI tool** installable via NuGet (`dotnet tool`)
- Compatibility with CI/CD and interactive usage
- Future extensibility to other platforms (e.g. Linux), without complicating initial implementation

---

### 2.2 Explicitly Out of Scope

- Source‑level builds (`.csproj`, `.sln`)
- MSBuild or SDK extension targets
- In‑place modification of application folders
- Installer creation (MSI, MSIX, etc.)
- Full Native AOT compilation
- Runtime execution or behavioural testing of applications
- Logging or audit history embedded into application state metadata

---

## 3. Core Concepts and Definitions

### 3.1 Application State

An **Application State** consists of:

- A physical application folder tree
- A corresponding **Application Specification** document describing that tree

Every operation consumes an Application State and produces a new Application State.

---

### 3.2 Application Specification

The **Application Specification** is a declarative, versioned document describing the **current state** of an application deployment.

It describes **what exists**, not **what should be done**.

Characteristics:

- Machine‑readable (e.g. JSON)
- Versioned schema
- Deterministic
- Free of logs or operation history
- Produced by:
  - Analysis
  - Repackaging
- Consumed by:
  - Metadata validation
  - Repackaging

The Application Specification is the **authoritative description of an application state once validated**.

---

### 3.3 Transformation Specification

The **Transformation Specification** is a separate declarative document describing **intended operations** to perform on an application state.

It describes **what to change**, not what exists.

Characteristics:

- Explicitly authored (user or CI)
- References entities defined in the Application Specification
- Declarative and reproducible
- Contains zero discovery or heuristics
- May specify multiple operations

---

## 4. Operations Model

### 4.1 Analysis (Required, Non‑Mutating)

**Analysis** is a first‑class operation.

Purpose:
- Analyse an unknown application directory
- Discover structure, dependencies, platforms, TFMs, and semantics
- Produce an initial Application Specification

Characteristics:
- Always non‑mutating
- May be executed independently
- Uses heuristics
- Produces artefacts to inform further decisions

Analysis must never modify the input directory.

---

### 4.2 Metadata Validation (Required Before Repackaging)

**Metadata Validation** is a distinct operation.

Purpose:
- Validate that an Application Specification correctly and consistently describes the real application folder tree

Characteristics:
- Non‑mutating
- May be run independently
- Required before any repackaging operation
- Detects:
  - Structural inconsistencies
  - Invalid references
  - Impossible or contradictory mappings
- Allows users to:
  - Review
  - Edit
  - Correct analysis outputs

A validated Application Specification is treated as authoritative.

---

### 4.3 Repackaging / Transformation (Mutating, Declarative)

Repackaging operations transform one Application State into another.

Characteristics:
- Require:
  - A validated Application Specification
  - A Transformation Specification
- Must perform **at least one** transformation
- May perform **multiple** transformations in a single invocation
- Must produce:
  - A new output directory
  - A new Application Specification describing the resulting state

Repackaging must never be performed in place.

---

## 5. Supported Transformation Categories

All transformation categories are optional and composable.

### 5.1 Retargeting

- Change target TFM
- Update configuration files as required
- Validate managed assembly compatibility

---

### 5.2 Patching

- Patch framework/runtime versions (e.g. 8.0.x → 8.0.y)
- Patch runtime components
- Patch selected non‑core libraries (explicit opt‑in)

---

### 5.3 Deployment / Packaging Model Changes

- Framework‑dependent ⇄ self‑contained
- Removal or inclusion of shared runtimes
- Normalisation of folder layout

---

### 5.4 Optimisation

- Ready‑to‑Run (R2R) compilation
- Future optimisation types (e.g. PGO)

Optimisation must not change functional semantics.

---

## 6. Operation Ordering

A safe and deterministic default ordering must be enforced:

1. Analysis
2. Metadata Validation
3. Retargeting
4. Patching
5. Deployment / Packaging changes
6. Optimisation

Ordering must be explicit and reflected in outputs.

---

## 7. Dry‑Run / Planning Mode

The tool must support a dry‑run mode.

In dry‑run mode:
- No filesystem mutation occurs
- Outputs may include:
  - A projected Application Specification
  - A detailed transformation plan
  - File‑level action summaries

Planning artefacts are **separate** from the Application Specification.

---

## 8. Artefact Management Rules

- Application Specification documents:
  - Represent state only
  - Must not contain logs or execution history
- Planning, diagnostics, and logs must be separate artefacts
- All artefacts must be reproducible from input specifications

---

## 9. Input and Output Directory Rules

- Input and output directories **must be different**
- In‑place modification is forbidden
- Default location for Application Specification output:
  - The application folder (if permitted)
- Alternate output locations must be supported to allow:
  - Read‑only input directories
  - CI environments

---

## 10. CLI and Distribution Requirements

- Implemented as a **C# .NET CLI tool**
- Installable as a **NuGet `dotnet tool`**
- Versioned using Semantic Versioning
- CLI conventions must align with first‑party `dotnet` commands
- Suitable for:
  - interactive use
  - automation
  - CI/CD pipelines

---

## 11. Platform and Runtime Requirements

- Tool runtime:
  - Targets a stable .NET LTS release
- Target runtimes:
  - Independently configurable
  - Not tied to the tool runtime
- Architecture must allow future Linux support with minimal disruption

---

## 12. Non‑Functional Requirements

- Deterministic behaviour
- Clear error reporting
- Auditable transformations
- Safe failure (no partial output corruption)
- Testability of analysis and planning stages
- Forward‑compatible metadata schemas

---

## 13. Summary

This tool is a **binary‑first application analysis and transformation engine** for .NET deployments.

Its design prioritises:
- Safety
- Transparency
- Reproducibility
- Extensibility

By separating **state (Application Specification)** from **intent (Transformation Specification)** and making **analysis and validation first‑class operations**, the tool provides a robust foundation for complex repackaging workflows across enterprise environments.

---
``