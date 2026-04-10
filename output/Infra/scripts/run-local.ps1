<#
.SYNOPSIS
    Starts both applications locally in Development mode.
.DESCRIPTION
    Launches the API App on port 5201 and the Web App on port 5200.
    Waits for the API to be ready before starting the Web App.
    Handles Ctrl+C to stop both processes cleanly.
#>

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Resolve project paths
# ---------------------------------------------------------------------------
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$apiProjectDir = Join-Path $repoRoot 'src' 'Expenses.Api'
$webProjectDir = Join-Path $repoRoot 'src' 'Expenses.Web'

$apiUrl = 'http://localhost:5201'
$webUrl = 'http://localhost:5200'

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Run Local Development" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "API App: $apiUrl"
Write-Host "Web App: $webUrl"
Write-Host ""

# ---------------------------------------------------------------------------
# Validate project directories exist
# ---------------------------------------------------------------------------
if (-not (Test-Path (Join-Path $apiProjectDir '*.csproj'))) {
    Write-Error "API project not found at: $apiProjectDir"
    exit 1
}
if (-not (Test-Path (Join-Path $webProjectDir '*.csproj'))) {
    Write-Error "Web project not found at: $webProjectDir"
    exit 1
}

# ---------------------------------------------------------------------------
# Process management
# ---------------------------------------------------------------------------
$apiProcess = $null
$webProcess = $null

function Stop-AllProcesses {
    Write-Host ""
    Write-Host "Shutting down..." -ForegroundColor Yellow

    if ($script:webProcess -and -not $script:webProcess.HasExited) {
        Write-Host "Stopping Web App (PID: $($script:webProcess.Id))..."
        try {
            Stop-Process -Id $script:webProcess.Id -Force -ErrorAction SilentlyContinue
            $script:webProcess.WaitForExit(5000) | Out-Null
        }
        catch { }
    }

    if ($script:apiProcess -and -not $script:apiProcess.HasExited) {
        Write-Host "Stopping API App (PID: $($script:apiProcess.Id))..."
        try {
            Stop-Process -Id $script:apiProcess.Id -Force -ErrorAction SilentlyContinue
            $script:apiProcess.WaitForExit(5000) | Out-Null
        }
        catch { }
    }

    Write-Host "All processes stopped." -ForegroundColor Green
}

# Register Ctrl+C handler
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-AllProcesses }

try {
    # ---------------------------------------------------------------------------
    # Start API App
    # ---------------------------------------------------------------------------
    Write-Host "Starting API App..." -ForegroundColor Yellow
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    $apiProcess = Start-Process -FilePath 'dotnet' `
        -ArgumentList "run --project `"$apiProjectDir`" --urls $apiUrl --no-launch-profile" `
        -PassThru `
        -NoNewWindow `
        -Environment @{ ASPNETCORE_ENVIRONMENT = 'Development' }

    # Wait for API to be ready
    Write-Host "Waiting for API App to be ready..." -ForegroundColor Yellow
    $maxWait = 60
    $waited = 0
    $ready = $false

    while ($waited -lt $maxWait) {
        Start-Sleep -Seconds 2
        $waited += 2

        if ($apiProcess.HasExited) {
            Write-Error "API App exited unexpectedly with code: $($apiProcess.ExitCode)"
            exit 1
        }

        try {
            $response = Invoke-WebRequest -Uri "$apiUrl/ready" -UseBasicParsing -TimeoutSec 3 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            # API not ready yet
            Write-Host "  Waiting... ($waited/$maxWait seconds)" -ForegroundColor Gray
        }
    }

    if (-not $ready) {
        # Try /live as fallback — app may be running but not fully ready yet
        try {
            $liveResp = Invoke-WebRequest -Uri "$apiUrl/live" -UseBasicParsing -TimeoutSec 3 -ErrorAction SilentlyContinue
            if ($liveResp.StatusCode -eq 200) {
                Write-Host "API App is live (readiness check timed out, proceeding)." -ForegroundColor Yellow
            }
            else {
                Write-Error "API App did not become ready within $maxWait seconds."
                Stop-AllProcesses
                exit 1
            }
        }
        catch {
            Write-Error "API App did not become ready within $maxWait seconds."
            Stop-AllProcesses
            exit 1
        }
    }
    else {
        Write-Host "API App is ready." -ForegroundColor Green
    }

    # ---------------------------------------------------------------------------
    # Start Web App
    # ---------------------------------------------------------------------------
    Write-Host "Starting Web App..." -ForegroundColor Yellow

    $webProcess = Start-Process -FilePath 'dotnet' `
        -ArgumentList "run --project `"$webProjectDir`" --urls $webUrl --no-launch-profile" `
        -PassThru `
        -NoNewWindow `
        -Environment @{ ASPNETCORE_ENVIRONMENT = 'Development' }

    # Wait briefly for Web App to start
    Start-Sleep -Seconds 5

    if ($webProcess.HasExited) {
        Write-Error "Web App exited unexpectedly with code: $($webProcess.ExitCode)"
        Stop-AllProcesses
        exit 1
    }

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host " Applications Running" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "API App: $apiUrl" -ForegroundColor White
    Write-Host "Web App: $webUrl" -ForegroundColor White
    Write-Host ""
    Write-Host "Press Ctrl+C to stop both applications." -ForegroundColor Gray
    Write-Host ""

    # Keep running until a process exits or user presses Ctrl+C
    while ($true) {
        if ($apiProcess.HasExited) {
            Write-Warning "API App has stopped (exit code: $($apiProcess.ExitCode))."
            break
        }
        if ($webProcess.HasExited) {
            Write-Warning "Web App has stopped (exit code: $($webProcess.ExitCode))."
            break
        }
        Start-Sleep -Seconds 2
    }
}
finally {
    Stop-AllProcesses
    Unregister-Event -SourceIdentifier PowerShell.Exiting -ErrorAction SilentlyContinue
}
