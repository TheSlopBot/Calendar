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
$builtMod = Join-Path $root "calendarhandbook\bin\$configuration\Mods\mod"
$releases = Join-Path $root "Releases"
New-Item -ItemType Directory -Force -Path $releases | Out-Null

$zipName = "calendarhandbook_$version.zip"
$zipPath = Join-Path $releases $zipName
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $builtMod -Recurse -File | ForEach-Object {
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

$looseFolder = Join-Path $modsDir "calendarhandbook"
if (Test-Path $looseFolder) {
    try {
        Remove-Item $looseFolder -Recurse -Force
        Write-Host "Removed leftover loose Mods\calendarhandbook folder"
    }
    catch {
        Write-Host "Close Vintage Story, then delete Mods\calendarhandbook. Use the zip instead: $zipName"
    }
}

Write-Host "Packaged $zipPath"
Write-Host "Copied to $(Join-Path $modsDir $zipName)"
