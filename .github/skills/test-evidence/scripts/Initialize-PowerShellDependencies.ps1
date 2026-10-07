[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$dependency = (Import-PowerShellDataFile (Join-Path $PSScriptRoot '../PowerShellDependencies.psd1')).Yaml
$cache = Join-Path $repoRoot 'artifacts/local-tools/powershell-modules'
$manifest = Join-Path $cache "$($dependency.Name)/$($dependency.RequiredVersion)/$($dependency.Name).psd1"

if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    if (-not (Get-Command Save-Module -ErrorAction SilentlyContinue)) {
        throw 'Save-Module is required. Install PowerShellGet for this PowerShell host, then rerun this setup script.'
    }
    $null = New-Item -ItemType Directory -Path $cache -Force
    Save-Module -Name $dependency.Name -RequiredVersion $dependency.RequiredVersion -Repository $dependency.Repository -Path $cache -ErrorAction Stop
}

Import-Module -Name $manifest -Force -ErrorAction Stop
$module = Get-Module -Name $dependency.Name | Where-Object { $_.Path -eq (Join-Path (Split-Path $manifest) "$($dependency.Name).psm1") }
if (-not $module -or $module.Version.ToString() -ne $dependency.RequiredVersion) {
    throw "Expected $($dependency.Name) $($dependency.RequiredVersion) at '$manifest'. Remove the invalid cached version and rerun this setup script."
}
"Ready: $($dependency.Name) $($dependency.RequiredVersion) from $($dependency.Source)"
