# Requirements Specification
## Command‑line Utility for Generating Ready‑to‑Run .NET Desktop Deployments

---

## 1. Purpose and Scope

This utility is a **Windows‑only command‑line tool** for transforming an existing .NET desktop application deployment directory into a **deployable, architecture‑specific, Ready‑to‑Run (R2R) optimised application layout**.

Primary goals:

- Improve **startup performance**
- Enable **ReadyToRun (R2R)** compilation using the .NET SDK (`crossgen2`)
- Support **corporate redeployment scenarios** (e.g. Intune)
- Convert between **framework‑dependent (FD)** and **self‑contained (SC)** deployments
- Support **patching, re‑targeting, and lifecycle management** of previously deployed applications

The tool operates **post‑build**, on **binary deployments**, not on source projects.

---

## 2. Target Platform and Constraints

### 2.1 Operating system and architecture

- Operating system: **Windows**
- Runtime Identifier (RID): **win-x64 only**

Multi‑RID and cross‑architecture output are explicitly out of scope for v1.

---

### 2.2 Application types in scope

- WPF applications
- WinForms applications
- Console applications
- WCF services (Windows‑hosted)

ASP.NET and web workloads are out of scope.

---

### 2.3 Privilege and execution assumptions

- Must run entirely in **user scope**
- Must **not require administrator privileges**
- Assumes required .NET SDKs are installed and available via `dotnet` on PATH
- May use **NuGet** to acquire non‑core Microsoft or third‑party libraries

---

## 3. Input Requirements

### 3.1 Primary input

- A single **input directory tree**
  - Represents an existing application deployment
  - May contain arbitrary sub‑folders
  - May be framework‑dependent or self‑contained
  - May contain managed assemblies, native binaries, configuration, and content

No source code, `.csproj`, or `.sln` files are assumed to be available.

---

### 3.2 Configuration inputs

The tool must support configuration via:

- Command‑line arguments
- Configuration file
- Response file

Configuration sources must be mergeable with deterministic precedence.

---

## 4. Output Requirements

### 4.1 Primary output

- A single **output directory tree**
- Represents a **ready‑to‑deploy application**
- Folder structure:
  - May differ from input
  - Must be deterministic given identical inputs and environment

---

### 4.2 Metadata output (optional but supported)

- Optional **JSON metadata output**
- Must be machine‑readable
- May describe:
  - Framework vs application assemblies
  - Native vs managed binaries
  - Target TFM and deployment model
  - Version provenance

Metadata may be reused as input for later patching or re‑targeting runs.

---

### 4.3 Determinism

- Repeated runs on the same machine with identical inputs and configuration should produce equivalent outputs
- Output equivalence is **not required across differing SDK/tooling versions**

---

## 5. Deployment Model Transformations

The tool must support all combinations:

- Framework‑dependent → Framework‑dependent
- Framework‑dependent → Self‑contained
- Self‑contained → Framework‑dependent
- Self‑contained → Self‑contained

These transformations must be explicit and configurable.

---

## 6. Target Framework and Runtime Requirements

### 6.1 Target Framework Moniker (TFM)

- The tool must allow specifying a target TFM
- Must update configuration files as required (e.g. `.runtimeconfig.json`)
- Must ensure required assemblies are present for the target TFM
- May remove incompatible assemblies

---

### 6.2 ReadyToRun (R2R) generation

- Managed assemblies must be compiled to **ReadyToRun (R2R)** images
- Must target `win-x64`
- Uses the .NET SDK R2R pipeline (`crossgen2`)
- Assemblies targeting other RIDs must be removed

---

## 7. File Handling and Pruning

### 7.1 Native binaries

- Copy native binaries targeting `win-x64`
- Remove native binaries for other RIDs

---

### 7.2 Non‑executable files

- Configuration files and content files must be copied unless excluded by filters

---

### 7.3 Resource files

- Language resources must be pruned
- Only English resources are retained:
  - `en`
  - recognised English variants (e.g. `en-US`, `en-AU`, `en-GB`)
- Language definitions may be configurable

---

## 8. Debug Symbols and Documentation

### 8.1 PDB handling

The tool must support:

- Including PDB files in the primary output
- Excluding PDB files entirely
- Extracting PDB files into a **separate symbols package**

---

### 8.2 XML documentation handling

- Assembly XML documentation files may be included or excluded
- Applies to application assemblies and framework assemblies

---

### 8.3 Symbols package

When enabled:

- A separate symbols package containing PDB files must be created
- May be a directory or archive
- Must be deterministic relative to the primary output

---

## 9. Re‑targeting and Patching Support

### 9.1 Framework patch replacement

For self‑contained deployments, the tool must support:

- Replacing framework assemblies with newer patch versions  
  (e.g. .NET `8.0.13` → `8.0.25`)

This includes managed and native runtime components.

---

### 9.2 TFM re‑targeting

- Support re‑targeting between TFMs (e.g. `net8.0` → `net10.0`)
- Must update configuration and assemblies accordingly
- Applies to both framework‑dependent and self‑contained outputs

---

### 9.3 Library updates via NuGet

- Non‑core Microsoft and third‑party libraries may be updated from NuGet
- Updates must be explicitly configured
- Backwards compatibility is assumed but not guaranteed
- Silent upgrades must not occur

---

### 9.4 Metadata‑assisted lifecycle operations

- JSON metadata output may be consumed to assist:
  - patching
  - re‑targeting
  - safe dependency classification

---

## 10. Folder Specification Support

### 10.1 Folder specification file

The tool must support an optional **folder specification file** that:

- Describes semantic meaning of folders
- Controls classification, pruning, reference resolution, and filtering
- Is structured and machine‑readable (e.g. JSON)

If absent, default heuristics must apply.

---

### 10.2 Folder definitions

Folder definitions may include:

- Stable identifiers (`id`)
- Reuse via references (`id_ref`)
- Folder masks (wildcards, glob patterns)
- Recursion flags
- Nested folder definitions

---

### 10.3 File classification

Supported file type classifications include (non‑exhaustive):

- assemblies
- platform_assemblies
- binaries
- platform_binaries
- config
- content

Classification affects R2R generation, pruning, and packaging decisions.

---

### 10.4 Reference paths

Folder definitions may specify reference paths such as:

- root
- parent
- current
- `<path>\runtimes`

Duplicate reference handling must be configurable.

---

### 10.5 Plugin and composite layouts

The specification must support:

- Plugin‑based directory layouts
- Multiple dependency graphs
- Repeated structural patterns

---

## 11. Filtering

### 11.1 Include / exclude filters

- Filters may be applied to files and folders
- Standard wildcard / glob semantics must be supported
- Filters may be global or folder‑specific

---

### 11.2 Filter precedence

- Filter application order must be deterministic and well‑defined

---

## 12. Error Handling and Diagnostics

- Error handling behaviour must be configurable
- Modes should include at least:
  - fail‑fast
  - warn‑and‑continue
- Failures must be clearly reported
- Exit codes must be meaningful and automation‑friendly

---

## 13. Non‑Goals (Explicitly Out of Scope)

- Source‑based builds
- Multi‑RID output
- Processor‑specific code generation
- Profile‑Guided Optimisation (PGO)
- Installer generation (MSI, MSIX)
- Arbitrary IL rewriting beyond R2R

---

## 14. Future Considerations (Design Constraints Only)

- PGO
- Processor‑specific optimisations
- Multi‑RID support

These must not be precluded by initial design, but are not required for v1.

---