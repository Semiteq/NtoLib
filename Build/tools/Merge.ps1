[CmdletBinding()]
param(
  [string]$Configuration = $env:BUILD_CONFIGURATION,
  [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Configuration)) { throw 'BUILD_CONFIGURATION is required.' }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { throw 'RepoRoot is required.' }
if (-not (Test-Path $RepoRoot)) { throw "RepoRoot does not exist: $RepoRoot" }

$binDir = Join-Path $RepoRoot "NtoLib\bin\$Configuration"
if (-not (Test-Path $binDir)) { throw "Build output directory not found: $binDir" }

$ilRepackExe = Join-Path $RepoRoot 'packages\ILRepack.2.0.44\tools\ILRepack.exe'
if (-not (Test-Path $ilRepackExe)) { throw "ILRepack.exe not found: $ilRepackExe" }

$targetDll = Join-Path $binDir 'NtoLib.dll'
if (-not (Test-Path $targetDll)) { throw "Target assembly not found: $targetDll" }

$probeDirs = @(
  $binDir,
  (Join-Path $RepoRoot 'Resources')
)

$excludedNameRegexes = @(
  '^NtoLib\.dll$',
  '^System\.Resources\.Extensions\.dll$',
  '^FB\.dll$',
  '^InSAT\..*\.dll$',
  '^Insat\..*\.dll$',
  '^MasterSCADA\..*\.dll$',
  '^MasterScada\..*\.dll$',
  '^MasterSCADALib\.dll$',
  '^ICSharpCode\..*\.dll$'
)

function IsExcluded([string]$fileName) {
  foreach ($rx in $excludedNameRegexes) {
    if ($fileName -match $rx) { return $true }
  }
  return $false
}

$mergeInputs = @()
$mergeInputs += $targetDll

$allDlls = Get-ChildItem -Path $binDir -Filter *.dll -File
foreach ($f in $allDlls) {
  if (IsExcluded $f.Name) { continue }
  $mergeInputs += $f.FullName
}

if ($mergeInputs.Count -le 1) { return }

$tempOut = Join-Path $binDir 'NtoLib.ilrepack.tmp.dll'
if (Test-Path $tempOut) { Remove-Item $tempOut -Force }

$args = @(
  '/target:library',
  "/out:$tempOut"
)

foreach ($d in $probeDirs) {
  if (-not (Test-Path $d)) { throw "ILRepack probe directory not found: $d" }
  $args += "/lib:$d"
}

$args += $mergeInputs

Push-Location $binDir
try {
  & $ilRepackExe @args
  if ($LASTEXITCODE -ne 0) { throw "ILRepack failed with exit code $LASTEXITCODE." }
}
finally {
  Pop-Location
}

Move-Item -Path $tempOut -Destination $targetDll -Force