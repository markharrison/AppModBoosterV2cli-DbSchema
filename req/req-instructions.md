# Implementation Instructions

## Policy Compliance

All generated infrastructure code (Bicep, scripts, workflows) **must** comply with the constraints in `req-policy-constraints.md`. That file is mandatory reading before generating any IaC.

## Application Name

The canonical application name prefix is `expenses`. This value must be the default for every `$AppName` / `app-name` parameter across all scripts, workflows, and OIDC setup. Never use a different default.

## Application Config

The `Web App` must be configured with the `API App` base URL so it can send requests to the API.

For environments: `Development` only:

- The `Web App` runs on port 5200. The `API App` runs on port 5201.
- These ports must be defined consistently in launchSettings.json, appsettings.Development.json, local run scripts, and documentation.

For environments: `Staging` and `Production` only:

- The `Web App` must get the `API App` base URL through configuration.
- IaC and deployment scripts must set the required environment variable or app setting so the `Web App` can resolve the configuration.
- Connection strings in `appsettings.{env}.json` are overridden by Azure App Settings at runtime.
- Set DisableHttpsRedirect to "true" - (in Azure App Service, HTTPS is terminated before the app, so the app receives HTTP internally).

## Application Resilience

Database migrations and seeding run on `API App` startup. The startup process must use retry logic with backoff, with at least 5 attempts, to handle cloud cold-start timing where the SQL Database or managed identity grant may not yet be available.

The `API App` must expose separate unauthenticated liveness and readiness endpoints.
`/live` - Liveness must confirm only that the process is running and can respond to HTTP requests. If the API is not ready, a normal data request should return error: 503 Service Unavailable.
`/ready` - Readiness must confirm that the API App is able to serve normal traffic, and must report ready only after startup work has completed successfully, including required database migrations, required seeding, and required database connectivity.
The solution must tolerate independent startup of the Web App and API App, and the Web App must handle temporary API unavailability or not-ready states gracefully by showing a clear temporary unavailable or warming-up experience instead of failing.

## Routing

All server-generated URLs and redirects must use **lowercase routes**. Configure the web framework accordingly. URL matching in tests and external tools is often case-sensitive.

Controller-based REST API routes must use **plural** resource names (e.g. `/api/users` not `/api/user`). Ensure the controller class names match the intended plural route — `[Route("api/[controller]")]` derives the route from the class name minus the "Controller" suffix.

## API JSON Serialization

API controllers must NEVER return EF Core entity objects directly as JSON. EF Core's change tracker auto-populates reverse-navigation collections (e.g. `User.Expenses`, `Category.Expenses`), which causes `System.Text.Json` to produce exponentially large responses — even with `ReferenceHandler.IgnoreCycles`. Either use dedicated DTO/response classes, or apply `[JsonIgnore]` to ALL `ICollection<T>` reverse-navigation properties on every EF model. Validate by checking that API list endpoints return responses under 100 KB with seeded data.

## Database Seeding Compatibility

Seed data that specifies explicit primary key values must work on **both SQLite and SQL Server**. SQL Server uses identity columns that reject explicit inserts unless `SET IDENTITY_INSERT [TableName] ON` is executed first. Seed code must detect the database provider (e.g. `context.Database.IsSqlServer()`) and wrap each table's seed inserts in an explicit transaction (`BeginTransactionAsync`) with `IDENTITY_INSERT ON` before and `OFF` after `SaveChangesAsync` — the transaction ensures all statements use the same connection.

## Application Testing

Ensure high level of confidence the generated solution works. Do proper testing:

- Validate `API App` is working using unit tests.

- Validate `Web App` + `API App` end-to-end using Playwright. Must spin up both apps and use a real database. Use `Development` mode.

For Playwright testing - just use the Chromium browser. No other browsers testing needed.

### API Verification

- Confirm `/live` and `/ready` return healthy responses
- Confirm all GET endpoints return seeded data and that list endpoint responses are a reasonable size (under 100 KB) — if a response is megabytes, there is a JSON serialization explosion from EF navigation properties
- Confirm all POST endpoints accept valid payloads and return 201/200 (test at least one create operation with a real HTTP POST)
- Confirm all PUT endpoints accept valid payloads and return 204/200 (test at least one update operation with a real HTTP PUT)
- Verify that invalid or missing required fields return appropriate error responses (400), not 500
- After deploying to Azure, verify `/live` and `/ready` return 200 and at least one GET endpoint returns seeded data — startup crashes (e.g. seeding failures) only surface in Azure, not in local SQLite testing

### Web App Verification

- Confirm all list/index pages render with data from the API
- Confirm all create forms submit successfully and the new record appears in the list (use browser automation or form POST, not just page load)
- Confirm all edit forms load with pre-populated data and save successfully

### Testing Rules

- Do not rely solely on unit tests — unit tests bypass ASP.NET model validation, middleware, and Program.cs startup logic
- Do not rely solely on GET requests — write operations (POST/PUT) are where most integration bugs hide
- Every user-facing form action (create, edit, delete) must be verified end-to-end at least once
- E2E tests must be written AFTER inspecting the actual running application — never write Playwright selectors, assertions, or expected text against assumed HTML. Start the apps, fetch the real pages, and base all tests on what is actually rendered.
- All test suites (unit AND E2E) must be executed and passing before the task is considered complete
- E2E test architecture requirements (sequential execution, health checks, navigation waits) are defined in `req-techstack.md` under "E2E Test Architecture" — these must be followed

## Scripts

### Required Scripts

The following scripts should include:

- Create infrastructure
- Handle any identity configuration
- Delete infrastructure
- Deploy applications
- Run all applications for local development

### Script Robustness

Ensure PowerShell scripts are robust — they must be idempotent and safe to rerun after partial completion.

Include retry logic for transient errors. Use retry loops (for example 3 attempts with 15s backoff).

Scripts executing SQL against Azure SQL must try multiple connection methods as fallbacks (Invoke-Sqlcmd → sqlcmd → .NET SqlConnection → Microsoft.Data.SqlClient) since not all environments have the same tools installed.

Destructive operations should require use confirmation unless parameter `-Force` is specified.

Do NOT use `ValidateSet` on the `$Environment` parameter — interactive prompts can include trailing whitespace that fails strict validation. Instead, `.Trim().ToLower()` the input and validate manually in the script body.

## Environments

Use only `Development`, `Staging`, and `Production` for all environment names everywhere, including GitHub environments and OIDC subjects.

## GitHub Workflows

Use only currently supported GitHub Action versions.
Do not use Actions that depend on deprecated Node.js runtimes.
Validate that the final workflow YAML is valid before finishing.
Verify that all default parameter values in workflows match the defaults in the corresponding PowerShell scripts they invoke.

## CDN

With CDN `<script>` or `<link>` tags, do NOT include integrity attributes. Incorrect hashes will cause browsers to block the resource.

## Documentation

### Legacy Application Analysis

Should include an inventory of all discovered artifacts, including but not limited to tables, forms, reports, queries, macros, modules, business logic, and supporting docs.

### Specification of the new modernised application

Use the Legacy Application Analysis document to create a specification of the new modernised application.
