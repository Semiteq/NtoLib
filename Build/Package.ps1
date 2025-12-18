[CmdletBinding()]
param(
  [string]$Configuration = $env:BUILD_CONFIGURATION,
  [string]$RepoRoot = $env:REPO_ROOT
)

$ErrorActionPreference = 'Stop'
$ToolsDir = Join-Path $PSScriptRoot 'tools'

if ([string]::IsNullOrWhiteSpace($Configuration)) { throw 'BUILD_CONFIGURATION is required.' }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { throw 'REPO_ROOT is required.' }
if (-not (Test-Path $RepoRoot)) { throw "REPO_ROOT does not exist: $RepoRoot" }

& (Join-Path $ToolsDir 'Build.ps1') -Configuration $Configuration -RepoRoot $RepoRoot
& (Join-Path $ToolsDir 'Merge.ps1') -Configuration $Configuration -RepoRoot $RepoRoot
& (Join-Path $ToolsDir 'Archive.ps1') -Configuration $Configuration -RepoRoot $RepoRoot