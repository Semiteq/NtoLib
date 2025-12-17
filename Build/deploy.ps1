[CmdletBinding()]
param(
  [string]$Configuration = $env:BUILD_CONFIGURATION,
  [string]$RepoRoot = $env:REPO_ROOT,
  [string]$DestinationDirectory = $env:NTOLIB_DEST_DIR,
  [string]$ConfigurationDirectory = $env:NTOLIB_CONFIG_DIR
)

$ErrorActionPreference = 'Stop'
$ToolsDir = Join-Path $PSScriptRoot 'tools'

if ([string]::IsNullOrWhiteSpace($Configuration)) { throw 'BUILD_CONFIGURATION is required.' }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { throw 'REPO_ROOT is required.' }
if (-not (Test-Path $RepoRoot)) { throw "REPO_ROOT does not exist: $RepoRoot" }
if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) { throw 'NTOLIB_DEST_DIR is required.' }
if ([string]::IsNullOrWhiteSpace($ConfigurationDirectory)) { throw 'NTOLIB_CONFIG_DIR is required.' }

& (Join-Path $ToolsDir 'Build.ps1') -Configuration $Configuration -RepoRoot $RepoRoot
& (Join-Path $ToolsDir 'Copy.ps1') -Configuration $Configuration -RepoRoot $RepoRoot -DestinationDirectory $DestinationDirectory -ConfigurationDirectory $ConfigurationDirectory