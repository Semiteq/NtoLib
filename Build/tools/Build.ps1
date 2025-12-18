[CmdletBinding()]
param(
    [string]$Configuration = $env:BUILD_CONFIGURATION,
    [string]$RepoRoot
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
