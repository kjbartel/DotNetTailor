# Application Specification Structure
## Binary-First .NET Application Analysis and Repackaging Tool

---

## 1. Purpose

The **Application Specification** is a declarative, machine-readable description of the current state and structure of a .NET application deployment.

It describes:

- Application identity and scope
- Deployment model
- Target frameworks
- Platforms and architectures
- Runtime and framework dependencies
- Folder structure and folder semantics
- File classification rules
- Relationships between associated files
- Managed and native dependency resolution
- Plugin structure and dependencies
- Capabilities relevant to subsequent transformations

The Application Specification describes **what the application is**, not **what transformations should be performed**.

Transformation intent belongs in a separate **Transformation Specification**.

The Application Specification must not contain:

- Transformation instructions
- Transformation history
- Execution logs
- Detailed operation records
- Audit logs

These may be represented by separate artefacts where required.

---

## 2. Lifecycle

### 2.1 Creation

An Application Specification may be created by:

1. Analysing an existing application folder tree.
2. Repackaging an existing application state to produce a new application state.
3. Projecting the result of a transformation during a dry run.

### 2.2 Analysis

Analysis uses heuristics to construct an initial Application Specification.

Because heuristic analysis may not correctly identify all application semantics, the generated specification is expected to be human-reviewable and editable.

Examples of classifications that may require correction include:

- Plugin folders
- Runtime folders
- Platform-specific folders
- Resource folders
- Content folders
- Recursion boundaries
- Reusable folder structures
- Assembly resolution scopes

### 2.3 User refinement

A user may modify the generated Application Specification to more accurately describe the application.

Typical refinements may include:

- Replacing explicit folder mappings with recursive rules
- Replacing repeated folder definitions with references to reusable definitions
- Correcting folder roles
- Correcting file classification rules
- Correcting plugin boundaries
- Adjusting assembly reference paths
- Adding application-specific rules or exceptions

### 2.4 Validation

An Application Specification must be validatable against the physical application folder tree that it describes.

A successfully validated specification becomes the authoritative description of that application state for subsequent transformations.

### 2.5 Repackaging

A repackaging operation consumes:

- An application folder tree
- Its validated Application Specification
- A Transformation Specification

It produces:

- A different application folder tree
- A new Application Specification describing the resulting state

The resulting Application Specification describes only the resulting state. It does not contain the history of operations that produced that state.

---

## 3. Design Principles

### 3.1 State, not intent

The Application Specification describes existing or projected application state.

It must not specify desired transformations.

### 3.2 Rule-based representation

The specification should describe files and folders using rules rather than explicitly enumerating every physical file or folder.

Wildcards, reusable definitions, recursion, and semantic associations should be preferred where they produce an accurate and concise description.

### 3.3 Complete coverage

Every file within the defined application scope must be accounted for by the effective specification.

This does not require every file to be explicitly listed.

Instead, classification rules must collectively provide complete coverage.

### 3.4 Catch-all behaviour

Classification scopes must support an explicit catch-all rule representing files not otherwise classified.

This commonly represents ordinary application content.

The catch-all ensures that new or unrecognised files are not silently omitted.

### 3.5 Human editability

The specification must remain practical for a technically competent user to inspect and edit manually.

Analysis should therefore prefer concise semantic rules over unnecessarily expanded representations.

### 3.6 Determinism

Applying a validated Application Specification to the same application folder tree must produce the same effective classification and dependency model.

### 3.7 Portability

Paths represented in the specification should normally be relative to the application root.

The specification should not unnecessarily depend on the machine or absolute location from which analysis was performed.

---

## 4. Document Organisation

An Application Specification may consist of:

- A single document; or
- A root Application Specification referencing subsidiary specification documents.

The logical specification is considered one unit regardless of its physical decomposition.

A multi-document representation should be supported where this improves:

- Readability
- Reuse
- Maintainability
- Application-specific customisation
- Sharing of common classification definitions

---

## 5. Specification Identity and Schema

### 5.1 Purpose

Identifies the specification format and controls schema compatibility.

### 5.2 Information

The specification should identify:

- Specification type
- Schema version
- Tool version that generated the specification, where applicable

Creation timestamps may be included as informational metadata but must not affect application-state semantics or deterministic processing.

### 5.3 Requirements

- Schema version must be explicit.
- Incompatible schema versions must be rejected.
- Schema evolution must be supported.
- Tool version and schema version must be treated as separate concepts.

---

## 6. Application Identity and Scope

### 6.1 Purpose

Identifies the application and establishes the root scope of the specification.

### 6.2 Information

May include:

- Application identifier
- Application display name
- Application version
- Publisher
- Product information
- Application root
- Application entry points

Identity information may be explicit or discovered.

### 6.3 Scope

One Application Specification describes one logical application deployment.

All physical paths should normally be relative to the application root.

---

## 7. Application Execution Model

### 7.1 Purpose

Describes the application's current execution and deployment characteristics.

### 7.2 Information

May include:

- Framework-dependent or self-contained deployment
- Application host executables
- Managed entry assemblies
- Runtime configuration files
- Dependency manifests
- Relevant runtime/framework versions

### 7.3 Multiple entry points

An application tree may contain multiple executable entry points, including:

- Main application executables
- Console utilities
- Services
- Plugin hosts
- Supporting tools

The specification must be capable of representing these without requiring them to be separate application specifications.

---

## 8. Platform and Architecture Model

### 8.1 Purpose

Describes the platforms and processor architectures represented by the application tree.

### 8.2 Information

May include:

- Operating systems
- Runtime Identifiers (RIDs)
- Processor architectures
- Platform-neutral content
- Platform-specific managed assemblies
- Platform-specific native binaries

### 8.3 Initial platform

Initial implementation may primarily support:

- Windows
- `win-x64`

The specification model must not unnecessarily prevent future representation of Linux or additional RIDs and architectures.

---

## 9. Target Framework and Framework Model

### 9.1 Purpose

Describes the managed framework requirements of the application.

### 9.2 Information

May include:

- Target Framework Moniker (TFM)
- Runtime framework references
- Desktop framework references
- Framework names
- Framework versions
- Runtime patch versions

### 9.3 Multiple framework contexts

The specification must support cases where different executable components or plugin groups have distinct framework information.

### 9.4 Confidence

Information inferred heuristically may optionally record how it was determined, for example:

- Explicit
- Derived
- Inferred
- Unknown

Such information may assist user review but must not substitute for validation.

---

## 10. Folder Model

### 10.1 Purpose

Describes the semantic structure of the application's physical folder tree.

The folder model should avoid enumerating every folder when recursion, patterns, or reusable definitions can describe the same structure accurately.

### 10.2 Folder definitions

A folder definition may specify:

- Logical identifier
- Physical folder mask
- Folder role
- Whether matching folders recurse
- Applicable file classification groups
- Reference paths
- Child folder definitions
- Reusable folder-definition references

### 10.3 Folder masks

Folder masks may use standard wildcard/glob mechanisms.

Typical examples include:

- Exact names
- `*`
- `**`
- Language/culture patterns
- Platform/RID patterns

### 10.4 Recursion

A folder definition may state that its semantics apply recursively.

This should be preferred to enumerating large repeating subtrees where all descendants share the same semantics.

### 10.5 Reusable definitions

Folder definitions may be identified and referenced by other definitions.

This supports repeating structures such as:

- Plugin layouts
- Runtime layouts
- Resource structures

References may permit explicitly defined overrides where required.

### 10.6 Folder roles

Example semantic roles may include:

- Application root
- Application components
- Plugin
- Runtime
- Platform assets
- Resources
- Content

Folder roles must be extensible.

### 10.7 Catch-all folders

A folder definition may use a catch-all pattern for child folders that are not otherwise semantically classified.

Such folders can normally be treated as ordinary content unless explicitly assigned another role.

---

## 11. File Classification Model

### 11.1 Purpose

Classifies files within each applicable folder scope without requiring every file to be individually enumerated.

### 11.2 Classification groups

A folder may contain multiple classification groups because mixed file types are expected.

Example groups include:

- Managed assemblies
- Platform-specific managed assemblies
- Native binaries
- Platform-specific native binaries
- Configuration
- Resources
- Debug symbols
- XML documentation
- General content

Classification groups must be extensible.

### 11.3 Matching

Groups may classify files using combinations of:

- Filename wildcards
- Extensions
- Name semantics
- Binary inspection
- Associated-file rules
- Explicit exceptions

For example, `*.dll` alone may not be sufficient to distinguish a managed assembly from a native DLL. Classification may therefore combine filename matching with binary inspection.

### 11.4 Rule precedence

Where multiple rules could potentially match a file, precedence must be explicit and deterministic.

A file must resolve to one effective primary classification.

### 11.5 Wildcard preference

Wildcards and semantic patterns should be used wherever they accurately describe a group.

Explicit file lists should be reserved for:

- Exceptions
- Irregular application-specific structures
- Cases where no reliable semantic rule exists

### 11.6 Catch-all classification

Every applicable file scope must provide an effective catch-all classification.

The catch-all represents files which do not belong to a more specific classification.

The normal catch-all classification is expected to be general application content.

This guarantees that unknown files are preserved and made visible rather than silently omitted.

---

## 12. File Associations and Semantic Groups

### 12.1 Purpose

Files which logically belong to another file or component must be expressible as associations rather than unrelated classifications.

This allows transformations to operate on the logical component as a group.

### 12.2 Primary files

A primary file may have zero or more associated files.

Common primary files include:

- Managed assemblies
- Application executables
- Native libraries

### 12.3 Associated file types

Examples include:

- Assembly configuration
- Runtime configuration
- Dependency manifests
- Debug symbols
- XML documentation
- Resource assemblies
- Other sidecar metadata

### 12.4 Name-based association

Associations should normally be derived using filename semantics.

For example, files associated with `Application.dll` may include:

- `Application.pdb`
- `Application.xml`
- `Application.dll.config`

An executable application component may additionally be associated with files such as:

- `Application.runtimeconfig.json`
- `Application.deps.json`

Exact supported association types and naming rules are defined by the schema.

### 12.5 Association semantics

Associations must permit later transformations to express operations against logical groups.

Examples include:

- Include an assembly but exclude its symbols.
- Move symbols into a separate symbols package.
- Retarget an executable and modify its runtime configuration.
- Remove an assembly together with its documentation and associated configuration.
- Preserve an assembly and all associated resources.

### 12.6 Missing associated files

An association rule may describe optional or required associated files.

Validation must distinguish:

- An association that legitimately has no matching file
- A required associated file that is missing

---

## 13. Managed Assembly Model

### 13.1 Purpose

Describes the semantics required to understand and transform managed assemblies.

### 13.2 Assembly information

Information may include:

- Assembly name
- Assembly version
- Culture
- Public key token / strong-name identity
- TFM information
- Architecture information
- Existing ReadyToRun state
- Framework/application/plugin role
- Managed assembly references

### 13.3 Rule-based representation

Assemblies need not all be explicitly listed where a classification rule accurately describes them.

However, derived assembly identity and dependency information must remain available to validation and transformation planning.

This information may be materialised as separate analysis artefacts rather than expanding the human-maintained specification.

---

## 14. Native Binary Model

### 14.1 Purpose

Describes native executables and libraries relevant to application operation and deployment.

### 14.2 Information

May include:

- Binary role
- Processor architecture
- Platform/RID affinity
- Framework/runtime ownership
- Application ownership

### 14.3 Classification

Native files should normally be identified by a combination of:

- Location semantics
- Filename rules
- Binary inspection

The model must not assume that all `.dll` files are managed assemblies.

---

## 15. Dependency and Reference Model

### 15.1 Purpose

Describes how managed components resolve dependencies.

### 15.2 Reference paths

Folder definitions may define ordered assembly reference scopes such as:

- Current folder
- Parent folder
- Application root
- Runtime subfolder
- Other explicitly defined relative paths

Reference paths must not implicitly escape application scope.

### 15.3 Dependency discovery

Dependencies are discovered from managed assembly metadata.

The Application Specification defines the semantic rules required to resolve those dependencies.

### 15.4 Duplicate references

The specification must allow deterministic handling of duplicate assembly candidates.

Duplicate handling may vary between folder contexts.

### 15.5 Derived dependency graph

The resolved dependency graph is primarily derived state.

It may be:

- Produced as a separate analysis artefact
- Used during validation
- Used during transformation planning

It need not require explicit enumeration of every dependency edge in the human-maintained Application Specification where those edges can be deterministically rediscovered.

---

## 16. Plugin Model

### 16.1 Purpose

Represents plugin boundaries and the dependency-resolution rules applying to plugin components.

### 16.2 Plugin identification

Plugins should normally be identified through folder semantics and reusable folder definitions rather than explicit lists of every plugin.

### 16.3 Plugin dependencies

Plugins may reference:

- Framework assemblies
- Core application assemblies
- Other plugin assemblies

Inter-plugin binary dependencies may only be one-way or absent.

Mutual or cyclic inter-plugin binary dependencies are invalid.

### 16.4 Dependency injection

Runtime relationships established through dependency injection do not alter binary dependency semantics.

Shared contracts and interfaces are treated as ordinary one-way managed assembly references.

---

## 17. Resources and Localisation

### 17.1 Purpose

Describes language and culture-specific resources.

### 17.2 Classification

Resource folders and files should normally be represented using:

- Culture/language folder patterns
- Resource assembly naming semantics
- Associations with primary managed assemblies

### 17.3 State versus transformation

The Application Specification describes which resources exist and how they are related.

Rules such as retaining only English resources belong in the Transformation Specification.

---

## 18. Symbols and Documentation

### 18.1 Purpose

Identifies debugging and documentation files and their association with primary application components.

### 18.2 Debug symbols

PDB files should normally be associated with their relevant managed or native binary using name semantics.

### 18.3 XML documentation

XML assembly documentation should normally be associated with its managed assembly rather than treated as unrelated content.

### 18.4 Transformation independence

Whether symbols or documentation are:

- Included
- Excluded
- Moved to another package

is transformation intent and must not be encoded in the Application Specification.

---

## 19. Capability Assessment

### 19.1 Purpose

Analysis may identify transformations that appear possible for the current application state.

### 19.2 Potential capabilities

May include:

- Retargeting
- Runtime patching
- Library patching
- Framework-dependent to self-contained conversion
- Self-contained to framework-dependent conversion
- ReadyToRun optimisation
- Future optimisation mechanisms

### 19.3 Advisory nature

Capability assessment is derived information.

It may describe a capability as:

- Supported
- Unsupported
- Conditional
- Unknown

It does not represent transformation intent and does not guarantee successful transformation.

---

## 20. Validation State

### 20.1 Purpose

Indicates whether the Application Specification has been validated against a particular application folder tree.

### 20.2 States

At minimum:

- Unvalidated
- Validated
- Validated with warnings
- Invalid

### 20.3 Validation relationship

Validation must verify the effective specification against the actual application state, including:

- Folder matching
- File classification
- Catch-all coverage
- File associations
- Managed dependencies
- Reference paths
- Plugin relationships
- Runtime/framework identification
- Platform assumptions

### 20.4 Manual modification

A user-modified Application Specification must be revalidated before it can be used for repackaging.

---

## 21. Multi-Document Specification

### 21.1 Purpose

Large or complex applications may benefit from decomposing the Application Specification into multiple physical files.

### 21.2 Root specification

A root specification acts as the authoritative entry point and may reference subsidiary documents.

### 21.3 Candidate subsidiary documents

The specification may be divided into documents such as:

- Application identity and runtime information
- Folder semantics
- File classifications
- File associations
- Reference-resolution rules
- Plugin semantics
- Application-specific overrides

The exact decomposition is an implementation/schema design decision.

### 21.4 Reuse

Subsidiary documents may be reusable where appropriate.

For example, common classification or runtime-folder definitions might be shared across applications.

Application-specific semantics must remain clearly distinguishable from reusable generic definitions.

### 21.5 Resolution

All referenced documents must be resolvable deterministically.

Circular document inclusion must be invalid.

---

## 22. Derived Analysis Artefacts

### 22.1 Purpose

Analysis may produce information which is valuable but inappropriate for inclusion in the human-maintained Application Specification.

### 22.2 Example artefacts

Separate analysis artefacts may include:

- Expanded physical file inventory
- Expanded folder tree
- File-to-classification mapping
- Managed assembly inventory
- Assembly dependency graph
- Plugin dependency graph
- Runtime/framework inventory
- Validation report
- Capability report

### 22.3 Relationship to the Application Specification

These artefacts are derived from:

- The application tree
- The effective Application Specification

They are not themselves authoritative state specifications.

They may be regenerated when required.

---

## 23. Location and Portability

### 23.1 Default location

Analysis should default to writing the root Application Specification into the application folder where that location is writable.

### 23.2 Alternate location

The user must be able to specify another location.

This is required for cases including:

- Read-only application folders
- Installed applications under protected directories
- CI/CD workspaces
- Centralised specification repositories

### 23.3 Path handling

The specification must remain valid independently of its storage location.

Application paths should therefore be application-root-relative rather than specification-file-relative except where explicitly defined otherwise.

---

## 24. Global Invariants

A valid Application Specification must satisfy the following principles:

1. **State only**  
   The specification describes application state, not transformation intent or operation history.

2. **Rule based**  
   Files and folders should be represented using semantic rules wherever practical instead of exhaustive enumeration.

3. **Complete coverage**  
   Every file in application scope must resolve to an effective classification, including through catch-all rules.

4. **Deterministic**  
   A specification must produce the same semantic interpretation for the same application state.

5. **Human editable**  
   Analysis output must be practical for technically competent users to review and refine.

6. **Validatable**  
   User changes must be mechanically verifiable against the physical application tree.

7. **No silent omissions**  
   Unknown files must fall into an explicit catch-all classification rather than disappearing from the model.

8. **Explicit associations**  
   Related files such as symbols, documentation, configuration and resources must be expressible as semantic associations with their primary files.

9. **Relative and portable**  
   Application paths should not depend on the application's absolute installation location.

10. **Composable**  
    Repeated folder and classification semantics should be reusable rather than unnecessarily duplicated.

11. **Versioned**  
    Schema evolution is expected and must be explicitly managed.

12. **History free**  
    Logs, transformation records and audit history belong in separate artefacts.

---

## 25. Relationship to Other Documents

The Application Specification forms one part of the overall transformation model.

### Application Specification

Describes:

> **What exists?**

### Transformation Specification

Describes:

> **What should change?**

### Transformation Plan

Derived from the two specifications and describes:

> **What actions will the tool perform?**

### Analysis and Validation Artefacts

Describe:

> **What did the tool discover, derive, or validate?**

### Execution Logs and Reports

Describe:

> **What happened during execution?**

These concerns must remain separate so that application state, user intent, derived plans, and execution history cannot be confused.

---