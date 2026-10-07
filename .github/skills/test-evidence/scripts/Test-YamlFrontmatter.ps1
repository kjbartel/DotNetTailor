[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$Path,

    [string]$ModuleCache
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$dependency = (Import-PowerShellDataFile (Join-Path $PSScriptRoot '../PowerShellDependencies.psd1')).Yaml
if (-not $ModuleCache) {
    $ModuleCache = Join-Path $repoRoot 'artifacts/local-tools/powershell-modules'
}
$manifest = Join-Path $ModuleCache "$($dependency.Name)/$($dependency.RequiredVersion)/$($dependency.Name).psd1"
$setup = 'pwsh -NoProfile -File .github/skills/test-evidence/scripts/Initialize-PowerShellDependencies.ps1'
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "Missing $($dependency.Name) $($dependency.RequiredVersion) at '$manifest'. From the repository root run: $setup. Validation never installs dependencies."
}
try {
    $module = Import-Module -Name $manifest -Force -PassThru -ErrorAction Stop
    if ($module.Version.ToString() -ne $dependency.RequiredVersion) {
        throw "Expected version $($dependency.RequiredVersion), found $($module.Version)."
    }
}
catch {
    throw "Cannot import $($dependency.Name): $($_.Exception.Message) Remove the invalid cached version and run from the repository root: $setup"
}

if (-not $Path) {
    $Path = @('instructions', 'prompts', 'skills', 'agents') | ForEach-Object { Join-Path $repoRoot ".github/$_" }
}
$files = @(foreach ($entry in $Path) {
    $item = Get-Item -LiteralPath $entry -ErrorAction Stop
    if ($item.PSIsContainer) {
        Get-ChildItem -LiteralPath $item.FullName -Recurse -File | Where-Object {
            $_.Name -eq 'SKILL.md' -or $_.Name -match '\.(instructions|prompt|agent)\.md$'
        }
    }
    else {
        $item
    }
})
$files = @($files | Sort-Object -Property FullName -Unique)
if ($files.Count -eq 0) {
    throw 'No customization files found to validate.'
}

foreach ($file in $files) {
    $lines = @(Get-Content -LiteralPath $file.FullName)
    if ($lines.Count -lt 3 -or $lines[0] -notmatch '^---\s*$') {
        throw "$($file.FullName): missing opening YAML frontmatter delimiter."
    }
    $closing = -1
    for ($index = 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match '^---\s*$') {
            $closing = $index
            break
        }
    }
    if ($closing -lt 2) {
        throw "$($file.FullName): missing closing delimiter or empty YAML frontmatter."
    }
    try {
        $metadata = powershell-yaml\ConvertFrom-Yaml -Yaml ($lines[1..($closing - 1)] -join "`n") -ErrorAction Stop
    }
    catch {
        throw "$($file.FullName): invalid YAML frontmatter: $($_.Exception.Message)"
    }
    if ($metadata -isnot [System.Collections.IDictionary]) {
        throw "$($file.FullName): YAML frontmatter must be a mapping."
    }
    if ($metadata['description'] -isnot [string] -or [string]::IsNullOrWhiteSpace($metadata['description'])) {
        throw "$($file.FullName): description must be a non-empty string."
    }
    if ($file.Name -eq 'SKILL.md' -and $metadata['name'] -cne $file.Directory.Name) {
        throw "$($file.FullName): skill name must match folder '$($file.Directory.Name)'."
    }
    if ($file.Name -like '*.instructions.md' -and
        ($metadata['applyTo'] -isnot [string] -or [string]::IsNullOrWhiteSpace($metadata['applyTo']))) {
        throw "$($file.FullName): applyTo must be a non-empty string."
    }
}

"Validated $($files.Count) YAML frontmatters with $($dependency.Name) $($dependency.RequiredVersion)."
