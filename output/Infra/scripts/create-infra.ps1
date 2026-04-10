<#
.SYNOPSIS
    Creates Azure infrastructure for the Expenses application.
.DESCRIPTION
    Deploys the Bicep templates to provision all Azure resources.
    Creates the resource group if it does not already exist.
.PARAMETER AppName
    Application name prefix. Default: 'expenses'
.PARAMETER Environment
    Target environment (staging or production).
.PARAMETER Location
    Azure region. Default: 'uksouth'
.PARAMETER AppServicePlanSku
    App Service Plan SKU. Default: 'B1'
#>

param(
    [string]$AppName = 'expenses',
    [string]$Environment,
    [string]$Location = 'uksouth',
    [string]$AppServicePlanSku = 'B1'
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
$templateFile = Join-Path $PSScriptRoot '..\bicep\main.bicep'
$deploymentName = "deploy-$AppName-$Environment-$(Get-Date -Format 'yyyyMMddHHmmss')"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Create Infrastructure" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "App Name     : $AppName"
Write-Host "Environment  : $Environment"
Write-Host "Location     : $Location"
Write-Host "SKU          : $AppServicePlanSku"
Write-Host "Resource Group: $resourceGroupName"
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
# Create resource group if it doesn't exist
# ---------------------------------------------------------------------------
Write-Host "Checking resource group '$resourceGroupName'..." -ForegroundColor Yellow
$rgExists = az group exists --name $resourceGroupName 2>$null
if ($rgExists -ne 'true') {
    Write-Host "Creating resource group '$resourceGroupName' in '$Location'..."
    Invoke-WithRetry {
        az group create --name $resourceGroupName --location $Location --output none
    }
    Write-Host "Resource group created." -ForegroundColor Green
} else {
    Write-Host "Resource group already exists." -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Resolve current Azure AD identity for SQL Admin (MCAPS policy: AAD-only auth)
# Extracts oid from the ARM access token — no Graph API call needed.
# ---------------------------------------------------------------------------
Write-Host "Resolving Azure AD identity for SQL Admin..." -ForegroundColor Yellow
$account = az account show --output json 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged in to Azure CLI. Run 'az login' first."
    exit 1
}
$sqlAadAdminName = $account.user.name

if ($account.user.type -eq 'servicePrincipal') {
    $sqlAadAdminObjectId = az ad sp show --id $sqlAadAdminName --query id -o tsv 2>$null
} else {
    # Decode the oid claim from the ARM access token (base64url → base64)
    $tokenJson = az account get-access-token --resource https://management.azure.com -o json 2>$null | ConvertFrom-Json
    $payload = $tokenJson.accessToken.Split('.')[1].Replace('-','+').Replace('_','/')
    $pad = $payload.Length % 4
    if ($pad) { $payload += '=' * (4 - $pad) }
    $claims = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload)) | ConvertFrom-Json
    $sqlAadAdminObjectId = $claims.oid
}

if (-not $sqlAadAdminObjectId) {
    Write-Error "Could not resolve Azure AD object ID. Ensure you are logged in with 'az login'."
    exit 1
}
Write-Host "SQL Admin  : $sqlAadAdminName ($sqlAadAdminObjectId)" -ForegroundColor Green
Write-Host ""

# ---------------------------------------------------------------------------
# Deploy Bicep template
# ---------------------------------------------------------------------------
Write-Host "Deploying Bicep template..." -ForegroundColor Yellow

$deploymentOutput = Invoke-WithRetry {
    az deployment group create `
        --resource-group $resourceGroupName `
        --name $deploymentName `
        --template-file $templateFile `
        --parameters `
            appName=$AppName `
            environment=$Environment `
            location=$Location `
            appServicePlanSku=$AppServicePlanSku `
            sqlAadAdminObjectId=$sqlAadAdminObjectId `
            sqlAadAdminName=$sqlAadAdminName `
        --output json
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed."
    exit 1
}

# ---------------------------------------------------------------------------
# Parse and display outputs
# ---------------------------------------------------------------------------
$result = $deploymentOutput | ConvertFrom-Json
$outputs = $result.properties.outputs

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host " Deployment Succeeded" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "API URL              : $($outputs.apiUrl.value)"
Write-Host "Web URL              : $($outputs.webUrl.value)"
Write-Host "SQL Server FQDN      : $($outputs.sqlServerFqdn.value)"
Write-Host "API Identity (Principal): $($outputs.apiIdentityPrincipalId.value)"
Write-Host "Web Identity (Principal): $($outputs.webIdentityPrincipalId.value)"
Write-Host ""

# Return outputs for pipeline consumption
return $outputs
