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

$binDir = Join-Path $RepoRoot "NtoLib\bin\$Configuration"
if (-not (Test-Path $binDir))
{
    throw "Build output directory not found: $binDir"
}

$ilRepackExe = Join-Path $RepoRoot 'packages\ILRepack.2.0.44\tools\ILRepack.exe'
if (-not (Test-Path $ilRepackExe))
{
    throw "ILRepack.exe not found: $ilRepackExe"
}

$finalDll = Join-Path $binDir 'NtoLib.dll'
if (-not (Test-Path $finalDll))
{
    throw "Target assembly not found: $finalDll"
}

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
    '^ICSharpCode\..*\.dll$',
    '^System\.Text\.Json\.dll$',
    '^System\.Text\.Encodings\.Web\.dll$'
)

function IsExcluded([string]$fileName)
{
    foreach ($rx in $excludedNameRegexes)
    {
        if ($fileName -match $rx)
        {
            return $true
        }
    }
    return $false
}

$mergeOthers = @()
$allDlls = Get-ChildItem -Path $binDir -Filter *.dll -File
foreach ($f in $allDlls)
{
    if (IsExcluded $f.Name)
    {
        continue
    }
    $mergeOthers += $f.FullName
}

if ($mergeOthers.Count -eq 0)
{
    return
}

$backupDll = Join-Path $binDir 'NtoLib.ilrepack.input.dll'
if (Test-Path $backupDll)
{
    Remove-Item $backupDll -Force
}

Move-Item -Path $finalDll -Destination $backupDll -Force

$args = @(
    '/target:library',
    "/out:$finalDll",
    '/skipconfig'
)

foreach ($d in $probeDirs)
{
    if (-not (Test-Path $d))
    {
        throw "ILRepack probe directory not found: $d"
    }
    $args += "/lib:$d"
}

if ($env:NTOLIB_ILREPACK_LOG -eq '1')
{
    $args += '/verbose'
    $args += "/log:$( $binDir )\ilrepack.log"
}

$args += $backupDll
$args += $mergeOthers

Push-Location $binDir
try
{
    & $ilRepackExe @args
    if ($LASTEXITCODE -ne 0)
    {
        throw "ILRepack failed with exit code $LASTEXITCODE."
    }
}
catch
{
    if (Test-Path $finalDll)
    {
        Remove-Item $finalDll -Force
    }
    Move-Item -Path $backupDll -Destination $finalDll -Force
    throw
}
finally
{
    Pop-Location
}

Remove-Item $backupDll -Force
