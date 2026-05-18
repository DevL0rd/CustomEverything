param(
    [switch]$Publish,
    [switch]$SkipBuild,
    [string]$ExpectedVersion
)

$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$envPath = Join-Path $Root ".env"

function Import-DotEnv {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) {
            continue
        }

        $parts = $trimmed.Split("=", 2)
        if ($parts.Length -ne 2) {
            continue
        }

        $name = $parts[0].Trim()
        $value = $parts[1].Trim().Trim('"').Trim("'")
        if ($name.Length -gt 0 -and [string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name))) {
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

Import-DotEnv $envPath

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

function Test-ThunderstoreVersionExists {
    param(
        [Parameter(Mandatory)][string]$Namespace,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Version
    )

    $uri = "https://thunderstore.io/api/experimental/package/$Namespace/$Name/$Version/"
    try {
        Invoke-RestMethod -Uri $uri -Headers @{ Accept = "application/json" } | Out-Null
        return $true
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 404) {
            return $false
        }

        throw "Could not check Thunderstore package version $Namespace-$Name-$Version`: $($_.Exception.Message)"
    }
}

function Invoke-TcliPublish {
    param(
        [Parameter(Mandatory)][string]$ZipPath,
        [Parameter(Mandatory)][string]$Namespace,
        [Parameter(Mandatory)][string]$PackageName,
        [Parameter(Mandatory)][string]$PackageVersion
    )

    if (-not (Test-Path -LiteralPath $ZipPath)) {
        throw "Package zip not found: $ZipPath"
    }

    $token = $env:TCLI_AUTH_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "TCLI_AUTH_TOKEN is empty. Add it as a repository secret, or attach the GitHub environment that contains it to this workflow job."
    }

    $output = @(dotnet tcli publish --file $ZipPath --token $token --package-namespace $Namespace --package-name $PackageName --package-version $PackageVersion 2>&1)
    $output | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        $joinedOutput = $output -join "`n"
        if ($joinedOutput -match "Package of the same namespace, name and version already exists") {
            Write-Host "Skipping publish because this exact package version already exists on Thunderstore."
            return
        }

        throw "Thunderstore publish failed: $ZipPath"
    }
}

& (Join-Path $PSScriptRoot "sync-version.ps1") -Root $Root

$version = (Get-Content -Raw -LiteralPath (Join-Path $Root "VERSION")).Trim()
$toml = Get-Content -Raw -LiteralPath (Join-Path $Root "thunderstore.toml")
$namespace = Get-TomlString $toml "namespace"
$packageName = Get-TomlString $toml "name"

if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    $normalizedExpected = $ExpectedVersion.Trim() -replace '^v', ''
    if ($version -ne $normalizedExpected) {
        throw "VERSION ($version) does not match expected release version ($normalizedExpected)"
    }
}

Push-Location $Root
try {
    dotnet tool restore
    if (-not $SkipBuild) {
        & (Join-Path $PSScriptRoot "build.ps1") -Configuration Release -NoDeploy
    }

    $zipPath = Join-Path $Root "build\$namespace-$packageName-$version.zip"
    if ($Publish -and (Test-ThunderstoreVersionExists -Namespace $namespace -Name $packageName -Version $version)) {
        Write-Host "Skipping $namespace-$packageName-$version; version already exists on Thunderstore."
        return
    }

    & (Join-Path $PSScriptRoot "package.ps1") -Configuration Release
    if (-not (Test-Path -LiteralPath $zipPath)) {
        throw "Expected package zip was not created: $zipPath"
    }

    if ($Publish) {
        Invoke-TcliPublish -ZipPath $zipPath -Namespace $namespace -PackageName $packageName -PackageVersion $version
    }
    else {
        Write-Host "Built Thunderstore package: $zipPath"
    }
}
finally {
    Pop-Location
}
