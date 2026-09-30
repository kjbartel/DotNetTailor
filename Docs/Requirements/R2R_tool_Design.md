# Architecture Specification
## Command‑line Utility for Generating Ready‑to‑Run .NET Desktop Deployments
---
- [Architecture Specification](#architecture-specification)
  - [Command‑line Utility for Generating Ready‑to‑Run .NET Desktop Deployments](#commandline-utility-for-generating-readytorun-net-desktop-deployments)
- [1. Overview](#1-overview)
  - [1.1 Purpose](#11-purpose)
  - [1.2 Key Capabilities](#12-key-capabilities)
  - [1.3 Non‑Negotiable Principles](#13-nonnegotiable-principles)
- [2. High‑Level Architecture](#2-highlevel-architecture)
  - [2.1 Processing Pipeline](#21-processing-pipeline)
  - [2.2 Architectural Rules](#22-architectural-rules)
- [3. Folder Specification Design](#3-folder-specification-design)
  - [3.1 Purpose](#31-purpose)
  - [3.2 Core Concepts](#32-core-concepts)
  - [3.3 Schema Elements](#33-schema-elements)
  - [3.4 Invariants](#34-invariants)
- [4. File Classification Model](#4-file-classification-model)
  - [4.1 Classification Requirement](#41-classification-requirement)
  - [4.2 File Categories](#42-file-categories)
  - [4.3 Rules](#43-rules)
- [5. Reference Resolution Architecture](#5-reference-resolution-architecture)
  - [5.1 Resolution Context](#51-resolution-context)
  - [5.2 Assembly Identity](#52-assembly-identity)
  - [5.3 Resolution Rules](#53-resolution-rules)
- [6. Plugin Architecture](#6-plugin-architecture)
  - [6.1 Plugin Definition](#61-plugin-definition)
  - [6.2 Allowed Dependencies](#62-allowed-dependencies)
  - [6.3 Hard Invariants](#63-hard-invariants)
  - [6.4 Validation Rules](#64-validation-rules)
- [7. Metadata Model](#7-metadata-model)
  - [7.1 Purpose](#71-purpose)
  - [7.2 Top‑Level Structure](#72-toplevel-structure)
  - [7.3 Guarantees](#73-guarantees)
- [8. Ready‑to‑Run Planning Model](#8-readytorun-planning-model)
  - [8.1 Responsibility](#81-responsibility)
  - [8.2 Eligibility States](#82-eligibility-states)
  - [8.3 Compilation Units](#83-compilation-units)
- [9. Patching and Retargeting](#9-patching-and-retargeting)
  - [9.1 Framework Patching](#91-framework-patching)
  - [9.2 TFM Re‑Targeting](#92-tfm-retargeting)
  - [9.3 NuGet Updates](#93-nuget-updates)
- [10. CLI Architecture – Plugin Management](#10-cli-architecture--plugin-management)
  - [10.1 Command Namespace](#101-command-namespace)
  - [10.2 Core Plugin Commands](#102-core-plugin-commands)
  - [10.3 CLI Principles](#103-cli-principles)
- [11. Validation and Acceptance Criteria](#11-validation-and-acceptance-criteria)
  - [11.1 Validation Layers](#111-validation-layers)
  - [11.2 Acceptance Guarantees](#112-acceptance-guarantees)
  - [11.3 Exit Code Semantics](#113-exit-code-semantics)
- [12. Non‑Goals and Future Constraints](#12-nongoals-and-future-constraints)
  - [12.1 Explicitly Out of Scope](#121-explicitly-out-of-scope)
  - [12.2 Forward Compatibility](#122-forward-compatibility)

# 1. Overview
## 1.1 Purpose

This tool transforms an existing binary deployment of a .NET desktop application into a
deployable, Ready‑to‑Run (R2R), win‑x64‑optimised output suitable for enterprise environments.

## 1.2 Key Capabilities

- Binary‑only operation (no source projects required)
- Ready‑to‑Run (crossgen2) generation
- Framework‑dependent ⇄ self‑contained conversion
- Framework patching and TFM re‑targeting
- Plugin‑aware validation and packaging
- Deterministic, auditable outputs

## 1.3 Non‑Negotiable Principles

1. Binary truth — only on‑disk artifacts are authoritative
2. Explicit semantics — no implicit probing or inference
3. Determinism — identical inputs produce equivalent outputs
4. Explainability — all decisions are captured in metadata
5. Separation of concerns — discovery, planning, execution are distinct
6. Enterprise safety — user‑scope only, documented SDK behaviour only

---
# 2. High‑Level Architecture

## 2.1 Processing Pipeline

Input Directory
  ↓
Folder Specification Resolution
  ↓
File Classification & Inventory
  ↓
Reference Resolution & Dependency Validation
  ↓
Metadata Model (Authoritative)
  ↓
R2R / Patch / Retarget Planning
  ↓
Execution (SDK, NuGet, Filesystem)
  ↓
Output Directory + Metadata

## 2.2 Architectural Rules

- Each stage produces explicit intermediate state
- Planning stages are side‑effect free
- Execution stages must strictly follow the plan

---
# 3. Folder Specification Design

## 3.1 Purpose

The folder specification assigns semantic meaning to a physical directory tree to control:
- traversal
- classification
- reference resolution
- plugin boundaries

## 3.2 Core Concepts

- Logical identity (`id`) is semantic and stable
- Physical structure is matched via `mask`
- Semantic reuse is enabled via `id_ref`
- At most one semantic definition may apply per folder

## 3.3 Schema Elements

- version
- root_folder
- id / id_ref
- mask (glob)
- recurse
- file_types
- reference_paths
- duplicate_references
- folders (nested definitions)

## 3.4 Invariants

- No ambiguous folder matches
- No circular id_ref chains
- No absolute or escaping paths
- Deterministic traversal order

---
# 4. File Classification Model

## 4.1 Classification Requirement

Every encountered file must be classified exactly once.

## 4.2 File Categories

- Managed assembly
- Native binary
- Configuration
- Content
- Symbols (PDB)
- XML documentation

## 4.3 Rules

- Classification is inherited from folder semantics
- No file may be silently ignored
- Classification decisions are persisted in metadata

---
# 5. Reference Resolution Architecture

## 5.1 Resolution Context

Each semantic folder produces a resolution context consisting of:
- Ordered reference roots
- Duplicate‑reference handling policy
- No implicit inheritance

## 5.2 Assembly Identity

Assembly identity comprises:
- Name
- PublicKeyToken
- Culture
- Version

Path is provenance, not identity.

## 5.3 Resolution Rules

- References resolved only from declared paths
- No global probing or SDK fallback
- Duplicate handling strictly policy‑driven
- Missing references are structural errors

---
# 6. Plugin Architecture

## 6.1 Plugin Definition

Plugins are identified via folder semantics and treated as first‑class dependency units.

## 6.2 Allowed Dependencies

Plugins may reference:
- Framework assemblies
- Core application assemblies
- Other plugin assemblies (one‑way only)

## 6.3 Hard Invariants

- Plugin binary dependencies form a Directed Acyclic Graph (DAG)
- Mutual or cyclic binary dependencies are forbidden
- Dependency‑injection via interfaces does not affect R2R planning

## 6.4 Validation Rules

- Mutual plugin references → structural error
- Cyclic plugin dependency chains → structural error
- Diagnostics must identify plugins, assemblies, and reference paths

---
# 7. Metadata Model

## 7.1 Purpose

Metadata is the authoritative lifecycle record enabling:
- re‑patching
- TFM re‑targeting
- auditing
- deterministic replay

## 7.2 Top‑Level Structure

- schema_info
- environment
- application_intent
- folder_semantics (resolved)
- files[]
- references[]
- transformations[]
- diagnostics[]

## 7.3 Guarantees

- Every output file has metadata
- Every transformation is explainable
- Metadata alone can justify the output state

---
# 8. Ready‑to‑Run Planning Model

## 8.1 Responsibility

The R2R planner determines:
- assembly eligibility
- reference closures
- compilation units
- skip reasons

Planning produces data only and performs no mutations.

## 8.2 Eligibility States

Each managed assembly is classified as one of:
- Planned
- Skipped (reasoned)
- Ineligible (structural)

## 8.3 Compilation Units

Each compilation unit contains:
- target_assembly
- resolved_references
- reference_roots
- target_tfm
- target_rid (win‑x64)
- options

Plugins may depend on upstream plugins but never require co‑compilation.

---
# 9. Patching and Retargeting

## 9.1 Framework Patching

- Framework assemblies may be replaced with newer patch versions
- Compatibility must be validated
- Original → replacement mapping is recorded in metadata

## 9.2 TFM Re‑Targeting

- runtimeconfig files are updated
- Assembly compatibility is validated
- Failures are explicit unless overridden

## 9.3 NuGet Updates

- Non‑core libraries only
- Explicit opt‑in required
- No implicit dependency upgrades

---
# 10. CLI Architecture – Plugin Management

## 10.1 Command Namespace

tool plugin <command>

## 10.2 Core Plugin Commands

- list
- inspect
- validate
- graph
- r2r
- retarget
- patch
- pack

## 10.3 CLI Principles

- Plugin scope is always explicit
- Validation commands are non‑destructive
- Structural violations always fail
- Exit codes are CI‑safe and deterministic

---
# 11. Validation and Acceptance Criteria

## 11.1 Validation Layers

1. Input validation
2. Structural validation
3. Dependency validation
4. Planning validation
5. Output validation
6. Metadata validation

## 11.2 Acceptance Guarantees

If validation passes:
- Output is deployable
- R2R compilation is correct
- Plugin rules are enforced
- Output is auditable
- Re‑runs are deterministic

## 11.3 Exit Code Semantics

- Success → 0
- Structural or dependency violation → non‑zero
- Warnings → configurable (strict vs permissive)

---
# 12. Non‑Goals and Future Constraints

## 12.1 Explicitly Out of Scope

- Source‑level builds
- Multi‑RID output
- Profile‑Guided Optimisation (PGO)
- CPU‑specific R2R
- Installer generation
- Arbitrary IL rewriting

## 12.2 Forward Compatibility

The architecture permits future addition of:
- PGO
- CPU‑specific optimisation
- Multi‑RID support

without redesign of core components.

---

