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

$projectDir = Join-Path $RepoRoot 'NtoLib'
$binDir = Join-Path $projectDir "bin\$Configuration"
$releasesDir = Join-Path $RepoRoot 'Releases'
New-Item -ItemType Directory -Force -Path $releasesDir | Out-Null

$assemblyInfo = Join-Path $projectDir 'Properties\AssemblyInfo.cs'
$version = '1.0.0'
if (Test-Path $assemblyInfo)
{
    $content = Get-Content $assemblyInfo -Raw
    $m = [regex]::Match($content, '\[assembly:\s*AssemblyInformationalVersion\("([^"]+)"\)\]')
    if ($m.Success)
    {
        $version = $m.Groups[1].Value
    }
}

$tempDir = Join-Path $env:TEMP ("NtoLib\archive_{0}" -f ([guid]::NewGuid().ToString('N')))
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

Copy-Item (Join-Path $binDir 'NtoLib.dll') (Join-Path $tempDir 'NtoLib.dll') -Force

$resourcesExt = Join-Path $binDir 'System.Resources.Extensions.dll'
if (Test-Path $resourcesExt)
{
    Copy-Item $resourcesExt (Join-Path $tempDir 'System.Resources.Extensions.dll') -Force
}

$cfgSrc = Join-Path $RepoRoot 'DefaultConfig'
if (Test-Path $cfgSrc)
{
    Copy-Item $cfgSrc (Join-Path $tempDir 'DefaultConfig') -Recurse -Force
}

$bat = Join-Path $projectDir 'NtoLib_reg.bat'
if (Test-Path $bat)
{
    Copy-Item $bat (Join-Path $tempDir 'NtoLib_reg.bat') -Force
}

$zip = Join-Path $releasesDir "NtoLib_v$version.zip"
if (Test-Path $zip)
{
    Remove-Item $zip -Force
}
Compress-Archive -Path (Join-Path $tempDir '*') -DestinationPath $zip

Remove-Item $tempDir -Recurse -Force
