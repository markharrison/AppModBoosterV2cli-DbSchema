# Deployment Guide — Expense Management System

---

## 1. Prerequisites

| Requirement | Version / Details |
|---|---|
| .NET SDK | 10.0 or later |
| PowerShell | 7.6 or later |
| Azure CLI | Latest (`az` command available) |
| Azure Subscription | With permissions to create App Service, SQL Database, and Managed Identity resources |
| Azure AD Login | The deployer must be signed in via `az login` with an Azure AD identity (user or service principal). Required because MCAPS policy enforces Azure AD-only authentication on SQL Server — the script auto-resolves the deployer's identity as the SQL admin. |
| GitHub Repository | With OIDC federated credentials configured for CI/CD |
| Git | Latest |

Optional (for E2E testing):

| Requirement | Details |
|---|---|
| Playwright browsers | Install via `pwsh bin/Debug/net10.0/playwright.ps1 install` in the E2E test project |

---

## 2. Architecture Overview

```
┌─────────────────┐       ┌─────────────────┐       ┌────────────────────────┐
│    Web App       │──────>│    API App       │──────>│      Database          │
│  (Razor Views)   │ HTTP  │  (Web API)       │  EF   │ (SQLite / SQL Server)  │
│  Expenses.Web    │       │  Expenses.Api    │ Core  │                        │
└─────────────────┘       └─────────────────┘       └────────────────────────┘
```

- **Web App** (`Expenses.Web`) — Razor Views UI on port 5200 (local). Calls the API App server-side using `HttpClient`. Never connects to the database directly.
- **API App** (`Expenses.Api`) — Controller-based REST APIs on port 5201 (local). Owns the database context, migrations, and seeding. Exposes `/live` and `/ready` health endpoints.
- **Database** — SQLite in Development; Azure SQL Database in Staging/Production.

CORS is not required — the API is only called server-side by the Web App.

---

## 3. Local Development Setup

Local development uses the **Development** environment with SQLite.

### 3.1 Clone the Repository

```powershell
git clone <your-repo-url>
cd <repo-root>
```

### 3.2 Start the Applications

```powershell
./output/Infra/scripts/run-local.ps1
```

This script:
- Sets `ASPNETCORE_ENVIRONMENT=Development`
- Starts the API App on port **5201**
- Starts the Web App on port **5200**
- The API App automatically runs database creation (`EnsureCreatedAsync`) and seeds sample data on startup

### 3.3 Verify Local Setup

| Endpoint | URL | Expected |
|---|---|---|
| Web App | http://localhost:5200 | Dashboard page loads with expense summary |
| API App | http://localhost:5201 | API root |
| API Docs (Scalar) | http://localhost:5201/scalar/v1 | Interactive API documentation |
| Liveness | http://localhost:5201/live | `200 OK` |
| Readiness | http://localhost:5201/ready | `200 OK` (after startup completes) |

### 3.4 Sample Data

In Development and Staging, the API seeds:
- 2 roles (Employee, Manager)
- 5 expense categories (Travel, Meals, Supplies, Accommodation, Other)
- 4 expense statuses (Draft, Submitted, Approved, Rejected)
- 2 users (Alice — employee, Bob — manager)
- 4 sample expenses belonging to Alice

---

## 4. Azure Deployment

### 4.1 Environment Summary

| Environment | Database | Authentication | Seeding | API Docs |
|---|---|---|---|---|
| Development | SQLite | None | Yes | Yes (Scalar) |
| Staging | Azure SQL Database | Managed Identity (Entra ID) | Yes | Yes (Scalar) |
| Production | Azure SQL Database | Managed Identity (Entra ID) | No | No |

### 4.2 Azure Resource Naming

Resources follow the naming convention: `{resourcetype}-{appname}-{env}`

With the canonical app name `expenses`, example resources for staging:
- `plan-expenses-staging` — App Service Plan (Linux, B1)
- `app-expenses-web-staging` — Web App
- `app-expenses-api-staging` — API App
- `sql-expenses-staging` — SQL Server
- `sqldb-expenses-staging` — SQL Database

### 4.3 Deployment Steps (Correct Execution Order)

> **⚠️ CRITICAL**: The scripts MUST be executed in the order below. The API App uses managed identity to authenticate to Azure SQL. If you deploy the app before configuring identity and database access, the API will fail to start because it cannot connect to the database.

#### Step 1: Create Infrastructure

```powershell
./output/Infra/scripts/create-infra.ps1 -Environment staging
```

This provisions:
- Azure Resource Group
- App Service Plan (Linux, B1 — overridable)
- Web App and API App (App Service)
- SQL Server with Azure AD-only authentication (MCAPS policy requirement) and SQL Database (Basic tier)
- User-assigned Managed Identities for both apps

> **Note**: The script automatically detects the current Azure AD identity (user or service principal from `az login`) and configures it as the SQL Server administrator. No manual AAD admin parameters are needed.

#### Step 2: Configure Identity and Database Access

```powershell
./output/Infra/scripts/configure-identity.ps1 -Environment staging
```

> **This step MUST come before deploying applications.**

This script:
- Grants the API App's managed identity access to Azure SQL Database
- Creates the SQL user for managed identity authentication
- Uses multiple fallback connection methods (Invoke-Sqlcmd → sqlcmd → .NET SqlConnection → Microsoft.Data.SqlClient)
- If all automated methods fail, prints manual instructions for Azure Portal Query Editor

#### Step 3: Deploy Applications

```powershell
./output/Infra/scripts/deploy-apps.ps1 -Environment staging
```

This deploys:
- API App code (with migrations that run on startup via `MigrateAsync()`)
- Web App code
- Sets Azure App Settings:
  - API base URL for the Web App
  - SQL connection string with managed identity authentication
  - `DisableHttpsRedirect=true` (Azure terminates HTTPS before the app)

#### Step 4: Verify Deployment

After deployment, verify the health endpoints:

```powershell
# Check API liveness (process is running)
Invoke-RestMethod -Uri "https://app-expenses-api-staging.azurewebsites.net/live"

# Check API readiness (migrations complete, DB accessible)
Invoke-RestMethod -Uri "https://app-expenses-api-staging.azurewebsites.net/ready"
```

| Endpoint | Expected Response | Meaning |
|---|---|---|
| `/live` | `200 OK` | Process is running |
| `/ready` | `200 OK` | Migrations, seeding, and DB connectivity are complete |
| `/ready` | `503 Service Unavailable` | Startup still in progress — wait and retry |

The API uses retry logic with backoff (at least 5 attempts) during startup to handle cloud cold-start timing.

### 4.4 Deployment for Production

```powershell
./output/Infra/scripts/create-infra.ps1 -Environment production
./output/Infra/scripts/configure-identity.ps1 -Environment production
./output/Infra/scripts/deploy-apps.ps1 -Environment production
```

Production differences:
- No sample data seeding
- API docs (Scalar) are disabled
- Same managed identity authentication

---

## 5. GitHub OIDC Setup

The CI/CD pipeline uses GitHub Actions with OIDC federated credentials — no stored secrets.

### 5.1 Register an App in Microsoft Entra ID

1. Navigate to **Azure Portal → Microsoft Entra ID → App registrations → New registration**
2. Name: `expenses-github-oidc` (or similar)
3. Note the **Application (client) ID** and **Directory (tenant) ID**

### 5.2 Create Federated Credentials

For each environment (staging, production), add a federated credential:

1. Go to **App registration → Certificates & secrets → Federated credentials → Add credential**
2. Select **GitHub Actions deploying Azure resources**
3. Configure:

| Field | Value |
|---|---|
| Organization | Your GitHub org or username |
| Repository | Your repository name |
| Entity type | Environment |
| Environment name | `Staging` or `Production` |
| Name | `expenses-staging-oidc` or `expenses-production-oidc` |

### 5.3 Grant Azure Permissions

Assign the app registration a role (e.g. **Contributor**) on the Azure subscription or resource group:

```powershell
az role assignment create `
  --assignee <app-client-id> `
  --role "Contributor" `
  --scope "/subscriptions/<subscription-id>"
```

### 5.4 Configure GitHub Repository

Add these as GitHub repository variables or environment-level variables:

| Variable | Value |
|---|---|
| `AZURE_CLIENT_ID` | Application (client) ID from Entra ID |
| `AZURE_TENANT_ID` | Directory (tenant) ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

### 5.5 GitHub Actions Workflow Usage

In workflows, use the `azure/login` action with OIDC:

```yaml
- uses: azure/login@v2
  with:
    client-id: ${{ vars.AZURE_CLIENT_ID }}
    tenant-id: ${{ vars.AZURE_TENANT_ID }}
    subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
```

---

## 6. Environment Configuration Reference

### 6.1 Development

| Setting | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| Web App port | `5200` |
| API App port | `5201` |
| Database | SQLite (local file) |
| API base URL (Web App config) | `http://localhost:5201` |
| Authentication | None |
| Seeding | Yes |
| API Docs (Scalar) | Yes — http://localhost:5201/scalar/v1 |

### 6.2 Staging

| Setting | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| Database | Azure SQL Database (Basic tier) |
| DB Authentication | Managed Identity (Entra ID) |
| DB Connection String | `Server=tcp:sql-expenses-staging...;Authentication=Active Directory Managed Identity` |
| API base URL | Set via Azure App Settings |
| `DisableHttpsRedirect` | `true` |
| Seeding | Yes |
| API Docs (Scalar) | Yes |

### 6.3 Production

| Setting | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| Database | Azure SQL Database (Basic tier) |
| DB Authentication | Managed Identity (Entra ID) |
| DB Connection String | `Server=tcp:sql-expenses-production...;Authentication=Active Directory Managed Identity` |
| API base URL | Set via Azure App Settings |
| `DisableHttpsRedirect` | `true` |
| Seeding | No |
| API Docs (Scalar) | No |

### 6.4 Configuration Hierarchy

Configuration is resolved in this order (later overrides earlier):

1. `appsettings.json` — base defaults
2. `appsettings.{Environment}.json` — environment-specific overrides
3. Environment variables / Azure App Settings — runtime overrides

In Azure, App Settings override any values in `appsettings.*.json` files.

---

## 7. Infrastructure Teardown

To delete all Azure resources for an environment:

```powershell
./output/Infra/scripts/delete-infra.ps1 -Environment staging -Force
```

Without `-Force`, the script will prompt for confirmation before deleting resources. This is a destructive operation — all data will be lost.

---

## 8. Script Reference

| Script | Purpose | Usage |
|---|---|---|
| `run-local.ps1` | Start both apps for local development | `./output/Infra/scripts/run-local.ps1` |
| `create-infra.ps1` | Provision Azure infrastructure (Bicep) | `./output/Infra/scripts/create-infra.ps1 -Environment staging` |
| `configure-identity.ps1` | Set up managed identity DB access | `./output/Infra/scripts/configure-identity.ps1 -Environment staging` |
| `deploy-apps.ps1` | Deploy application code to Azure | `./output/Infra/scripts/deploy-apps.ps1 -Environment staging` |
| `delete-infra.ps1` | Delete Azure resources | `./output/Infra/scripts/delete-infra.ps1 -Environment staging -Force` |

All scripts:
- Are idempotent and safe to re-run after partial completion
- Include retry logic for transient errors (3 attempts with 15s backoff)
- Require PowerShell 7.6 or later
- Accept `-Environment` parameter (Development, Staging, Production)

---

## 9. Troubleshooting

### API returns 503 Service Unavailable

**Cause**: The API App is still starting up — migrations or seeding have not completed.

**Resolution**: Wait 30–60 seconds and check `/ready` again. The API uses retry logic with backoff during startup. If the issue persists, check Application logs in Azure Portal.

### API fails to connect to database after deployment

**Cause**: `configure-identity.ps1` was not run before `deploy-apps.ps1`, so the managed identity SQL user does not exist.

**Resolution**: Run `configure-identity.ps1` for the environment, then restart the API App:

```powershell
./output/Infra/scripts/configure-identity.ps1 -Environment staging
az webapp restart --name app-expenses-api-staging --resource-group rg-expenses-staging
```

### SQL user creation fails in configure-identity.ps1

**Cause**: None of the automated SQL connection methods are available.

**Resolution**: The script prints manual SQL instructions as a fallback. Execute the SQL commands in the Azure Portal SQL Query Editor.

### Web App shows "service unavailable" or warming-up message

**Cause**: The Web App started before the API App is ready.

**Resolution**: This is expected behaviour. The Web App handles API unavailability gracefully and will recover automatically once the API `/ready` endpoint returns `200 OK`.

### EF Core migrations create no tables (empty database)

**Cause**: Initial migration was not generated before deployment.

**Resolution**: Ensure the initial migration exists:

```powershell
cd output/Src/Expenses.Api
dotnet ef migrations add InitialCreate
```

Then redeploy with `deploy-apps.ps1`.

### Port conflicts on local development

**Cause**: Ports 5200 or 5201 are already in use.

**Resolution**: Stop any processes using those ports, or check for zombie processes from previous runs. The E2E test teardown should kill processes with `entireProcessTree: true` to prevent this.

### Bicep deployment fails

**Cause**: Missing Azure CLI login, insufficient permissions, or Azure policy violation.

**Resolution**:

```powershell
az login
az account set --subscription <subscription-id>
```

Verify the account has Contributor role on the target subscription.

### Bicep deployment fails with policy violation (RequestDisallowedByPolicy)

**Cause**: MCAPS governance policies require Azure AD-only authentication on SQL Server. This happens if the SQL Server resource is created without `azureADOnlyAuthentication: true` and a valid Azure AD administrator.

**Resolution**: The `create-infra.ps1` script automatically resolves the deployer's Azure AD identity as the SQL admin. Ensure you are logged in with `az login` (not using `--use-device-code` with a non-AAD account). Verify your identity:

```powershell
az account show --query user
```

If using a service principal in CI/CD, ensure the principal has an Azure AD object ID that can be resolved via `az ad sp show`.

### GitHub Actions workflow fails with OIDC error

**Cause**: Federated credentials are not configured correctly.

**Resolution**: Verify:
1. The federated credential entity type matches (Environment, Branch, or Tag)
2. The environment name matches exactly (case-sensitive)
3. `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` variables are set correctly
4. The app registration has the required role assignment on the subscription
