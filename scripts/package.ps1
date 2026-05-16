param(
    [string]$Configuration = "Release",
    [string]$ZipName = $env:ZIP_NAME
)

$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$version = (Get-Content -Raw -LiteralPath (Join-Path $Root "VERSION")).Trim()
$tomlPath = Join-Path $Root "thunderstore.toml"
$toml = Get-Content -Raw -LiteralPath $tomlPath

function Get-TomlString {
    param(
        [Parameter(Mandatory)][string]$Content,
        [Parameter(Mandatory)][string]$Name
    )

    $match = [regex]::Match($Content, "(?m)^\s*$([regex]::Escape($Name))\s*=\s*`"([^`"]*)`"\s*$")
    if (-not $match.Success) {
        throw "$Name was not found in thunderstore.toml"
    }
    return $match.Groups[1].Value
}

function Get-TomlDependencies {
    param([Parameter(Mandatory)][string]$Content)

    $match = [regex]::Match($Content, '(?ms)^\[package\.dependencies\]\s*(.*?)(?=^\[|\z)')
    if (-not $match.Success) {
        throw "[package.dependencies] was not found in thunderstore.toml"
    }

    $dependencies = New-Object System.Collections.Generic.List[string]
    foreach ($line in ($match.Groups[1].Value -split "`r?`n")) {
        $dependency = [regex]::Match($line, '^\s*([A-Za-z0-9_.-]+)\s*=\s*"([^"]+)"\s*$')
        if ($dependency.Success) {
            $dependencies.Add("$($dependency.Groups[1].Value)-$($dependency.Groups[2].Value)")
        }
    }

    if ($dependencies.Count -eq 0) {
        throw "No package dependencies were found in thunderstore.toml"
    }

    return $dependencies.ToArray()
}

$packageName = Get-TomlString $toml "name"
$websiteUrl = Get-TomlString $toml "websiteUrl"
$description = Get-TomlString $toml "description"
$tomlVersion = Get-TomlString $toml "versionNumber"
$dependencies = Get-TomlDependencies $toml
if ($tomlVersion -ne $version) {
    throw "VERSION ($version) does not match thunderstore.toml versionNumber ($tomlVersion). Run scripts\sync-version.ps1."
}

$modOutDir = Get-ChildItem -LiteralPath (Join-Path $Root "CustomEverything\bin\$Configuration") -Directory -Filter "net10.0-windows*" -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "CustomEverything.dll") } |
    Select-Object -First 1

if ($null -eq $modOutDir) {
    throw "CustomEverything.dll not found under CustomEverything\bin\$Configuration\net10.0-windows*. Run scripts\build.ps1 first."
}

$modDll = Join-Path $modOutDir.FullName "CustomEverything.dll"
if ([string]::IsNullOrWhiteSpace($ZipName)) {
    $short = (& git -C $Root rev-parse --short HEAD 2>$null)
    if ([string]::IsNullOrWhiteSpace($short)) {
        $short = "unknown"
    }

    $timestamp = Get-Date -Format "yyyy.MM.dd_HH.mm.ss"
    $ZipName = "CustomEverything-Alpha-${timestamp}_$short"
}

$stage = Join-Path $env:TEMP "CustomEverythingPackage\$ZipName"
$outZip = Join-Path $Root "$ZipName.zip"
$readmeSource = Join-Path $Root "README_THUNDERSTORE.md"
$changelogSource = Join-Path $Root "CHANGELOG.md"

foreach ($path in @($modDll, $readmeSource, $changelogSource)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required package input not found: $path"
    }
}

Write-Host "Building zip layout in: $stage"
Write-Host "Using CustomEverything build output: $($modOutDir.FullName)"

if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stage | Out-Null

$pluginDir = Join-Path $stage "plugins\CustomEverything"
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null

Copy-Item -LiteralPath $modDll -Destination (Join-Path $pluginDir "CustomEverything.dll")
Copy-Item -LiteralPath $readmeSource -Destination (Join-Path $stage "README.md")
Copy-Item -LiteralPath $changelogSource -Destination (Join-Path $stage "CHANGELOG.md")

$manifest = [ordered]@{
    name = $packageName
    version_number = $version
    website_url = $websiteUrl
    description = $description
    dependencies = $dependencies
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -NoNewline -LiteralPath (Join-Path $stage "manifest.json")

if (Test-Path -LiteralPath $outZip) {
    Remove-Item -LiteralPath $outZip -Force
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($outZip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $stageRoot = $stage.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File) {
        $entryName = $file.FullName.Substring($stageRoot.Length).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive,
            $file.FullName,
            $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $archive.Dispose()
}
Remove-Item -LiteralPath $stage -Recurse -Force

Write-Host ""
Write-Host "Done:"
Write-Host "  $ZipName.zip (Thunderstore package layout)"
