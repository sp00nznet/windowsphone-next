<#
.SYNOPSIS
    Builds all Windows Phone Next applications
.DESCRIPTION
    Compiles all 19 applications in Release mode and copies them to the Output folder
.EXAMPLE
    .\build-all.ps1
    .\build-all.ps1 -Configuration Debug
#>

param(
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter()]
    [string]$OutputPath = "$PSScriptRoot\..\Output",

    [Parameter()]
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$script:BuildErrors = @()
$script:BuildSuccesses = @()

# Color output helpers
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }

# Banner
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Windows Phone Next - Build System" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# Check for dotnet
Write-Info "Checking for .NET SDK..."
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error "ERROR: .NET SDK not found. Please install .NET 8.0 SDK"
    exit 1
}
Write-Success "Found .NET SDK version: $dotnetVersion"

# Project root
$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$AppsFolder = Join-Path $ProjectRoot "Apps"

# All projects to build (in dependency order)
$SharedProjects = @(
    "Shared\ModemLib\ModemLib.csproj",
    "Shared\BlockingService\BlockingService.csproj",
    "Shared\Services\SharedServices.csproj"
)

$AppProjects = @(
    "Launcher\WindowsPhoneLauncher.csproj",
    "Dialer\WindowsPhoneDialer.csproj",
    "Messaging\WindowsPhoneMessaging.csproj",
    "Contacts\WindowsPhoneContacts.csproj",
    "Browser\WindowsPhoneBrowser.csproj",
    "Gmail\WindowsPhoneGmail.csproj",
    "Maps\WindowsPhoneMaps.csproj",
    "Music\Launcher\WindowsPhoneMusic.csproj",
    "Video\WindowsPhoneVideo.csproj",
    "Calendar\WindowsPhoneCalendar.csproj",
    "Gallery\WindowsPhoneGallery.csproj",
    "Settings\WindowsPhoneSettings.csproj",
    "Terminal\WindowsPhoneTerminal.csproj",
    "ClaudeCode\WindowsPhoneClaudeCode.csproj",
    "AndroidApps\WindowsPhoneAndroidApps.csproj",
    "Camera\WindowsPhoneCamera.csproj",
    "Files\WindowsPhoneFiles.csproj",
    "Solitaire\WindowsPhoneSolitaire.csproj",
    "Mahjong\WindowsPhoneMahjong.csproj"
)

# Create output directory
$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}
Write-Info "Output directory: $OutputPath"

# Clean if requested
if ($Clean) {
    Write-Warning "Cleaning previous builds..."
    Remove-Item -Path "$OutputPath\*" -Recurse -Force -ErrorAction SilentlyContinue

    # Clean each project
    Get-ChildItem -Path $AppsFolder -Recurse -Directory -Include "bin", "obj" |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }

    Write-Success "Clean complete"
}

# Restore packages
Write-Host ""
Write-Info "Restoring NuGet packages..."
$RestoreStartTime = Get-Date

# Restore all projects
foreach ($proj in ($SharedProjects + $AppProjects)) {
    $projPath = Join-Path $AppsFolder $proj
    if (Test-Path $projPath) {
        dotnet restore $projPath --verbosity quiet 2>$null
    }
}

$RestoreTime = (Get-Date) - $RestoreStartTime
Write-Success "Package restore complete ($('{0:N1}' -f $RestoreTime.TotalSeconds)s)"

# Build function
function Build-Project {
    param(
        [string]$ProjectPath,
        [string]$ProjectName,
        [string]$Configuration,
        [string]$OutputPath
    )

    $startTime = Get-Date

    Write-Host "  Building $ProjectName... " -NoNewline

    $outputDir = Join-Path $OutputPath $ProjectName

    try {
        $result = dotnet build $ProjectPath `
            --configuration $Configuration `
            --output $outputDir `
            --no-restore `
            --verbosity quiet `
            2>&1

        if ($LASTEXITCODE -eq 0) {
            $buildTime = (Get-Date) - $startTime
            Write-Success "OK ($('{0:N1}' -f $buildTime.TotalSeconds)s)"
            return $true
        } else {
            Write-Error "FAILED"
            $script:BuildErrors += @{
                Project = $ProjectName
                Error = ($result | Out-String)
            }
            return $false
        }
    }
    catch {
        Write-Error "FAILED"
        $script:BuildErrors += @{
            Project = $ProjectName
            Error = $_.Exception.Message
        }
        return $false
    }
}

# Build shared libraries first
Write-Host ""
Write-Info "Building shared libraries..."
$SharedBuildTime = Get-Date

foreach ($proj in $SharedProjects) {
    $projPath = Join-Path $AppsFolder $proj
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj)

    if (Test-Path $projPath) {
        $success = Build-Project -ProjectPath $projPath -ProjectName $projName -Configuration $Configuration -OutputPath "$OutputPath\_Shared"
        if ($success) {
            $script:BuildSuccesses += $projName
        }
    } else {
        Write-Warning "  Skipping $projName (not found)"
    }
}

Write-Success "Shared libraries built in $('{0:N1}' -f ((Get-Date) - $SharedBuildTime).TotalSeconds)s"

# Build applications
Write-Host ""
Write-Info "Building applications..."
$AppBuildTime = Get-Date

foreach ($proj in $AppProjects) {
    $projPath = Join-Path $AppsFolder $proj
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj)

    if (Test-Path $projPath) {
        $success = Build-Project -ProjectPath $projPath -ProjectName $projName -Configuration $Configuration -OutputPath $OutputPath
        if ($success) {
            $script:BuildSuccesses += $projName
        }
    } else {
        Write-Warning "  Skipping $projName (not found)"
    }
}

$TotalAppBuildTime = (Get-Date) - $AppBuildTime
Write-Success "Applications built in $('{0:N1}' -f $TotalAppBuildTime.TotalSeconds)s"

# Create apps manifest
Write-Host ""
Write-Info "Creating apps manifest..."
$manifest = @{
    BuildDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Configuration = $Configuration
    DotNetVersion = $dotnetVersion
    Applications = @()
}

foreach ($success in $script:BuildSuccesses) {
    if ($success -notin @("ModemLib", "BlockingService", "SharedServices")) {
        $appDir = Join-Path $OutputPath $success
        if (Test-Path $appDir) {
            $exeFile = Get-ChildItem -Path $appDir -Filter "*.exe" | Select-Object -First 1
            if ($exeFile) {
                $manifest.Applications += @{
                    Name = $success
                    Executable = $exeFile.Name
                    Path = $success
                }
            }
        }
    }
}

$manifestPath = Join-Path $OutputPath "apps-manifest.json"
$manifest | ConvertTo-Json -Depth 3 | Set-Content -Path $manifestPath -Encoding UTF8
Write-Success "Manifest created: $manifestPath"

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Build Summary" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

$successCount = $script:BuildSuccesses.Count
$failCount = $script:BuildErrors.Count
$totalCount = $successCount + $failCount

Write-Host "  Total projects: $totalCount"
Write-Success "  Successful:     $successCount"
if ($failCount -gt 0) {
    Write-Error "  Failed:         $failCount"
}

# Show errors if any
if ($script:BuildErrors.Count -gt 0) {
    Write-Host ""
    Write-Error "Build Errors:"
    foreach ($err in $script:BuildErrors) {
        Write-Host ""
        Write-Error "  $($err.Project):"
        Write-Host "    $($err.Error)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Info "Output location: $OutputPath"
Write-Host ""

# Exit with error code if any builds failed
if ($script:BuildErrors.Count -gt 0) {
    exit 1
}

exit 0
