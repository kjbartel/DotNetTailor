# Transformation Specification Structure
## Binary-First .NET Application Analysis and Repackaging Tool

---

## 1. Purpose

The **Transformation Specification** is a declarative, machine-readable description of the transformations to apply to an existing .NET application state.

It describes:

- Which transformations are requested
- Which parts of the application each transformation applies to
- Desired target framework, runtime, platform, and deployment characteristics
- File inclusion, exclusion, relocation, and packaging policies
- Library and runtime patching policies
- Optimisation policies
- Symbol, documentation, resource, and content handling
- Transformation-specific validation and failure policies

The Transformation Specification describes **what should change**, not **what currently exists**.

Current application state belongs in the separate **Application Specification**.

The Transformation Specification must not contain:

- Discovered application state
- Expanded file inventories
- Transformation execution history
- Execution logs
- Transformation results

These are represented by other artefacts.

---

## 2. Lifecycle

### 2.1 Creation

A Transformation Specification is normally:

- Authored by a user
- Generated or templated by automation
- Maintained in source control
- Reused where appropriate against compatible application states

It is not normally generated as part of application analysis.

### 2.2 Validation

Before planning or execution, the Transformation Specification must be validated against:

- Its own schema and semantic rules
- The input Application Specification
- The physical application tree where required
- Available SDKs, runtimes, packages, or other required tooling where relevant

### 2.3 Planning

A validated Transformation Specification and validated Application Specification are combined to produce a **Transformation Plan**.

The plan expands declarative rules into concrete actions.

### 2.4 Execution

Execution consumes the Transformation Plan and produces:

- A new application directory tree
- A new Application Specification describing that output state
- Optional separate reports and logs

Execution must never modify the input application directory.

---

## 3. Design Principles

### 3.1 Intent, not state

The Transformation Specification describes desired changes.

It must not duplicate information already represented by the Application Specification unless that information represents a required target state or an explicit assertion.

### 3.2 Rule-based selection

Transformations should target semantic groups and patterns rather than individual files wherever possible.

Selectors may reference:

- Folder semantics
- Folder definition IDs
- File classification groups
- File association types
- Managed assembly roles
- Plugin semantics
- Framework roles
- Platforms and RIDs
- Languages and cultures
- Wildcard filename/path patterns
- Explicit exceptions

### 3.3 Application Specification references

Rules should preferentially reference semantic identities defined by the Application Specification rather than repeat physical layout information.

For example:

- All managed assemblies in `plugins`
- Runtime binaries for `win-x64`
- Documentation associated with application assemblies
- Resource groups for cultures other than `en-*`

### 3.4 Composability

A single Transformation Specification may request multiple independent transformations.

For example:

1. Retarget from .NET 8 to .NET 10
2. Convert from framework-dependent to self-contained
3. Update selected NuGet libraries
4. Remove non-English resources
5. Separate PDB files into a symbols package
6. Compile eligible application assemblies as ReadyToRun

### 3.5 Explicitness

Transformations which materially alter application behaviour or dependency versions must be explicitly requested.

The tool must not silently infer that an upgrade, retarget, or optimisation should occur.

### 3.6 Determinism

The same:

- Input application state
- Application Specification
- Transformation Specification
- Tooling environment

should produce an equivalent Transformation Plan and output application state.

---

## 4. Document Organisation

A Transformation Specification may consist of:

- A single document; or
- A root document referencing subsidiary transformation-policy documents.

A multi-document representation may be useful for separating:

- Organisation-wide defaults
- Application-specific transformations
- Packaging policies
- Optimisation policies
- Library update policies

The effective specification must be deterministic after all referenced documents are resolved.

---

## 5. Specification Identity and Schema

### 5.1 Purpose

Identifies the transformation specification format and controls schema compatibility.

### 5.2 Information

The specification should identify:

- Specification type
- Schema version
- Optional logical identifier
- Optional description

### 5.3 Requirements

- Schema version must be explicit.
- Unsupported schema versions must be rejected.
- Schema evolution must be supported.
- Transformation schema version must be independent of Application Specification schema version.

---

## 6. Input Application Requirements

### 6.1 Purpose

Defines constraints that the input Application Specification must satisfy before the transformation may be applied.

This enables a Transformation Specification to fail safely when applied to an unexpected application state.

### 6.2 Assertions

Input assertions may include:

- Expected application identity
- Expected application version or version range
- Expected TFM or TFM range
- Expected deployment model
- Expected runtime/framework
- Expected runtime version or version range
- Expected platforms or architectures
- Expected folder semantic definitions
- Expected plugin structure
- Expected components or assemblies

### 6.3 Optional assertions

Assertions should only be required where they protect transformation correctness.

The specification should not unnecessarily reproduce the entire Application Specification.

### 6.4 Failure behaviour

A failed input assertion must prevent execution unless an explicit and safe override mechanism exists.

---

## 7. Transformation Operations

### 7.1 Purpose

Defines the transformations requested for the application.

At least one transformation operation must be specified for a repackaging operation.

### 7.2 Initial transformation categories

The initial model must support:

- Retargeting
- Patching
- Deployment-model changes
- Optimisation

Additional transformation categories must be extensible without redesigning the overall specification.

### 7.3 Multiple transformations

Multiple transformation categories may be requested in a single specification.

The Transformation Planner is responsible for deriving a valid execution order.

The specification describes desired intent rather than requiring the user to manually sequence every low-level action.

---

## 8. Selectors

### 8.1 Purpose

Selectors identify which portions of the input application state a rule applies to.

Selectors are the primary mechanism for avoiding explicit file lists.

### 8.2 Semantic selectors

Selectors may reference semantic information from the Application Specification, including:

- Folder IDs
- Folder roles
- File classification groups
- File association types
- Assembly roles
- Plugin IDs or plugin patterns
- Framework/runtime roles
- Platforms
- Architectures
- RIDs
- Cultures
- TFMs

### 8.3 Physical selectors

Where semantic information is insufficient, selectors may additionally use:

- Relative path patterns
- Folder masks
- Filename masks
- Extensions

Standard wildcard/glob semantics should be supported.

### 8.4 Logical composition

Selectors should support logical composition where required, including:

- All conditions
- Any condition
- Negation

This permits expressions conceptually equivalent to:

> All managed assemblies under plugin folders except assemblies matching `Legacy.*`.

### 8.5 Default scope

A transformation rule may apply to the entire application state if no narrower selector is specified.

This must be explicit in schema semantics.

---

## 9. Include and Exclude Rules

### 9.1 Purpose

Controls which existing application artefacts are represented in the resulting application state.

### 9.2 Rule-based filtering

Rules should preferentially target:

- Classification groups
- Associated-file groups
- Folder semantics
- Languages
- Platforms
- Wildcard patterns

rather than enumerating files.

### 9.3 Include behaviour

Include rules may explicitly preserve artefacts that would otherwise be excluded by broader policy.

### 9.4 Exclude behaviour

Exclude rules may remove:

- Files
- File groups
- Associated files
- Folders
- Resource cultures
- Inapplicable platform assets

### 9.5 Catch-all behaviour

The default must preserve artefacts unless a transformation requires their removal.

Unrecognised content must not be silently discarded.

### 9.6 Precedence

Include/exclude precedence must be deterministic and documented.

Explicit exceptions should be capable of overriding broader wildcard rules where appropriate.

---

## 10. Associated-File Policies

### 10.1 Purpose

Controls transformations of files associated with primary application artefacts.

### 10.2 Association selectors

Rules may target associations such as:

- Assembly configuration
- Runtime configuration
- Dependency manifests
- Debug symbols
- XML documentation
- Resources

### 10.3 Group semantics

Operations may apply to:

- The primary artefact only
- Selected associated artefacts
- The complete associated group

### 10.4 Examples

The model must support policies conceptually equivalent to:

- Copy every application assembly and its configuration.
- Exclude XML documentation associated with framework assemblies.
- Move all PDB files associated with included binaries to the symbols output.
- Remove a component and all files exclusively associated with that component.

---

## 11. Retargeting Specification

### 11.1 Purpose

Defines a requested change to the target framework or framework context.

### 11.2 Target state

May specify:

- Target TFM
- Target framework versions
- Target desktop/runtime framework
- Relevant compatibility policy

### 11.3 Scope

Retargeting may apply to:

- The entire application
- Particular application components
- Selected plugins
- Selected assembly groups

### 11.4 Required transformations

The planner may derive actions including:

- Runtime configuration modification
- Dependency manifest modification
- Framework assembly replacement
- Addition or removal of framework components
- Assembly compatibility validation

These derived actions belong in the Transformation Plan, not the Transformation Specification.

### 11.5 Compatibility

An incompatible retarget request must fail planning unless explicitly supported by a defined compatibility policy.

---

## 12. Patching Specification

### 12.1 Purpose

Defines requested version changes that do not fundamentally change the logical application component.

### 12.2 Framework/runtime patching

May specify:

- Target framework/runtime patch version
- Latest applicable patch
- Explicit version
- Version range or policy

For example, a self-contained .NET 8 application may be transformed from one .NET 8 runtime patch to a later .NET 8 patch.

### 12.3 Library patching

May select:

- Individual packages/components
- Assembly groups
- Non-core Microsoft libraries
- Third-party libraries

### 12.4 Package sources

Where packages are acquired externally, the specification may identify or reference:

- Allowed NuGet sources
- Version policies
- Package selection rules

Credentials must not be embedded in the Transformation Specification.

### 12.5 Upgrade safety

Non-core package updates must be explicit.

The specification must be capable of limiting upgrades to:

- Patch versions
- Minor versions
- Defined ranges
- Exact versions

No unrequested library upgrade may occur.

---

## 13. Deployment Model Transformation

### 13.1 Purpose

Specifies the required output deployment model.

### 13.2 Supported transformations

Must support:

- Framework-dependent to framework-dependent
- Framework-dependent to self-contained
- Self-contained to framework-dependent
- Self-contained to self-contained

A same-model transformation remains useful when combined with patching, retargeting, filtering, or optimisation.

### 13.3 Target platform

Deployment transformation may specify:

- Target RID
- Target architecture
- Target operating system characteristics

Initial implementation may primarily support `win-x64`.

### 13.4 Derived actions

The planner may derive actions such as:

- Add runtime components
- Remove bundled runtime components
- Replace platform assets
- Update configuration

These actions belong to the Transformation Plan.

---

## 14. Optimisation Specification

### 14.1 Purpose

Defines optional application optimisations.

Optimisation is not inherently required by repackaging.

### 14.2 Optimisation types

Initial support includes:

- ReadyToRun (R2R)

The model must permit future optimisation types such as:

- Profile-Guided Optimisation
- Processor-specific optimisation
- Other deployment-time optimisations

### 14.3 ReadyToRun scope

R2R rules may target:

- All eligible managed assemblies
- Application assemblies
- Framework assemblies
- Plugins
- Selected semantic groups
- Explicit include/exclude patterns

### 14.4 Eligibility

The Transformation Specification expresses desired optimisation.

Actual eligibility is determined from the validated Application Specification and tooling environment during planning.

### 14.5 Optimisation failure

The specification may define policy for cases where selected artefacts cannot be optimised, for example:

- Fail
- Warn and preserve unoptimised artefact
- Skip selected artefact

---

## 15. Resource and Localisation Policy

### 15.1 Purpose

Defines which localised resources should be retained in the output state.

### 15.2 Selection

Rules may select cultures using patterns.

Examples include:

- `en`
- `en-*`
- Explicit additional cultures

### 15.3 Default behaviour

Resources should be preserved unless an explicit transformation policy removes them.

### 15.4 Associated resources

Resource filtering must respect associations with their owning application components.

---

## 16. Debug Symbol Policy

### 16.1 Purpose

Controls handling of debugging symbols.

### 16.2 Supported policies

Must support:

- Preserve PDB files in the primary output
- Exclude PDB files
- Emit PDB files into a separate symbols output

### 16.3 Scope

Policies may apply globally or to selected semantic groups.

For example, application symbols may be retained while framework symbols are omitted.

### 16.4 Association behaviour

Symbol selection should normally operate through the associated-file relationships defined by the Application Specification.

---

## 17. Documentation Policy

### 17.1 Purpose

Controls handling of assembly documentation and similar associated documentation files.

### 17.2 Supported policies

Must support:

- Preserve documentation
- Exclude documentation

Rules may operate:

- Globally
- By assembly role
- By folder semantic
- By file pattern

---

## 18. File and Folder Layout Rules

### 18.1 Purpose

Controls how retained or generated artefacts are mapped into the output application tree.

### 18.2 Default behaviour

Unless otherwise required by a transformation, an artefact should retain its logical relative location.

### 18.3 Mapping rules

Where required, rules may define:

- Output folder
- Relative path transformation
- Folder flattening or restructuring
- Placement of newly acquired runtime or package artefacts

### 18.4 Semantic references

Mappings should prefer folder semantics from the Application Specification rather than hard-coded physical paths.

### 18.5 Collision handling

Any transformation producing two different artefacts at the same output path must be detected during planning.

Collision resolution must never be implicit.

---

## 19. Addition Rules

### 19.1 Purpose

Describes explicitly requested content which does not already exist in the input state.

### 19.2 Sources

New artefacts may originate from:

- Runtime packs
- NuGet packages
- User-provided files
- Generated configuration
- Transformation tooling

### 19.3 Destination

Added content must have a deterministic output location.

### 19.4 Provenance

The Transformation Plan and associated reports must record the source of added artefacts.

The resulting Application Specification describes the added files as part of the new state but does not retain operational history.

---

## 20. Removal Rules

### 20.1 Purpose

Explicitly defines application content which should not exist in the resulting state.

### 20.2 Semantic removal

Removal should preferentially target semantic concepts such as:

- Unsupported RID assets
- Unwanted locales
- Symbols
- Documentation
- Replaced framework components

### 20.3 Dependency safety

Removal must not proceed if a retained component has a required dependency on the artefact unless another transformation supplies a valid replacement.

Such conflicts must be detected during planning.

---

## 21. Transformation Defaults

### 21.1 Purpose

Allows common behaviour to be defined once rather than repeated on every transformation rule.

### 21.2 Potential defaults

May include:

- Default include behaviour
- Default symbol handling
- Default documentation handling
- Default language retention
- Default optimisation failure policy
- Default duplicate handling

### 21.3 Override behaviour

More specific rules may override defaults.

Precedence must be deterministic.

### 21.4 Safe defaults

Defaults should favour preservation over removal or upgrade.

Potentially destructive or version-changing transformations must require explicit intent.

---

## 22. Rule Precedence and Conflict Resolution

### 22.1 Purpose

Ensures that multiple rule-based transformations produce deterministic intent.

### 22.2 Specificity

Where appropriate, more specific selectors may override broader selectors.

For example:

1. Global default
2. Classification-group rule
3. Folder-semantic rule
4. Path/file rule
5. Explicit exception

### 22.3 Conflicting operations

Two incompatible rules applying at the same effective precedence must be a validation error.

### 22.4 Examples of conflicts

Examples include:

- Include and exclude the same artefact with equal precedence
- Request two different target versions for the same component
- Request both preservation and removal of the same associated group
- Produce multiple outputs at the same destination path

### 22.5 No order-dependent ambiguity

Document ordering must not accidentally resolve semantic conflicts unless ordering is explicitly part of the relevant rule type.

---

## 23. Transformation Dependencies and Ordering

### 23.1 Purpose

Multiple requested transformations may have dependencies on one another.

### 23.2 Declarative operations

The user should normally specify desired end state rather than low-level execution order.

### 23.3 Planner responsibility

The Transformation Planner determines a valid execution sequence.

Conceptually, this may include:

1. Validate input state
2. Resolve target framework and platform state
3. Acquire required runtime/package artefacts
4. Apply retargeting
5. Apply patching/replacement
6. Change deployment model
7. Apply filtering and layout rules
8. Apply optimisations
9. Generate/update configuration
10. Validate projected/output state

The exact physical implementation order may differ where required.

### 23.4 Impossible combinations

If requested transformations cannot be combined safely, planning must fail.

The tool must not silently discard or reorder intent in a way that changes the requested target state.

---

## 24. Validation and Failure Policies

### 24.1 Purpose

Controls how non-fatal transformation conditions are handled.

### 24.2 Error classes

Policies may distinguish conditions such as:

- Requested selector matches nothing
- Optional associated file missing
- R2R optimisation failure
- Package update unavailable
- Version compatibility uncertain
- Unknown content encountered
- Validation warning

### 24.3 Behaviour

Where appropriate, configurable behaviours may include:

- Error
- Warning
- Skip
- Preserve existing state

### 24.4 Structural errors

Some conditions must remain unconditional errors, including:

- Invalid specification
- Ambiguous transformation intent
- Invalid output path collisions
- Incompatible mandatory dependencies
- Input/output directory identity
- Unsafe path traversal

---

## 25. Output State Requirements

### 25.1 Purpose

Defines assertions that the resulting application state must satisfy.

### 25.2 Assertions

May include:

- Required TFM
- Required deployment model
- Required RID/platform
- Required runtime version
- Absence of specified resources
- Required optimisation state for selected assemblies
- Required symbols policy

### 25.3 Planning

These assertions provide acceptance conditions against which the projected state can be validated during dry-run planning.

### 25.4 Execution

After execution, equivalent assertions must be checked against the actual output state before the transformation is considered successful.

---

## 26. Dry-Run and Planning Behaviour

### 26.1 Purpose

The Transformation Specification must be fully usable without executing the transformation.

### 26.2 Inputs

Dry-run planning uses:

- Validated Application Specification
- Transformation Specification
- Available tooling/package information where necessary

### 26.3 Outputs

Dry-run may produce:

- Projected Application Specification
- Expanded Transformation Plan
- Output file manifest
- Detailed per-file action report
- Dependency changes
- Package/runtime acquisition report
- Validation diagnostics

### 26.4 Per-file action report

The expanded plan may describe every affected physical file, including actions such as:

- Copy
- Add
- Remove
- Replace
- Move
- Modify configuration
- Retarget
- Optimise
- Extract to symbols output
- Preserve unchanged

This expanded representation belongs to the plan/report, not the Transformation Specification.

---

## 27. Multi-Document Transformation Specifications

### 27.1 Purpose

Complex transformations may be decomposed into reusable documents.

### 27.2 Possible decomposition

For example:

- Root transformation specification
- Organisation defaults
- Platform policy
- Filtering policy
- Dependency patch policy
- Optimisation policy

### 27.3 Composition

The root specification determines which subsidiary documents are included.

### 27.4 Precedence

Precedence between included documents must be explicit and deterministic.

### 27.5 Circular references

Circular specification inclusion is invalid.

---

## 28. Variables and Parameters

### 28.1 Purpose

Reusable Transformation Specifications may require limited parameterisation.

### 28.2 Potential parameters

Examples include:

- Target TFM
- Target runtime version
- Target RID
- Application version
- Package version policy

### 28.3 Constraints

Parameters must remain declarative.

They must not introduce arbitrary scripting or executable logic into the specification format.

### 28.4 Resolution

All variables must be fully resolved before transformation planning begins.

Unresolved values are validation errors.

---

## 29. External Sources and Credentials

### 29.1 External sources

Transformations may require external artefacts such as NuGet packages or runtime components.

The specification may reference:

- Named package sources
- Package source URLs
- Source policies

### 29.2 Credentials

Secrets and credentials must not be stored directly in the Transformation Specification.

Authentication must be obtained from external user/environment configuration.

### 29.3 Reproducibility

Where an external artefact influences transformation output, its resolved identity and version must be represented in the Transformation Plan or dependency report.

---

## 30. Relationship to Application Specification

### 30.1 Application Specification

Describes:

> **What exists?**

### 30.2 Transformation Specification

Describes:

> **What should change?**

### 30.3 Transformation Plan

Combines both to answer:

> **Exactly what actions are required to reach the requested state?**

### 30.4 Output Application Specification

Describes:

> **What exists after those actions are applied?**

The Transformation Specification remains separate from both the input and output Application Specifications.

---

## 31. Relationship to Transformation Plans and Logs

The Transformation Specification must not contain expanded execution details.

Those belong to separate artefacts.

### Transformation Plan

May contain:

- Fully resolved source files
- Destination paths
- Concrete tool invocations
- Runtime/package acquisitions
- Configuration modifications
- Optimisation actions
- Expected hashes or identities
- Validation steps

### Execution Report

May contain:

- Actual actions performed
- Action results
- Warnings
- Failures
- Timings
- Tool outputs

### Logs

Contain diagnostic execution information.

None of these form part of application state or transformation intent.

---

## 32. Global Invariants

A valid Transformation Specification must satisfy the following principles:

1. **Intent only**  
   It describes desired transformation, not discovered application state.

2. **At least one operation**  
   A repackaging specification must request at least one material transformation.

3. **Rule based**  
   Semantic selectors and patterns should be preferred over exhaustive file lists.

4. **Application-aware**  
   Rules should reference semantic information from the Application Specification wherever practical.

5. **Declarative**  
   The specification describes desired outcomes and policies rather than implementing procedural scripts.

6. **Composable**  
   Retargeting, patching, deployment changes, filtering and optimisation may be combined.

7. **Deterministic**  
   Equivalent inputs and environment must produce an equivalent transformation plan.

8. **Explicit upgrades**  
   No framework, runtime, or non-core library upgrade may occur without transformation intent authorising it.

9. **Preservation by default**  
   Unknown or unmatched content is preserved unless explicitly excluded or made incompatible by another requested transformation.

10. **No in-place transformation**  
    Input and output application directories must always differ.

11. **Associations respected**  
    Transformations may act on logical groups of primary and associated files.

12. **Plan before execution**  
    All rules must be fully resolved and validated before filesystem mutation begins.

13. **No silent conflict resolution**  
    Ambiguous or contradictory intent must fail validation or planning.

14. **Projected state validation**  
    The intended output state must be validatable before execution where sufficient information is available.

15. **History free**  
    Execution history, logs and actual operation records belong in separate artefacts.

16. **Versioned**  
    Transformation Specification schema evolution must be explicitly managed.

17. **Extensible**  
    New transformation and optimisation types must be addable without redesigning the core specification model.

---

## 33. Conceptual Example

A Transformation Specification should be capable of expressing intent equivalent to:

> Starting from this validated application state:
>
> - Retarget the application to `net10.0-windows`.
> - Produce a self-contained `win-x64` deployment.
> - Use the latest permitted runtime patch.
> - Update explicitly selected non-core libraries within allowed version ranges.
> - Preserve application and plugin assemblies.
> - Remove runtime assets targeting other platforms.
> - Retain `en` and `en-*` resources and remove other localisations.
> - Preserve configuration files associated with included assemblies.
> - Exclude XML documentation.
> - Place PDB files in a separate symbols output.
> - Apply ReadyToRun to all eligible application and plugin assemblies except explicitly excluded assemblies.
> - Preserve all otherwise unclassified content.
>
> The specification describes these rules without listing every physical file affected.
>
> Planning expands the rules into concrete file-level actions and produces a projected Application Specification before execution.

---