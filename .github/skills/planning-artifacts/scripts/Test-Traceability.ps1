[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string]$Path
)

$ErrorActionPreference = "Stop"
$repoRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$fileName = Split-Path -Leaf $Path
if ($fileName -notmatch '^(EP-\d+|FT-\d+|WU-\d+)') {
    throw "Cannot derive an artifact ID (EP-/FT-/WU-) from '$fileName'."
}
$artifact = $Matches[1]

$criteria = foreach ($line in Get-Content -LiteralPath $Path) {
    if ($line -match '^\s*- \[[ xX]\] ((?:AC|FC|EC|MC)-\d+)(?: \(([TIAD])\))?') {
        [pscustomobject]@{ Id = $Matches[1]; Method = if ($Matches[2]) { $Matches[2] } else { '?' } }
    }
}

$traits = @{}
Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests') -Recurse -File -Filter *.cs |
    Select-String -Pattern ('Trait\(\s*"AC"\s*,\s*"' + [regex]::Escape($artifact) + '/([A-Z]+-\d+)"\s*\)') -AllMatches |
    ForEach-Object {
        foreach ($m in $_.Matches) {
            $id = $m.Groups[1].Value
            if (-not $traits.ContainsKey($id)) { $traits[$id] = [System.Collections.Generic.List[string]]::new() }
            $traits[$id].Add("$($_.Path.Substring($repoRoot.Length + 1)):$($_.LineNumber)")
        }
    }

$failed = $false
foreach ($c in $criteria) {
    $count = if ($traits.ContainsKey($c.Id)) { $traits[$c.Id].Count } else { 0 }
    $state = if ($c.Method -eq 'T' -and $count -eq 0) { $failed = $true; 'MISSING TESTS' }
        elseif ($c.Method -eq '?') { 'no method (legacy)' }
        else { 'ok' }
    "{0,-6} ({1}) tests={2,-3} {3}" -f $c.Id, $c.Method, $count, $state
}

$known = @($criteria.Id)
foreach ($id in $traits.Keys | Where-Object { $_ -notin $known } | Sort-Object) {
    $failed = $true
    "ORPHAN $artifact/$id -> $($traits[$id] -join ', ')"
}

if ($failed) { exit 1 }
