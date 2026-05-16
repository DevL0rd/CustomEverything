param(
    [Alias("r")]
    [switch]$Restart,

    [Alias("d")]
    [switch]$Desktop,

    [switch]$NoDeploy,

    [string]$ProfileName = "Default",

    [string]$ProfilePath,

    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnetBuildArgs = @()

function Resolve-LocalProfilePath {
    param(
        [string]$RequestedProfilePath,
        [string]$RequestedProfileName,
        [bool]$RequireNamedProfile
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedProfilePath)) {
        return (Resolve-Path -LiteralPath $RequestedProfilePath -ErrorAction Stop).Path
    }

    if ([string]::IsNullOrWhiteSpace($env:APPDATA)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($RequestedProfileName)) {
        $RequestedProfileName = "Default"
    }

    $galeProfile = Join-Path $env:APPDATA (Join-Path "com.kesomannen.gale\resonite\profiles" $RequestedProfileName)
    if (Test-Path -LiteralPath (Join-Path $galeProfile "BepInEx\plugins")) {
        return $galeProfile
    }

    if ($RequireNamedProfile) {
        throw "Gale Resonite profile '$RequestedProfileName' was not found or does not contain BepInEx\plugins: $galeProfile"
    }

    return $null
}

function Get-CustomEverythingOutput {
    param([Parameter(Mandatory)][string]$ConfigurationName)

    $output = Get-ChildItem -LiteralPath (Join-Path $Root "CustomEverything\bin\$ConfigurationName") -Directory -Filter "net10.0-windows*" -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "CustomEverything.dll") } |
        Select-Object -First 1

    if ($null -eq $output) {
        throw "CustomEverything.dll not found under CustomEverything\bin\$ConfigurationName\net10.0-windows*."
    }

    return $output.FullName
}

function Copy-CustomEverythingProfileDeploy {
    param(
        [Parameter(Mandatory)][string]$ResolvedProfilePath,
        [Parameter(Mandatory)][string]$ConfigurationName
    )

    $modOutDir = Get-CustomEverythingOutput -ConfigurationName $ConfigurationName
    $pluginDir = Join-Path $ResolvedProfilePath "BepInEx\plugins\CustomEverything"
    $pluginDll = Join-Path $modOutDir "CustomEverything.dll"
    if (-not (Test-Path -LiteralPath $pluginDll)) {
        throw "Required deploy input not found: $pluginDll"
    }

    Write-Host "Deploying CustomEverything to BepInEx profile: $ResolvedProfilePath"
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    Copy-Item -LiteralPath $pluginDll -Destination (Join-Path $pluginDir "CustomEverything.dll") -Force
}

function Stop-ProcessTreeByName {
    param([Parameter(Mandatory)][string]$Name)

    $running = @(Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($Name)) -ErrorAction SilentlyContinue)
    if ($running.Count -eq 0) {
        return
    }

    Write-Host "Stopping $Name..."
    foreach ($process in $running) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

function Wait-ProcessesStopped {
    param([Parameter(Mandatory)][string[]]$Names)

    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        $running = @()
        foreach ($name in $Names) {
            $processName = [IO.Path]::GetFileNameWithoutExtension($name)
            $running += @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
        }

        if ($running.Count -eq 0) {
            return
        }

        foreach ($name in $Names) {
            Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($name)) -ErrorAction SilentlyContinue |
                Stop-Process -Force -ErrorAction SilentlyContinue
        }
        Start-Sleep -Milliseconds 500
    }

    throw "FAILED TO STOP RESONITE - not building or restarting"
}

function Start-Resonite {
    param(
        [switch]$DesktopMode,
        [string]$ResolvedProfilePath
    )

    $steamExe = $null
    $steamKey = Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue
    if ($steamKey -and -not [string]::IsNullOrWhiteSpace($steamKey.SteamExe) -and (Test-Path -LiteralPath $steamKey.SteamExe)) {
        $steamExe = $steamKey.SteamExe
    }
    elseif (Test-Path -LiteralPath "C:\Program Files (x86)\Steam\steam.exe") {
        $steamExe = "C:\Program Files (x86)\Steam\steam.exe"
    }

    if ([string]::IsNullOrWhiteSpace($steamExe)) {
        throw "Steam executable was not found."
    }

    $launchArgs = @("-applaunch", "2519830", "--hookfxr-enable")
    if (-not [string]::IsNullOrWhiteSpace($ResolvedProfilePath)) {
        $launchArgs += @(
            "--bepinex-target",
            (Join-Path $ResolvedProfilePath "BepInEx"),
            "--doorstop-enabled",
            "true",
            "--doorstop-target-assembly",
            (Join-Path $ResolvedProfilePath "Renderer\BepInEx\core\BepInEx.Preloader.dll")
        )
    }
    if ($DesktopMode) {
        $launchArgs += "-Screen"
    }

    Start-Process -FilePath $steamExe -ArgumentList $launchArgs
}

$localProfilePath = $null
if (-not $NoDeploy -and $env:GITHUB_ACTIONS -ne "true") {
    $localProfilePath = Resolve-LocalProfilePath `
        -RequestedProfilePath $ProfilePath `
        -RequestedProfileName $ProfileName `
        -RequireNamedProfile $PSBoundParameters.ContainsKey("ProfileName")
}

if ($Restart) {
    Stop-ProcessTreeByName "Resonite.exe"
    Stop-ProcessTreeByName "Renderite.Host.exe"
    Stop-ProcessTreeByName "Renderite.Renderer.exe"
    Wait-ProcessesStopped @("Resonite.exe", "Renderite.Host.exe", "Renderite.Renderer.exe")
    Start-Sleep -Seconds 2
}

Push-Location $Root
try {
    dotnet build (Join-Path $Root "CustomEverything\CustomEverything.csproj") -c $Configuration @dotnetBuildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "CUSTOM EVERYTHING BUILD FAILED - not launching Resonite"
    }

    if (-not [string]::IsNullOrWhiteSpace($localProfilePath)) {
        Copy-CustomEverythingProfileDeploy -ResolvedProfilePath $localProfilePath -ConfigurationName $Configuration
    }
}
finally {
    Pop-Location
}

if ($Restart) {
    if ($Desktop) {
        Write-Host "Starting Resonite in desktop mode..."
        Start-Resonite -DesktopMode -ResolvedProfilePath $localProfilePath
    }
    else {
        Start-Resonite -ResolvedProfilePath $localProfilePath
    }
}
