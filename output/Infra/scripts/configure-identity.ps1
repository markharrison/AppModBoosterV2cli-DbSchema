<#
.SYNOPSIS
    Configures managed identity database access for the Expenses application.
.DESCRIPTION
    Creates SQL users for the API and Web managed identities with appropriate
    database roles. Uses multi-method fallback for SQL execution.
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

$sqlServerName = "sql-$AppName-$Environment"
$sqlDatabaseName = "sqldb-$AppName-$Environment"
$sqlServerFqdn = "$sqlServerName.database.windows.net"
$apiIdentityName = "id-$AppName-api-$Environment"
$webIdentityName = "id-$AppName-web-$Environment"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Configure Identity" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "SQL Server   : $sqlServerFqdn"
Write-Host "Database     : $sqlDatabaseName"
Write-Host "API Identity : $apiIdentityName"
Write-Host "Web Identity : $webIdentityName"
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
# Get access token for Azure SQL
# ---------------------------------------------------------------------------
function Get-SqlAccessToken {
    Write-Host "Acquiring Azure SQL access token..." -ForegroundColor Yellow
    $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
    if (-not $token) {
        Write-Error "Failed to acquire SQL access token. Ensure you are logged in with 'az login'."
        exit 1
    }
    return $token
}

# ---------------------------------------------------------------------------
# Set Azure AD admin on SQL Server if not already set
# ---------------------------------------------------------------------------
function Set-AadAdminIfNeeded {
    Write-Host "Checking Azure AD admin on SQL Server..." -ForegroundColor Yellow
    $resourceGroup = "rg-$AppName-$Environment"

    $currentAdmin = az sql server ad-admin list `
        --server-name $sqlServerName `
        --resource-group $resourceGroup `
        --output json 2>$null | ConvertFrom-Json

    if (-not $currentAdmin -or $currentAdmin.Count -eq 0) {
        Write-Host "Setting current user as Azure AD admin..." -ForegroundColor Yellow
        $currentUser = az ad signed-in-user show --query '{displayName:displayName, id:id}' -o json | ConvertFrom-Json
        if ($currentUser) {
            Invoke-WithRetry {
                az sql server ad-admin create `
                    --server-name $sqlServerName `
                    --resource-group $resourceGroup `
                    --display-name $currentUser.displayName `
                    --object-id $currentUser.id `
                    --output none
            }
            Write-Host "Azure AD admin set to: $($currentUser.displayName)" -ForegroundColor Green
        }
        else {
            Write-Warning "Could not determine current user. Set Azure AD admin manually."
        }
    }
    else {
        Write-Host "Azure AD admin already set: $($currentAdmin[0].login)" -ForegroundColor Green
    }
}

# ---------------------------------------------------------------------------
# SQL execution methods (multi-method fallback)
# ---------------------------------------------------------------------------
function Invoke-SqlWithFallback {
    param(
        [string]$SqlQuery,
        [string]$AccessToken,
        [string]$ServerFqdn,
        [string]$DatabaseName
    )

    # Method 1: Invoke-Sqlcmd (SqlServer PowerShell module)
    Write-Host "  Trying Method 1: Invoke-Sqlcmd..." -ForegroundColor Gray
    try {
        if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
            Invoke-Sqlcmd -ServerInstance $ServerFqdn `
                -Database $DatabaseName `
                -AccessToken $AccessToken `
                -Query $SqlQuery `
                -ConnectionTimeout 30 `
                -QueryTimeout 30
            Write-Host "  Success (Invoke-Sqlcmd)" -ForegroundColor Green
            return $true
        }
        Write-Host "  Invoke-Sqlcmd not available." -ForegroundColor Gray
    }
    catch {
        Write-Warning "  Invoke-Sqlcmd failed: $($_.Exception.Message)"
    }

    # Method 2: sqlcmd CLI
    Write-Host "  Trying Method 2: sqlcmd..." -ForegroundColor Gray
    try {
        if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
            $env:SQLCMDPASSWORD = $AccessToken
            $result = sqlcmd -S $ServerFqdn -d $DatabaseName -G -Q $SqlQuery 2>&1
            $env:SQLCMDPASSWORD = $null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  Success (sqlcmd)" -ForegroundColor Green
                return $true
            }
            Write-Warning "  sqlcmd returned exit code: $LASTEXITCODE"
        }
        Write-Host "  sqlcmd not available." -ForegroundColor Gray
    }
    catch {
        Write-Warning "  sqlcmd failed: $($_.Exception.Message)"
    }

    # Method 3: .NET System.Data.SqlClient
    Write-Host "  Trying Method 3: System.Data.SqlClient..." -ForegroundColor Gray
    try {
        $connStr = "Server=tcp:$ServerFqdn,1433;Database=$DatabaseName;Encrypt=True;TrustServerCertificate=False;"
        $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
        $conn.AccessToken = $AccessToken
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $SqlQuery
        $cmd.CommandTimeout = 30
        $cmd.ExecuteNonQuery() | Out-Null
        $conn.Close()
        Write-Host "  Success (System.Data.SqlClient)" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Warning "  System.Data.SqlClient failed: $($_.Exception.Message)"
    }

    # Method 4: Microsoft.Data.SqlClient from NuGet cache
    Write-Host "  Trying Method 4: Microsoft.Data.SqlClient..." -ForegroundColor Gray
    try {
        $nugetPaths = @(
            "$env:USERPROFILE\.nuget\packages\microsoft.data.sqlclient"
            "$env:NUGET_PACKAGES\microsoft.data.sqlclient"
        )
        $dllPath = $null
        foreach ($basePath in $nugetPaths) {
            if (Test-Path $basePath) {
                $latestVersion = Get-ChildItem $basePath -Directory | Sort-Object Name -Descending | Select-Object -First 1
                if ($latestVersion) {
                    $candidate = Join-Path $latestVersion.FullName "lib\net8.0\Microsoft.Data.SqlClient.dll"
                    if (-not (Test-Path $candidate)) {
                        $candidate = Join-Path $latestVersion.FullName "lib\net6.0\Microsoft.Data.SqlClient.dll"
                    }
                    if (Test-Path $candidate) { $dllPath = $candidate; break }
                }
            }
        }

        if ($dllPath) {
            Add-Type -Path $dllPath -ErrorAction SilentlyContinue
            $connStr = "Server=tcp:$ServerFqdn,1433;Database=$DatabaseName;Encrypt=True;TrustServerCertificate=False;"
            $conn = New-Object Microsoft.Data.SqlClient.SqlConnection($connStr)
            $conn.AccessToken = $AccessToken
            $conn.Open()
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = $SqlQuery
            $cmd.CommandTimeout = 30
            $cmd.ExecuteNonQuery() | Out-Null
            $conn.Close()
            Write-Host "  Success (Microsoft.Data.SqlClient)" -ForegroundColor Green
            return $true
        }
        Write-Host "  Microsoft.Data.SqlClient DLL not found in NuGet cache." -ForegroundColor Gray
    }
    catch {
        Write-Warning "  Microsoft.Data.SqlClient failed: $($_.Exception.Message)"
    }

    return $false
}

# ---------------------------------------------------------------------------
# Create SQL user for a managed identity
# ---------------------------------------------------------------------------
function New-SqlUserForIdentity {
    param(
        [string]$IdentityName,
        [string]$AccessToken,
        [string]$ServerFqdn,
        [string]$DatabaseName
    )

    Write-Host ""
    Write-Host "Creating SQL user for '$IdentityName'..." -ForegroundColor Yellow

    # Create user and assign roles (idempotent — IF NOT EXISTS pattern)
    $sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$IdentityName')
BEGIN
    CREATE USER [$IdentityName] FROM EXTERNAL PROVIDER;
END

IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
    JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
    WHERE r.name = 'db_datareader' AND m.name = '$IdentityName')
BEGIN
    ALTER ROLE db_datareader ADD MEMBER [$IdentityName];
END

IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
    JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
    WHERE r.name = 'db_datawriter' AND m.name = '$IdentityName')
BEGIN
    ALTER ROLE db_datawriter ADD MEMBER [$IdentityName];
END

IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
    JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
    WHERE r.name = 'db_ddladmin' AND m.name = '$IdentityName')
BEGIN
    ALTER ROLE db_ddladmin ADD MEMBER [$IdentityName];
END
"@

    $success = Invoke-WithRetry {
        Invoke-SqlWithFallback -SqlQuery $sql -AccessToken $AccessToken `
            -ServerFqdn $ServerFqdn -DatabaseName $DatabaseName
    }

    if (-not $success) {
        Write-Warning "All automated SQL methods failed for '$IdentityName'."
        Write-Host ""
        Write-Host "============================================" -ForegroundColor Yellow
        Write-Host " MANUAL STEPS REQUIRED" -ForegroundColor Yellow
        Write-Host "============================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Open Azure Portal > SQL Database '$DatabaseName' > Query Editor"
        Write-Host "Log in with Azure AD and run:"
        Write-Host ""
        Write-Host $sql -ForegroundColor White
        Write-Host ""
        return $false
    }

    Write-Host "SQL user '$IdentityName' configured successfully." -ForegroundColor Green
    return $true
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
Set-AadAdminIfNeeded
$accessToken = Get-SqlAccessToken

$apiResult = New-SqlUserForIdentity -IdentityName $apiIdentityName `
    -AccessToken $accessToken -ServerFqdn $sqlServerFqdn -DatabaseName $sqlDatabaseName

$webResult = New-SqlUserForIdentity -IdentityName $webIdentityName `
    -AccessToken $accessToken -ServerFqdn $sqlServerFqdn -DatabaseName $sqlDatabaseName

Write-Host ""
if ($apiResult -and $webResult) {
    Write-Host "============================================" -ForegroundColor Green
    Write-Host " Identity Configuration Complete" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
}
else {
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host " Identity Configuration Partially Complete" -ForegroundColor Yellow
    Write-Host " See manual instructions above." -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
}
