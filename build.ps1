$ErrorActionPreference = "Stop"

if (-not $env:VINTAGE_STORY) {
    $env:VINTAGE_STORY = Join-Path $env:APPDATA "Vintagestory"
}

$root = $PSScriptRoot
$project = Join-Path $root "calendarhandbook\calendarhandbook.csproj"
$configuration = if ($args.Count -gt 0) { $args[0] } else { "Release" }

dotnet build $project -c $configuration
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$modinfo = Get-Content (Join-Path $root "calendarhandbook\modinfo.json") -Raw | ConvertFrom-Json
$version = $modinfo.version
$modid = $modinfo.modid
if (-not $modid) { $modid = $modinfo.modId }
$builtMod = Join-Path $root "calendarhandbook\bin\$configuration\Mods\$modid"
$releases = Join-Path $root "Releases"
New-Item -ItemType Directory -Force -Path $releases | Out-Null

$zipName = "${modid}_$version.zip"
$zipPath = Join-Path $releases $zipName
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $builtMod -Recurse -File |
        Where-Object { $_.Name -notlike "*.deps.json" } |
        ForEach-Object {
            $relative = $_.FullName.Substring($builtMod.Length).TrimStart('\', '/').Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $_.FullName,
                $relative,
                [System.IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
}
finally {
    $archive.Dispose()
}

$modsDir = Join-Path $env:APPDATA "VintagestoryData\Mods"
New-Item -ItemType Directory -Force -Path $modsDir | Out-Null
Copy-Item $zipPath (Join-Path $modsDir $zipName) -Force

$looseFolder = Join-Path $modsDir $modid
if (Test-Path $looseFolder) {
    try {
        Remove-Item $looseFolder -Recurse -Force
        Write-Host "Removed leftover loose Mods\$modid folder"
    }
    catch {
        Write-Host "Close Vintage Story, then delete Mods\$modid. Use the zip instead: $zipName"
    }
}

Write-Host "Packaged $zipPath"
Write-Host "Copied to $(Join-Path $modsDir $zipName)"
