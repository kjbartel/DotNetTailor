[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Root = ".",

    [string[]]$Exclude = @(),

    [switch]$IncludeDocumentation
)

$ErrorActionPreference = "Stop"
$resolvedRoot = (Resolve-Path -LiteralPath $Root).Path

function ConvertTo-NormalizedPath {
    param([string]$Path)

    return $Path.Replace("\", "/").TrimStart("./")
}

function Test-IsDocumentation {
    param([string]$Path)

    if ($IncludeDocumentation) {
        return $false
    }

    return $Path -match "(?i)(^|/)(docs?|documentation|specs?)(/|$)" -or
        $Path -match "(?i)\.md$" -or
        $Path -match "(?i)(^|/)(README|CHANGELOG|CONTRIBUTING|LICENSE)(\.[^/]*)?$"
}

function Test-IsExplicitlyExcluded {
    param([string]$Path)

    foreach ($pattern in $Exclude) {
        if ($Path -like $pattern) {
            return $true
        }
    }
    return $false
}

function Get-RepositoryEntries {
    param(
        [string]$RepositoryRoot,
        [string]$Prefix = ""
    )

    $entries = & git -C $RepositoryRoot ls-files --cached --others --exclude-standard 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not enumerate Git files under '$RepositoryRoot'."
    }

    foreach ($entry in $entries) {
        $normalizedEntry = ConvertTo-NormalizedPath $entry
        $relativePath = if ($Prefix) {
            ConvertTo-NormalizedPath (Join-Path $Prefix $normalizedEntry)
        }
        else {
            $normalizedEntry
        }
        $fullPath = Join-Path $RepositoryRoot $entry

        if (Test-Path -LiteralPath $fullPath -PathType Container) {
            Get-RepositoryEntries -RepositoryRoot $fullPath -Prefix $relativePath
            continue
        }

        [pscustomobject]@{
            RelativePath = $relativePath
            FullPath = $fullPath
        }
    }
}

$manifest = foreach ($entry in Get-RepositoryEntries -RepositoryRoot $resolvedRoot) {
    if ((Test-IsDocumentation $entry.RelativePath) -or
        (Test-IsExplicitlyExcluded $entry.RelativePath)) {
        continue
    }

    $fileHash = if (Test-Path -LiteralPath $entry.FullPath -PathType Leaf) {
        (Get-FileHash -LiteralPath $entry.FullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    else {
        "<missing>"
    }

    "{0}`t{1}" -f $entry.RelativePath, $fileHash
}

$orderedManifest = @($manifest | Sort-Object)
$manifestText = $orderedManifest -join "`n"
$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($manifestText)
    $fingerprint = ([System.BitConverter]::ToString($sha256.ComputeHash($bytes))).Replace("-", "").ToLowerInvariant()
}
finally {
    $sha256.Dispose()
}

[pscustomobject]@{
    fingerprint = $fingerprint
    files = $orderedManifest.Count
    includeDocumentation = [bool]$IncludeDocumentation
    exclusions = @($Exclude)
    root = $resolvedRoot
} | ConvertTo-Json -Depth 3
