<#
.SYNOPSIS
    Publishes Vocal Coach for Windows x64, signs it with Azure Artifact Signing, and zips it.

.DESCRIPTION
    Produces the same self-contained build as .github/workflows/release.yml, but signs
    VocalCoach.App.exe and the app's own DLLs before zipping. Signing uses the
    JohnApolloni Artifact Signing account and the JohnApolloniPublic certificate profile
    (see build/signing/metadata.json).

    Before the first run:
      1. Install the Azure CLI:   winget install -e --id Microsoft.AzureCLI
      2. Sign in to the tenant:   az login --tenant johnapollonioutlook.onmicrosoft.com
    Your account needs the "Artifact Signing Certificate Profile Signer" role on the
    JohnApolloni account.

    The script downloads the Artifact Signing client and, if needed, the Windows SDK
    build tools (for a SignTool new enough to load the signing client) from nuget.org
    into %LOCALAPPDATA%\VocalCoach\signing-tools.

.PARAMETER SkipPublish
    Sign and zip whatever is already in the publish folder instead of publishing again.

.PARAMETER NoSign
    Publish and zip without signing (checks the rest of the script without Azure access).

.EXAMPLE
    ./build/Sign-Release.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [switch]$NoSign
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repo       = Split-Path $PSScriptRoot -Parent
$project    = Join-Path $repo 'src/VocalCoach.App/VocalCoach.App.csproj'
$publishDir = Join-Path $repo 'publish'
$zip        = Join-Path $repo 'VocalCoach-win-x64.zip'
$metadata   = Join-Path $PSScriptRoot 'signing/metadata.json'
$toolsDir   = Join-Path $env:LOCALAPPDATA 'VocalCoach/signing-tools'

# Pinned versions. Artifact Signing needs SignTool 10.0.22621.755 or later.
$clientVersion     = '1.0.146'
$buildToolsVersion = '10.0.22621.756'
$timestampUrl      = 'http://timestamp.acs.microsoft.com'

# The files we build and therefore sign. Third-party DLLs keep their publishers' signatures.
$filesToSign = 'VocalCoach.App.exe', 'VocalCoach.App.dll', 'VocalCoach.Core.dll'

function Get-NuGetPackage([string]$id, [string]$version) {
    $dest = Join-Path $toolsDir "$id.$version"
    if (-not (Test-Path $dest)) {
        Write-Host "Downloading $id $version..."
        New-Item -ItemType Directory -Force $toolsDir | Out-Null
        $pkg = Join-Path $toolsDir "$id.$version.zip"
        Invoke-WebRequest "https://www.nuget.org/api/v2/package/$id/$version" -OutFile $pkg
        Expand-Archive $pkg $dest -Force
        Remove-Item $pkg
    }
    $dest
}

function Get-SignTool {
    $cached = Join-Path $env:USERPROFILE ".nuget/packages/microsoft.windows.sdk.buildtools/$buildToolsVersion"
    $root = if (Test-Path $cached) { $cached } else { Get-NuGetPackage 'Microsoft.Windows.SDK.BuildTools' $buildToolsVersion }
    $tool = Get-ChildItem $root -Recurse -Filter signtool.exe | Where-Object FullName -match '\\x64\\' | Select-Object -First 1
    if (-not $tool) { throw "signtool.exe (x64) not found under $root" }
    $tool.FullName
}

function Invoke-Checked([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$(Split-Path $exe -Leaf) failed with exit code $LASTEXITCODE" }
}

# 1. Publish (same command as the release workflow).
if (-not $SkipPublish) {
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    Invoke-Checked 'dotnet' @(
        'publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
        '-f', 'net10.0-windows10.0.19041.0', '-p:WindowsOnly=true', '-p:WindowsPackageType=None',
        '-p:WindowsAppSDKSelfContained=true', '-o', $publishDir)
}

$targets = foreach ($name in $filesToSign) {
    $path = Join-Path $publishDir $name
    if (-not (Test-Path $path)) { throw "Expected $path in the publish output." }
    $path
}

# 2. Sign and verify.
if (-not $NoSign) {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        throw 'Azure CLI not found. Install it (winget install -e --id Microsoft.AzureCLI), then run: az login --tenant johnapollonioutlook.onmicrosoft.com'
    }
    az account show --only-show-errors *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Not signed in to Azure. Run: az login --tenant johnapollonioutlook.onmicrosoft.com'
    }

    $signTool = Get-SignTool
    $dlib = Join-Path (Get-NuGetPackage 'Microsoft.ArtifactSigning.Client' $clientVersion) 'bin/x64/Azure.CodeSigning.Dlib.dll'
    if (-not (Test-Path $dlib)) { throw "Signing client not found at $dlib" }

    Write-Host "Signing $($targets.Count) files with $signTool"
    Invoke-Checked $signTool (@('sign', '/v', '/fd', 'SHA256', '/tr', $timestampUrl, '/td', 'SHA256',
        '/dlib', $dlib, '/dmdf', $metadata) + $targets)
    Invoke-Checked $signTool (@('verify', '/pa', '/q') + $targets)
    Write-Host 'Signatures verified.'
}

# 3. Zip and checksum (same layout as the release workflow).
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zip
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
"$hash  $(Split-Path $zip -Leaf)" | Out-File "$zip.sha256" -Encoding ascii -NoNewline
Get-Item $zip | Select-Object Name, Length
