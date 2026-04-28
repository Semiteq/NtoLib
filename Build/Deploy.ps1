[CmdletBinding()]
param(
    [string]$Configuration = $env:BUILD_CONFIGURATION,
    [string]$RepoRoot = $env:REPO_ROOT,
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
    throw 'REPO_ROOT is required.'
}
if (-not (Test-Path $RepoRoot))
{
    throw "REPO_ROOT does not exist: $RepoRoot"
}
if ( [string]::IsNullOrWhiteSpace($DestinationDirectory))
{
    throw 'NTOLIB_DEST_DIR is required.'
}
if ( [string]::IsNullOrWhiteSpace($ConfigurationDirectory))
{
    throw 'NTOLIB_CONFIG_DIR is required.'
}

$solutionPath = Join-Path $RepoRoot 'NtoLib.sln'
if (-not (Test-Path $solutionPath))
{
    throw "Solution not found: $solutionPath"
}

dotnet build $solutionPath -c $Configuration -v minimal
if ($LASTEXITCODE -ne 0)
{
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

# Bundle NuGet runtime dependencies into NtoLib.dll via the ILRepack.targets MSBuild target.
# Gated by /p:RunILRepack=true so the preceding solution build stays un-merged
# (see NtoLib/ILRepack.targets header for the rationale).
$projectPath = Join-Path $RepoRoot 'NtoLib\NtoLib.csproj'
dotnet build $projectPath -c $Configuration -v minimal -p:RunILRepack=true --no-restore
if ($LASTEXITCODE -ne 0)
{
    throw "ILRepack merge step failed with exit code $LASTEXITCODE."
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
    New-Item -ItemType Directory -Force -Path $ConfigurationDirectory | Out-Null
    Copy-Item (Join-Path $cfgSrc '*') $ConfigurationDirectory -Recurse -Force
}
