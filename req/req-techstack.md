# Requirement - Tech Stack

## Application

- Language: C#
- Frameworks: .NET 10, ASP.NET Core, EF Core, Razor views

### Web App

- HTML / CSS / JavaScript
- Bootstrap (CSS / JS) Framework

### API App

- Controller-based REST APIs

For environments: `Development` and `Staging` only - enable API documentation UI using Scalar (Swashbuckle is incompatible with .NET 10; use the built-in `AddOpenApi()` with `Scalar.AspNetCore`)

## Database

For environments: `Development` only - database is SQLite

For environments: `Staging` and `Production` only - database is Azure SQL Database

- EF Core to support code-first migrations
- SQLite (Development): use `EnsureCreatedAsync()` for schema creation — do NOT use `MigrateAsync()` with SQLite unless migrations exist
- SQL Server (Staging/Production): use EF Core `MigrateAsync()` for schema creation — **an initial migration MUST be generated** (e.g. `dotnet ef migrations add InitialCreate`) before deployment, otherwise `MigrateAsync()` creates no tables
- **Migration provider mismatch**: Since Development uses SQLite and Staging/Production use SQL Server, migrations MUST be generated targeting SQL Server. Add an `IDesignTimeDbContextFactory<TContext>` that configures `UseSqlServer(...)` so that `dotnet ef migrations add` always produces SQL Server column types (`int`, `nvarchar`, `bit`, `SqlServer:Identity`). Without this factory, EF Core generates SQLite types (`INTEGER`, `TEXT`, `Sqlite:Autoincrement`) that cause runtime failures on SQL Server.

## Infrastructure

Must support both local development and Azure cloud deployment.

### Local Development

Local development is using the `Development` environment.

Compute: developer PC

Database: 1 × SQLite

Authentication:

- No authentication between Web App and API App
- Uses SQLite; no SQL authentication required

### Cloud Deployment

Cloud deployment is using the `Staging` and `Production` environments.

Azure resource naming format: `{resourcetype}-{appname}-{env}`

Compute: 1 × Azure Linux App Service Plan (default `B1` but overridable)

- All web applications share the same plan
- Location: default `uksouth` but overridable

Database: 1 × Azure SQL Database (Basic tier)

Authentication:

- Authentication between Web App and API App using managed identity (Microsoft Entra ID)
- API App authenticates to Azure SQL Database using managed identity (Microsoft Entra ID)

### SQL Setup Scripts

Scripts that execute SQL against Azure SQL Database (e.g. creating managed identity users) must try **multiple connection methods** as fallbacks — not all environments have the same tools installed:

1. `Invoke-Sqlcmd` (SqlServer PowerShell module)
2. `sqlcmd` CLI tool
3. Direct .NET `System.Data.SqlClient.SqlConnection` with access token
4. `Microsoft.Data.SqlClient` from NuGet cache

If all automated methods fail, print clear instructions for Azure Portal Query Editor as the manual fallback.

### Developer Platform

GitHub Repos for storing code, documents, etc.
GitHub Workflows/Actions for CI/CD

Authentication:

- Use GitHub Actions OIDC federated credentials with `azure/login` (provide `client-id`, `tenant-id`, `subscription-id`)
- Do not use credentials secrets

## Scripts

For environments: `Development` only - simple script is needed to spin up applications. Set ASPNETCORE_ENVIRONMENT = "Development" .

For environments: `Staging` and `Production` only - Infrastructure-as-Code (IaC) + scripts are needed for to manage infrastructure and deploy application updates

- Scripts should be designed to be run from developer PC or via GitHub Workflows

- IaC to use Bicep templates

- Scripts must be PowerShell 7.6 (or later) — do not use Bash.

## Testing

For testing use the following frameworks and tooling:

- NUnit (test framework)
- Playwright (end-to-end browser testing)

### E2E Test Architecture

E2E tests spin up both the API and Web apps as child processes and share a single Playwright browser instance. This architecture requires:

- **Sequential execution** - Add `[assembly: NonParallelizable]` to the E2E test project so tests run one at a time. These tests share the same localhost API, Web App, and browser, so parallel execution can cause race conditions and `CONNECTION_REFUSED` failures.
- **App health verification** - In each test's `[SetUp]`, call the Web App health endpoint, such as `/live`, with a short timeout before the test continues. If a previous test crashed the app, fail immediately with a clear error instead of waiting for a Playwright timeout.
- **Browser state isolation** - The E2E suite may share a single Playwright browser instance, but each test must create and dispose its own browser context and page. Do not share cookies, local storage, session storage, or page instances across tests.
- **Proper navigation waits** - After submitting a form, wait for the full submit flow to finish before asserting: POST, redirect, and the final page load. This matters most when the redirect goes back to the same URL, because waiting only for a URL change can succeed too early. Use a wait that confirms both the submit response and that the destination page has fully loaded or re-rendered.
- **Browser event handlers** - Register browser event handlers, such as dialog or confirm handlers, only once per page instance. Registering the same handler multiple times can crash the whole test run, not just one test.
- **Test data independence** - Any test that changes data, such as submit, approve, or reject, must create its own test data. Do not rely on shared seed data, because earlier tests may already have changed it.
- **Process cleanup** - In `[OneTimeTearDown]`, dispose Playwright resources first, then kill spawned child processes with `entireProcessTree: true` so later test runs do not fail because ports are still in use.

## Validation

After generating all code, verify:

- Solution builds with zero errors and zero warnings
- All unit tests pass
- API App starts in Development and `/live` and `/ready` endpoints return success
- Seeded data is accessible via API endpoints
- Web App starts and renders pages with data from the API
- All NuGet packages are compatible with the target .NET version — check for runtime `TypeLoadException` or missing type errors
- EF Core initial migration has been generated (`dotnet ef migrations add InitialCreate`) — without this, `MigrateAsync()` in Staging/Production creates no tables and the app starts with an empty database
- Deployment documentation lists scripts in the correct execution order — SQL user setup must come before app deployment (the API uses managed identity auth and cannot start without DB permissions)

---
