<#
.SYNOPSIS
    Downloads Windows 11 IoT Enterprise LTSC evaluation ISO
.DESCRIPTION
    Automatically downloads the Windows 11 IoT Enterprise LTSC ISO from
    Microsoft's Evaluation Center. Falls back to browser download if
    automated download is not possible.
.EXAMPLE
    .\download-iso.ps1
    .\download-iso.ps1 -OutputPath "C:\ISOs"
    .\download-iso.ps1 -Language "English"
#>

param(
    [Parameter()]
    [string]$OutputPath = ".",

    [Parameter()]
    [ValidateSet("English", "German", "French", "Spanish", "Japanese", "Korean", "Chinese")]
    [string]$Language = "English",

    [Parameter()]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Configuration
$EvalCenterUrl = "https://www.microsoft.com/en-us/evalcenter/evaluate-windows-11-iot-enterprise-ltsc"
$IsoFileName = "Windows11_IoT_Enterprise_LTSC.iso"
$OutputFile = Join-Path (Resolve-Path $OutputPath) $IsoFileName

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Windows 11 IoT Enterprise LTSC ISO"  -ForegroundColor Cyan
Write-Host "  Download Helper"                      -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if ISO already exists
if ((Test-Path $OutputFile) -and -not $Force) {
    Write-Host "ISO already exists: $OutputFile" -ForegroundColor Green
    Write-Host "Use -Force to re-download."
    return $OutputFile
}

# Create output directory if needed
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

Write-Host "Attempting to download Windows 11 IoT Enterprise LTSC..." -ForegroundColor Yellow
Write-Host ""

# Language codes for Microsoft downloads
$LangCodes = @{
    "English" = "en-us"
    "German" = "de-de"
    "French" = "fr-fr"
    "Spanish" = "es-es"
    "Japanese" = "ja-jp"
    "Korean" = "ko-kr"
    "Chinese" = "zh-cn"
}

$LangCode = $LangCodes[$Language]

# Try to get download info from evaluation center
try {
    Write-Host "Checking Microsoft Evaluation Center..." -ForegroundColor Gray

    # First, try to fetch the evaluation page to get the download API endpoint
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $session.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"

    # Try the direct download API endpoint
    $apiUrl = "https://www.microsoft.com/en-us/api/controls/contentinclude/html"
    $apiParams = @{
        "pageId" = "6abe2f47-d046-4eb8-83b1-52b6ebb0befc"
        "host" = "www.microsoft.com"
        "segments" = "software-download,windows11"
        "query" = ""
    }

    # Alternative: Try known evaluation download patterns
    # Microsoft sometimes has direct download links for evaluation ISOs

    $possibleUrls = @(
        # These are example patterns - actual URLs change frequently
        "https://software-static.download.prss.microsoft.com/dbazure/988969d5-f34g-4e03-ac9d-1f9786c66749/26100.1.240331-1435.ge_release_CLIENT_ENTERPRISES_OEM_x64FRE_en-us.iso"
    )

    $downloadUrl = $null
    $downloadFound = $false

    # Try to find a working download URL
    foreach ($url in $possibleUrls) {
        try {
            $response = Invoke-WebRequest -Uri $url -Method Head -TimeoutSec 10 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $downloadUrl = $url
                $downloadFound = $true
                break
            }
        }
        catch {
            continue
        }
    }

    if (-not $downloadFound) {
        throw "Could not find direct download link"
    }

    # Download the ISO
    Write-Host "Found download URL!" -ForegroundColor Green
    Write-Host "Downloading ISO (this may take 30-60 minutes)..." -ForegroundColor Yellow
    Write-Host ""

    # Use BITS for better download handling
    $bitsSupported = Get-Command Start-BitsTransfer -ErrorAction SilentlyContinue

    if ($bitsSupported) {
        Write-Host "Using BITS transfer for reliable download..." -ForegroundColor Gray
        Start-BitsTransfer -Source $downloadUrl -Destination $OutputFile -DisplayName "Windows 11 IoT LTSC"
    }
    else {
        # Fallback to Invoke-WebRequest with progress
        $ProgressPreference = 'Continue'
        Invoke-WebRequest -Uri $downloadUrl -OutFile $OutputFile -UseBasicParsing
    }

    Write-Host ""
    Write-Host "Download complete!" -ForegroundColor Green
    Write-Host "ISO saved to: $OutputFile" -ForegroundColor Green

    return $OutputFile
}
catch {
    Write-Host ""
    Write-Host "Automatic download not available." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Microsoft requires registration to download evaluation ISOs." -ForegroundColor White
    Write-Host ""
    Write-Host "Please download manually:" -ForegroundColor White
    Write-Host ""
    Write-Host "  1. Opening the Microsoft Evaluation Center in your browser..."
    Write-Host ""

    # Open the browser
    Start-Process $EvalCenterUrl

    Write-Host "  2. Fill out the registration form"
    Write-Host "  3. Select: 64-bit edition, $Language language"
    Write-Host "  4. Download the ISO file"
    Write-Host "  5. Save it as: $OutputFile"
    Write-Host ""
    Write-Host "  Or save it anywhere and run:" -ForegroundColor Gray
    Write-Host "    .\deploy.ps1 -IsoPath 'C:\path\to\downloaded.iso'" -ForegroundColor Gray
    Write-Host ""

    # Wait for user to download
    Write-Host "Waiting for ISO file..." -ForegroundColor Yellow
    Write-Host "(Press Ctrl+C to cancel)" -ForegroundColor DarkGray
    Write-Host ""

    $waitStart = Get-Date
    $maxWait = 3600  # 1 hour timeout

    # Also check Downloads folder
    $downloadsFolder = [Environment]::GetFolderPath("UserProfile") + "\Downloads"
    $possibleFiles = @(
        $OutputFile,
        "$downloadsFolder\*.iso"
    )

    while ($true) {
        # Check if the expected file exists
        if (Test-Path $OutputFile) {
            Write-Host ""
            Write-Host "ISO detected at: $OutputFile" -ForegroundColor Green
            return $OutputFile
        }

        # Check Downloads folder for any recent ISO files
        $recentIsos = Get-ChildItem -Path $downloadsFolder -Filter "*.iso" -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -gt $waitStart } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($recentIsos) {
            $foundIso = $recentIsos.FullName
            Write-Host ""
            Write-Host "ISO detected in Downloads: $foundIso" -ForegroundColor Green

            # Copy to expected location
            Write-Host "Copying to: $OutputFile" -ForegroundColor Gray
            Copy-Item -Path $foundIso -Destination $OutputFile -Force

            return $OutputFile
        }

        # Check timeout
        $elapsed = ((Get-Date) - $waitStart).TotalSeconds
        if ($elapsed -gt $maxWait) {
            Write-Host ""
            Write-Host "Timeout waiting for ISO download." -ForegroundColor Red
            Write-Host "Please download manually and run:" -ForegroundColor Yellow
            Write-Host "  .\deploy.ps1 -IsoPath 'C:\path\to\windows.iso'" -ForegroundColor Yellow
            exit 1
        }

        # Show waiting indicator
        $minutes = [math]::Floor($elapsed / 60)
        $seconds = [math]::Floor($elapsed % 60)
        Write-Host "`rWaiting... ($minutes`:$($seconds.ToString('00')) elapsed)" -NoNewline

        Start-Sleep -Seconds 5
    }
}
