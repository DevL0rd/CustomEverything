param(
    [string]$Configuration = "Release",
    [string]$ZipName = $env:ZIP_NAME,

    [ValidateSet("Manual", "Main", "All")]
    [string]$Package,

    [Alias("Thunderstore")]
    [switch]$ThunderstoreFormat
)

$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
& (Join-Path $PSScriptRoot "sync-version.ps1") -Root $Root

if ([string]::IsNullOrWhiteSpace($Package)) {
    $Package = if ($ThunderstoreFormat) { "Main" } else { "Manual" }
}

$version = (Get-Content -Raw -LiteralPath (Join-Path $Root "VERSION")).Trim()
$toml = Get-Content -Raw -LiteralPath (Join-Path $Root "thunderstore.toml")

function Assert-SemVer {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Value
    )

    if ($Value -notmatch '^\d+\.\d+\.\d+$') {
        throw "$Name must be semantic Major.Minor.Patch without suffix. Current value: '$Value'"
    }
}

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

    return $dependencies.ToArray()
}

function Get-CustomEverythingOutput {
    param([Parameter(Mandatory)][string]$ConfigurationName)

    $output = Get-ChildItem -LiteralPath (Join-Path $Root "CustomEverything\bin\$ConfigurationName") -Directory -Filter "net10.0-windows*" -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "CustomEverything.dll") } |
        Select-Object -First 1

    if ($null -eq $output) {
        throw "CustomEverything.dll not found under CustomEverything\bin\$ConfigurationName\net10.0-windows*. Run scripts\build.ps1 first."
    }

    return $output.FullName
}

function New-ZipFromStage {
    param(
        [Parameter(Mandatory)][string]$Stage,
        [Parameter(Mandatory)][string]$OutZip
    )

    if (Test-Path -LiteralPath $OutZip) {
        Remove-Item -LiteralPath $OutZip -Force
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::Open($OutZip, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $stageRoot = $Stage.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        foreach ($file in Get-ChildItem -LiteralPath $Stage -Recurse -File) {
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
}

function New-PackageStage {
    param([Parameter(Mandatory)][string]$Name)

    $stage = Join-Path $env:TEMP "CustomEverythingPackage\$Name"
    if (Test-Path -LiteralPath $stage) {
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    return $stage
}

function Add-PackageMetadata {
    param(
        [Parameter(Mandatory)][string]$Stage,
        [Parameter(Mandatory)][string]$PackageName,
        [Parameter(Mandatory)][string]$PackageVersion,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$Dependencies
    )

    Copy-Item -LiteralPath (Join-Path $Root "README_THUNDERSTORE.md") -Destination (Join-Path $Stage "README.md")
    Copy-Item -LiteralPath (Join-Path $Root "icon.png") -Destination (Join-Path $Stage "icon.png")
    Copy-Item -LiteralPath (Join-Path $Root "CHANGELOG.md") -Destination (Join-Path $Stage "CHANGELOG.md")

    $manifest = [ordered]@{
        name = $PackageName
        version_number = $PackageVersion
        website_url = Get-TomlString $toml "websiteUrl"
        description = $Description
        dependencies = $Dependencies
    }
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -NoNewline -LiteralPath (Join-Path $Stage "manifest.json")
}

function Build-ManualPackage {
    $modOutDir = Get-CustomEverythingOutput -ConfigurationName $Configuration
    $namespace = Get-TomlString $toml "namespace"
    $mainName = Get-TomlString $toml "name"
    $description = Get-TomlString $toml "description"
    $dependencies = Get-TomlDependencies $toml
    $pluginDll = Join-Path $modOutDir "CustomEverything.dll"

    foreach ($path in @($pluginDll, (Join-Path $Root "README_THUNDERSTORE.md"), (Join-Path $Root "icon.png"), (Join-Path $Root "CHANGELOG.md"))) {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required package input not found: $path"
        }
    }

    $name = if ([string]::IsNullOrWhiteSpace($ZipName)) { "CustomEverything-$version" } else { $ZipName }
    $stage = New-PackageStage -Name $name
    $outZip = Join-Path $Root "$name.zip"
    $packageRoot = Join-Path $stage "BepInEx\plugins\$namespace-$mainName"
    $pluginDir = Join-Path $packageRoot "CustomEverything"

    New-Item -ItemType Directory -Force -Path $packageRoot, $pluginDir | Out-Null
    Add-PackageMetadata -Stage $packageRoot -PackageName $mainName -PackageVersion $version -Description $description -Dependencies $dependencies
    Copy-Item -LiteralPath $pluginDll -Destination (Join-Path $pluginDir "CustomEverything.dll")

    New-ZipFromStage -Stage $stage -OutZip $outZip
    Remove-Item -LiteralPath $stage -Recurse -Force
    Write-Host "Done: $outZip (manual profile-root package layout)"
}

function Build-MainThunderstorePackage {
    $modOutDir = Get-CustomEverythingOutput -ConfigurationName $Configuration
    $namespace = Get-TomlString $toml "namespace"
    $mainName = Get-TomlString $toml "name"
    $description = Get-TomlString $toml "description"
    $dependencies = Get-TomlDependencies $toml
    $pluginDll = Join-Path $modOutDir "CustomEverything.dll"

    foreach ($path in @($pluginDll, (Join-Path $Root "README_THUNDERSTORE.md"), (Join-Path $Root "icon.png"), (Join-Path $Root "CHANGELOG.md"))) {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Required package input not found: $path"
        }
    }

    $name = if ([string]::IsNullOrWhiteSpace($ZipName)) { "$namespace-$mainName-$version" } else { $ZipName }
    $stage = New-PackageStage -Name $name
    $outDir = Join-Path $Root "build"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $outZip = Join-Path $outDir "$name.zip"
    $pluginDir = Join-Path $stage "plugins\CustomEverything"

    Add-PackageMetadata -Stage $stage -PackageName $mainName -PackageVersion $version -Description $description -Dependencies $dependencies
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    Copy-Item -LiteralPath $pluginDll -Destination (Join-Path $pluginDir "CustomEverything.dll")

    New-ZipFromStage -Stage $stage -OutZip $outZip
    Remove-Item -LiteralPath $stage -Recurse -Force
    Write-Host "Done: $outZip (Thunderstore main package)"
}

Assert-SemVer -Name "VERSION" -Value $version

$tomlVersion = Get-TomlString $toml "versionNumber"
if ($tomlVersion -ne $version) {
    throw "VERSION ($version) does not match thunderstore.toml versionNumber ($tomlVersion). Run scripts\sync-version.ps1."
}

switch ($Package) {
    "Manual" { Build-ManualPackage }
    "Main" { Build-MainThunderstorePackage }
    "All" {
        Build-ManualPackage
        Build-MainThunderstorePackage
    }
}
