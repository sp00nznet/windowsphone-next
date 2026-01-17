<#
.SYNOPSIS
    Downloads Windows 11 IoT Enterprise LTSC 2024 ISO automatically
.DESCRIPTION
    Downloads the Windows 11 IoT Enterprise LTSC 2024 ISO from archive.org.
    No user interaction required - fully automated download.
.EXAMPLE
    .\download-iso.ps1
    .\download-iso.ps1 -OutputPath "C:\ISOs"
#>

param(
    [Parameter()]
    [string]$OutputPath = ".",

    [Parameter()]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Archive.org item details
$ArchiveItemId = "windows-11-iot-enterprise-ltsc-2024"
$ArchiveBaseUrl = "https://archive.org/download/$ArchiveItemId"
$MetadataUrl = "https://archive.org/metadata/$ArchiveItemId"

# Output file
$IsoFileName = "Windows11_IoT_Enterprise_LTSC_2024.iso"
$OutputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    $OutputPath = $PWD.Path
}
$OutputFile = Join-Path $OutputPath $IsoFileName

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Windows 11 IoT Enterprise LTSC 2024" -ForegroundColor Cyan
Write-Host "  Automatic Download" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if ISO already exists
if ((Test-Path $OutputFile) -and -not $Force) {
    $fileSize = (Get-Item $OutputFile).Length / 1GB
    Write-Host "ISO already exists: $OutputFile" -ForegroundColor Green
    Write-Host "Size: $([math]::Round($fileSize, 2)) GB" -ForegroundColor Gray
    Write-Host "Use -Force to re-download."
    return $OutputFile
}

# Create output directory if needed
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

Write-Host "Source: archive.org/$ArchiveItemId" -ForegroundColor Gray
Write-Host "Destination: $OutputFile" -ForegroundColor Gray
Write-Host ""

# Try to get metadata to find the exact ISO filename
$IsoDownloadUrl = $null
$IsoFileSize = 0

Write-Host "Fetching file list from archive.org..." -ForegroundColor Yellow

try {
    # Try to get metadata JSON
    $metadata = Invoke-RestMethod -Uri $MetadataUrl -TimeoutSec 30 -ErrorAction Stop

    # Find the ISO file in the files list
    $isoFile = $metadata.files | Where-Object { $_.name -like "*.iso" } | Select-Object -First 1

    if ($isoFile) {
        $IsoDownloadUrl = "$ArchiveBaseUrl/$($isoFile.name)"
        $IsoFileSize = [long]$isoFile.size
        Write-Host "Found: $($isoFile.name)" -ForegroundColor Green
        Write-Host "Size: $([math]::Round($IsoFileSize / 1GB, 2)) GB" -ForegroundColor Gray
    }
}
catch {
    Write-Host "Could not fetch metadata, trying known filenames..." -ForegroundColor Yellow
}

# If metadata didn't work, try known filename patterns
if (-not $IsoDownloadUrl) {
    $knownFilenames = @(
        "en-us_windows_11_iot_enterprise_ltsc_2024_x64_dvd_f6b14814.iso",
        "en-us_windows_11_iot_enterprise_ltsc_2024_x64.iso",
        "Windows_11_IoT_Enterprise_LTSC_2024_x64.iso",
        "Win11_IoT_Enterprise_LTSC_2024_English_x64.iso",
        "windows-11-iot-enterprise-ltsc-2024.iso"
    )

    foreach ($filename in $knownFilenames) {
        $testUrl = "$ArchiveBaseUrl/$filename"
        Write-Host "Trying: $filename" -ForegroundColor Gray

        try {
            $response = Invoke-WebRequest -Uri $testUrl -Method Head -TimeoutSec 10 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                $IsoDownloadUrl = $testUrl
                if ($response.Headers["Content-Length"]) {
                    $IsoFileSize = [long]$response.Headers["Content-Length"]
                }
                Write-Host "Found: $filename" -ForegroundColor Green
                break
            }
        }
        catch {
            continue
        }
    }
}

# Last resort: construct URL from the item page pattern
if (-not $IsoDownloadUrl) {
    # Archive.org often uses the item ID as part of the filename
    $IsoDownloadUrl = "$ArchiveBaseUrl/${ArchiveItemId}.iso"
    Write-Host "Using default URL pattern..." -ForegroundColor Yellow
}

if (-not $IsoDownloadUrl) {
    Write-Host ""
    Write-Host "ERROR: Could not determine download URL." -ForegroundColor Red
    Write-Host ""
    Write-Host "Please download manually from:" -ForegroundColor Yellow
    Write-Host "  https://archive.org/details/$ArchiveItemId" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Then run:" -ForegroundColor Yellow
    Write-Host "  .\deploy.ps1 -IsoPath 'C:\path\to\downloaded.iso'" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "Downloading Windows 11 IoT Enterprise LTSC 2024..." -ForegroundColor Yellow
if ($IsoFileSize -gt 0) {
    Write-Host "This will download $([math]::Round($IsoFileSize / 1GB, 2)) GB" -ForegroundColor Gray
}
Write-Host "This may take 30-60 minutes depending on your connection." -ForegroundColor Gray
Write-Host ""

# Download the ISO
$downloadSuccess = $false
$tempFile = "$OutputFile.downloading"

# Method 1: Try BITS transfer (best for large files, supports resume)
$bitsAvailable = Get-Command Start-BitsTransfer -ErrorAction SilentlyContinue

if ($bitsAvailable) {
    Write-Host "Using BITS transfer (supports resume if interrupted)..." -ForegroundColor Gray
    Write-Host ""

    try {
        # Remove any existing temp file
        if (Test-Path $tempFile) {
            Remove-Item $tempFile -Force
        }

        Start-BitsTransfer -Source $IsoDownloadUrl -Destination $tempFile -DisplayName "Windows 11 IoT LTSC 2024" -Description "Downloading from archive.org"

        if (Test-Path $tempFile) {
            Move-Item $tempFile $OutputFile -Force
            $downloadSuccess = $true
        }
    }
    catch {
        Write-Host "BITS transfer failed: $_" -ForegroundColor Yellow
        Write-Host "Falling back to direct download..." -ForegroundColor Yellow
    }
}

# Method 2: Invoke-WebRequest with progress
if (-not $downloadSuccess) {
    Write-Host "Using direct download..." -ForegroundColor Gray
    Write-Host ""

    try {
        # Remove any existing temp file
        if (Test-Path $tempFile) {
            Remove-Item $tempFile -Force
        }

        $ProgressPreference = 'Continue'

        # Use .NET WebClient for better progress and performance
        $webClient = New-Object System.Net.WebClient
        $webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) WindowsPhoneNext-Downloader/1.0")

        # Progress tracking
        $downloadStart = Get-Date
        $lastProgress = 0

        $webClient.DownloadProgressChanged += {
            param($sender, $e)
            if ($e.ProgressPercentage -ne $lastProgress) {
                $lastProgress = $e.ProgressPercentage
                $elapsed = (Get-Date) - $downloadStart
                $speed = if ($elapsed.TotalSeconds -gt 0) { $e.BytesReceived / $elapsed.TotalSeconds / 1MB } else { 0 }
                Write-Progress -Activity "Downloading Windows 11 IoT Enterprise LTSC 2024" `
                    -Status "$($e.ProgressPercentage)% - $([math]::Round($speed, 1)) MB/s" `
                    -PercentComplete $e.ProgressPercentage
            }
        }

        $webClient.DownloadFileCompleted += {
            param($sender, $e)
            Write-Progress -Activity "Downloading" -Completed
            if ($e.Error) {
                throw $e.Error
            }
        }

        # Start async download and wait
        $downloadTask = $webClient.DownloadFileTaskAsync($IsoDownloadUrl, $tempFile)

        while (-not $downloadTask.IsCompleted) {
            Start-Sleep -Milliseconds 500
        }

        if ($downloadTask.IsFaulted) {
            throw $downloadTask.Exception
        }

        $webClient.Dispose()

        if (Test-Path $tempFile) {
            Move-Item $tempFile $OutputFile -Force
            $downloadSuccess = $true
        }
    }
    catch {
        Write-Host "WebClient download failed: $_" -ForegroundColor Yellow
    }
}

# Method 3: curl.exe (available on Windows 10+)
if (-not $downloadSuccess) {
    $curlPath = Get-Command curl.exe -ErrorAction SilentlyContinue

    if ($curlPath) {
        Write-Host "Using curl..." -ForegroundColor Gray
        Write-Host ""

        try {
            # Remove any existing temp file
            if (Test-Path $tempFile) {
                Remove-Item $tempFile -Force
            }

            & curl.exe -L -# -o $tempFile $IsoDownloadUrl

            if ($LASTEXITCODE -eq 0 -and (Test-Path $tempFile)) {
                Move-Item $tempFile $OutputFile -Force
                $downloadSuccess = $true
            }
        }
        catch {
            Write-Host "curl download failed: $_" -ForegroundColor Yellow
        }
    }
}

# Cleanup temp file if it exists
if (Test-Path $tempFile) {
    Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
}

# Verify download
if ($downloadSuccess -and (Test-Path $OutputFile)) {
    $finalSize = (Get-Item $OutputFile).Length

    # Basic size validation (ISO should be at least 4GB)
    if ($finalSize -lt 4GB) {
        Write-Host ""
        Write-Host "WARNING: Downloaded file seems too small ($([math]::Round($finalSize / 1GB, 2)) GB)" -ForegroundColor Yellow
        Write-Host "Expected at least 4 GB for Windows 11 ISO." -ForegroundColor Yellow
        Write-Host "The download may be incomplete or the file may be an error page." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Please verify the file or download manually from:" -ForegroundColor Yellow
        Write-Host "  https://archive.org/details/$ArchiveItemId" -ForegroundColor Cyan
    }
    else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  Download Complete!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "ISO saved to: $OutputFile" -ForegroundColor Green
        Write-Host "Size: $([math]::Round($finalSize / 1GB, 2)) GB" -ForegroundColor Gray
        Write-Host ""
    }

    return $OutputFile
}
else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Download Failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Could not download the ISO automatically." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please download manually:" -ForegroundColor White
    Write-Host "  1. Go to: https://archive.org/details/$ArchiveItemId" -ForegroundColor Cyan
    Write-Host "  2. Click the ISO file to download" -ForegroundColor White
    Write-Host "  3. Save it to: $OutputFile" -ForegroundColor White
    Write-Host ""
    Write-Host "Then run:" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -IsoPath '$OutputFile'" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}
