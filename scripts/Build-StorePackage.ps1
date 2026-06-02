param(
    [string]$Version
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot "BiliRadar"
$projectFile = Join-Path $projectDir "BiliRadar.csproj"
$manifestPath = Join-Path $projectDir "Package.appxmanifest"
$artifactsRoot = Join-Path $projectDir "artifacts"

$originalManifestBytes = [System.IO.File]::ReadAllBytes($manifestPath)
$originalManifestContent = Get-Content -Raw -LiteralPath $manifestPath
[xml]$manifest = $originalManifestContent
$manifestNamespace = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$manifestNamespace.AddNamespace("appx", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
$identity = $manifest.SelectSingleNode("/appx:Package/appx:Identity", $manifestNamespace)

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $identity.Version
}

$parsedVersion = [System.Version]::Parse($Version)
if ($parsedVersion.Build -lt 0 -or $parsedVersion.Revision -lt 0) {
    throw "Store package version must contain four components, for example 1.0.1.0."
}

if ($parsedVersion.Revision -ne 0) {
    throw "Microsoft Store reserves the fourth version component. Use a version ending in .0, for example 1.0.1.0."
}

$manifestVersionUpdated = $identity.Version -ne $Version
if ($manifestVersionUpdated) {
    $identity.Version = $Version
    $manifest.Save($manifestPath)
}

$stagingRoot = Join-Path $artifactsRoot "store-upload-$Version"
$submissionRoot = Join-Path $artifactsRoot "StoreSubmission-$Version"
$bundleInput = Join-Path $submissionRoot "bundle-input"

function Remove-StagingDirectory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $resolvedArtifacts = [System.IO.Path]::GetFullPath($artifactsRoot).TrimEnd('\') + '\'
    $resolvedTarget = [System.IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
    if (-not $resolvedTarget.StartsWith($resolvedArtifacts, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a directory outside the project artifacts folder: $resolvedTarget"
    }

    Remove-Item -Recurse -Force -LiteralPath $Path
}

Remove-StagingDirectory $stagingRoot
Remove-StagingDirectory $submissionRoot
New-Item -ItemType Directory -Force -Path $bundleInput | Out-Null

$buildProperties = @(
    "-c", "Release",
    "-t:Rebuild",
    "-p:Version=$Version",
    "-p:UseProductionPackageIdentity=true",
    "-p:AppxBundle=Never",
    "-p:GenerateAppxPackageOnBuild=true",
    "-p:AppxPackageSigningEnabled=false",
    "-p:GenerateTemporaryStoreCertificate=false",
    "-p:AppxAutoIncrementPackageRevision=false"
)

Push-Location $projectDir
try {
    foreach ($platform in @("x64", "ARM64")) {
        $platformOutput = "artifacts/store-upload-$Version/$($platform.ToLowerInvariant())/"
        dotnet build $projectFile @buildProperties "-p:Platform=$platform" "-p:AppxPackageDir=$platformOutput"
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed for $platform."
        }
    }
}
finally {
    Pop-Location
    if ($manifestVersionUpdated) {
        [System.IO.File]::WriteAllBytes($manifestPath, $originalManifestBytes)
    }
}

$x64Msix = Get-ChildItem -LiteralPath (Join-Path $stagingRoot "x64") -Recurse -Filter "*.msix" |
    Select-Object -First 1
$arm64Msix = Get-ChildItem -LiteralPath (Join-Path $stagingRoot "arm64") -Recurse -Filter "*.msix" |
    Select-Object -First 1

if (-not $x64Msix -or -not $arm64Msix) {
    throw "The expected x64 and ARM64 MSIX packages were not generated."
}

Copy-Item -LiteralPath $x64Msix.FullName -Destination (Join-Path $bundleInput "BiliRadar_${Version}_x64.msix")
Copy-Item -LiteralPath $arm64Msix.FullName -Destination (Join-Path $bundleInput "BiliRadar_${Version}_arm64.msix")

$windowsKitsBin = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
$makeAppx = Get-ChildItem -Path $windowsKitsBin -Recurse -Filter "makeappx.exe" |
    Where-Object { $_.FullName -match '\\x64\\makeappx\.exe$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if (-not $makeAppx) {
    throw "makeappx.exe was not found under $windowsKitsBin."
}

$bundlePath = Join-Path $submissionRoot "BiliRadar_${Version}_x64_arm64.msixbundle"
& $makeAppx.FullName bundle /d $bundleInput /p $bundlePath /bv $Version /o
if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx failed to create the Store bundle."
}

Remove-StagingDirectory $stagingRoot

Write-Host ""
Write-Host "Store bundle created:"
Write-Host $bundlePath
Get-FileHash -Algorithm SHA256 -LiteralPath $bundlePath
