<#
.SYNOPSIS
    Builds and deploys both applications to Azure App Service.
.DESCRIPTION
    Publishes the API and Web apps, then deploys them to Azure using zip deploy.
.PARAMETER AppName
    Application name prefix. Default: 'expenses'
.PARAMETER Environment
    Target environment (staging or production).
#>

param(
    [string]$AppName = 'expenses',
    [string]$Environment
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Validate environment
# ---------------------------------------------------------------------------
$Environment = ($Environment ?? '').Trim().ToLower()
if ($Environment -notin @('staging', 'production')) {
    Write-Error "Environment must be 'staging' or 'production'. Got: '$Environment'"
    exit 1
}

$apiAppName = "app-$AppName-api-$Environment"
$webAppName = "app-$AppName-web-$Environment"
$resourceGroup = "rg-$AppName-$Environment"

# Resolve project paths relative to the repo root
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$apiProjectDir = Join-Path $repoRoot 'src' 'Expenses.Api'
$webProjectDir = Join-Path $repoRoot 'src' 'Expenses.Web'
$publishRoot = Join-Path $repoRoot 'publish'
$apiPublishDir = Join-Path $publishRoot 'api'
$webPublishDir = Join-Path $publishRoot 'web'

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Deploy Applications" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "App Name       : $AppName"
Write-Host "Environment    : $Environment"
Write-Host "Resource Group : $resourceGroup"
Write-Host "API App        : $apiAppName"
Write-Host "Web App        : $webAppName"
Write-Host ""

# ---------------------------------------------------------------------------
# Retry helper
# ---------------------------------------------------------------------------
function Invoke-WithRetry {
    param(
        [scriptblock]$ScriptBlock,
        [int]$MaxAttempts = 3,
        [int]$BackoffSeconds = 15
    )
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            return & $ScriptBlock
        }
        catch {
            if ($attempt -eq $MaxAttempts) { throw }
            Write-Warning "Attempt $attempt/$MaxAttempts failed: $($_.Exception.Message)"
            Write-Host "Retrying in $BackoffSeconds seconds..."
            Start-Sleep -Seconds $BackoffSeconds
        }
    }
}

# ---------------------------------------------------------------------------
# Build and publish
# ---------------------------------------------------------------------------
Write-Host "Building and publishing API App..." -ForegroundColor Yellow
dotnet publish $apiProjectDir -c Release -o $apiPublishDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Error "API publish failed."; exit 1 }
Write-Host "API App published." -ForegroundColor Green

Write-Host "Building and publishing Web App..." -ForegroundColor Yellow
dotnet publish $webProjectDir -c Release -o $webPublishDir --nologo
if ($LASTEXITCODE -ne 0) { Write-Error "Web App publish failed."; exit 1 }
Write-Host "Web App published." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Create zip packages
# ---------------------------------------------------------------------------
$apiZip = Join-Path $publishRoot 'api.zip'
$webZip = Join-Path $publishRoot 'web.zip'

if (Test-Path $apiZip) { Remove-Item $apiZip -Force }
if (Test-Path $webZip) { Remove-Item $webZip -Force }

Write-Host "Creating deployment packages..." -ForegroundColor Yellow
Compress-Archive -Path "$apiPublishDir\*" -DestinationPath $apiZip
Compress-Archive -Path "$webPublishDir\*" -DestinationPath $webZip
Write-Host "Packages created." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Deploy to Azure
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Deploying API App to '$apiAppName'..." -ForegroundColor Yellow
Invoke-WithRetry {
    az webapp deploy `
        --resource-group $resourceGroup `
        --name $apiAppName `
        --src-path $apiZip `
        --type zip `
        --output none
}
Write-Host "API App deployed." -ForegroundColor Green

Write-Host "Deploying Web App to '$webAppName'..." -ForegroundColor Yellow
Invoke-WithRetry {
    az webapp deploy `
        --resource-group $resourceGroup `
        --name $webAppName `
        --src-path $webZip `
        --type zip `
        --output none
}
Write-Host "Web App deployed." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Verify deployments
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Verifying deployments..." -ForegroundColor Yellow

$apiUrl = "https://$apiAppName.azurewebsites.net"
$webUrl = "https://$webAppName.azurewebsites.net"

# Give apps time to start
Start-Sleep -Seconds 10

$verifyOk = $true

# Verify API /live endpoint
try {
    $apiResponse = Invoke-WithRetry {
        $resp = Invoke-WebRequest -Uri "$apiUrl/live" -UseBasicParsing -TimeoutSec 30
        if ($resp.StatusCode -ne 200) { throw "API /live returned $($resp.StatusCode)" }
        return $resp
    }
    Write-Host "API /live: OK ($($apiResponse.StatusCode))" -ForegroundColor Green
}
catch {
    Write-Warning "API /live verification failed: $($_.Exception.Message)"
    $verifyOk = $false
}

# Verify Web App
try {
    $webResponse = Invoke-WithRetry {
        $resp = Invoke-WebRequest -Uri "$webUrl/live" -UseBasicParsing -TimeoutSec 30
        if ($resp.StatusCode -ne 200) { throw "Web /live returned $($resp.StatusCode)" }
        return $resp
    }
    Write-Host "Web /live: OK ($($webResponse.StatusCode))" -ForegroundColor Green
}
catch {
    Write-Warning "Web /live verification failed: $($_.Exception.Message)"
    $verifyOk = $false
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host ""
if ($verifyOk) {
    Write-Host "============================================" -ForegroundColor Green
    Write-Host " Deployment Succeeded" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
}
else {
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host " Deployment Complete (verification warnings)" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
}
Write-Host "API URL: $apiUrl"
Write-Host "Web URL: $webUrl"

# Clean up publish artifacts
Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
