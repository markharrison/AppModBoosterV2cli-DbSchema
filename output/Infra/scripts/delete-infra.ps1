<#
.SYNOPSIS
    Deletes all Azure infrastructure for the Expenses application.
.DESCRIPTION
    Removes the entire resource group. Requires confirmation unless -Force is specified.
.PARAMETER AppName
    Application name prefix. Default: 'expenses'
.PARAMETER Environment
    Target environment (staging or production).
.PARAMETER Force
    Skip confirmation prompt.
#>

param(
    [string]$AppName = 'expenses',
    [string]$Environment,
    [switch]$Force
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

$resourceGroupName = "rg-$AppName-$Environment"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Delete Infrastructure" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Resource Group: $resourceGroupName"
Write-Host ""

# ---------------------------------------------------------------------------
# Check if resource group exists
# ---------------------------------------------------------------------------
$rgExists = az group exists --name $resourceGroupName 2>$null
if ($rgExists -ne 'true') {
    Write-Host "Resource group '$resourceGroupName' does not exist. Nothing to delete." -ForegroundColor Yellow
    exit 0
}

# ---------------------------------------------------------------------------
# Confirm deletion
# ---------------------------------------------------------------------------
if (-not $Force) {
    Write-Host "WARNING: This will permanently delete ALL resources in '$resourceGroupName'." -ForegroundColor Red
    Write-Host ""
    $confirmation = Read-Host "Type the resource group name to confirm deletion"
    if ($confirmation -ne $resourceGroupName) {
        Write-Host "Deletion cancelled. Input did not match resource group name." -ForegroundColor Yellow
        exit 0
    }
}

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
# Delete resource group
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Deleting resource group '$resourceGroupName'..." -ForegroundColor Yellow
Invoke-WithRetry {
    az group delete --name $resourceGroupName --yes --no-wait --output none
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host " Deletion Initiated" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "Resource group '$resourceGroupName' is being deleted."
Write-Host "This may take several minutes to complete in the background."
