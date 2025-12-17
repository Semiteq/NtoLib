[CmdletBinding()]
param(
  [string]$Configuration = $env:BUILD_CONFIGURATION,
  [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Configuration)) { throw 'BUILD_CONFIGURATION is required.' }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { throw 'RepoRoot is required.' }
if (-not (Test-Path $RepoRoot)) { throw "RepoRoot does not exist: $RepoRoot" }

$testProjectPath = Join-Path $RepoRoot 'Tests\Tests.csproj'
if (-not (Test-Path $testProjectPath)) { throw "Test project not found: $testProjectPath" }

dotnet test $testProjectPath -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE." }