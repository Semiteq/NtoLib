[CmdletBinding()]
param(
    [string]$Configuration = $env:BUILD_CONFIGURATION,
    [string]$RepoRoot,
    [string]$DestinationDirectory = $env:NTOLIB_DEST_DIR,
    [string]$ConfigurationDirectory = $env:NTOLIB_CONFIG_DIR
)

$ErrorActionPreference = 'Stop'

if ( [string]::IsNullOrWhiteSpace($Configuration))
{
    throw 'BUILD_CONFIGURATION is required.'
}
if ( [string]::IsNullOrWhiteSpace($RepoRoot))
{
    throw 'RepoRoot is required.'
}
if (-not (Test-Path $RepoRoot))
{
    throw "RepoRoot does not exist: $RepoRoot"
}
if ( [string]::IsNullOrWhiteSpace($DestinationDirectory))
{
    throw 'NTOLIB_DEST_DIR is required.'
}
if ( [string]::IsNullOrWhiteSpace($ConfigurationDirectory))
{
    throw 'NTOLIB_CONFIG_DIR is required.'
}

$binDir = Join-Path (Join-Path $RepoRoot 'NtoLib') "bin\$Configuration"
if (-not (Test-Path $binDir))
{
    throw "Build output directory not found: $binDir"
}

New-Item -ItemType Directory -Force -Path $DestinationDirectory | Out-Null

Copy-Item (Join-Path $binDir 'NtoLib.dll') (Join-Path $DestinationDirectory 'NtoLib.dll') -Force
Copy-Item (Join-Path $binDir 'NtoLib.pdb') (Join-Path $DestinationDirectory 'NtoLib.pdb') -Force -ErrorAction SilentlyContinue

$resourcesExt = Join-Path $binDir 'System.Resources.Extensions.dll'
if (Test-Path $resourcesExt)
{
    Copy-Item $resourcesExt (Join-Path $DestinationDirectory 'System.Resources.Extensions.dll') -Force
}

$cfgSrc = Join-Path $RepoRoot 'DefaultConfig'
if (Test-Path $cfgSrc)
{
    if (Test-Path $ConfigurationDirectory)
    {
        Remove-Item $ConfigurationDirectory -Recurse -Force
    }
    Copy-Item $cfgSrc $ConfigurationDirectory -Recurse -Force
}
