# Testing Guide — Expense Management System

---

## 1. Testing Strategy Overview

The Expense Management System uses a two-tier testing strategy:

| Tier | Project | Framework | Purpose |
|---|---|---|---|
| Unit Tests | `Expenses.Api.Tests` | NUnit | Validate API controllers, business rules, and validation logic |
| E2E Tests | `Expenses.Web.E2E` | NUnit + Playwright | Validate full user workflows through the browser |

Both tiers are required for confidence in the generated solution. Unit tests alone are insufficient — they bypass ASP.NET model validation, middleware, and `Program.cs` startup logic. E2E tests validate the complete stack.

---

## 2. Unit Tests — `Expenses.Api.Tests`

### 2.1 How to Run

```powershell
dotnet test output/Src/Expenses.Api.Tests
```

### 2.2 Framework

- **NUnit** test framework
- Tests target the `Expenses.Api` project

### 2.3 What They Cover

| Area | Tests |
|---|---|
| **Health endpoints** | `/live` returns 200 OK; `/ready` returns 200 when ready, 503 when not |
| **GET endpoints** | All endpoints return seeded data (roles, users, categories, statuses, expenses) |
| **POST operations** | `POST /api/expenses` accepts valid payloads and returns 201 Created |
| **PUT operations** | `PUT /api/expenses/{id}` accepts valid payloads and returns 200/204 |
| **Validation** | Invalid or missing required fields return 400 Bad Request (not 500) |
| **Business rules** | Status transition enforcement (e.g. only Draft expenses can be submitted) |
| **Ownership rules** | Only the expense owner can edit/submit/delete; only the manager can approve/reject |
| **Delete restrictions** | Only Draft expenses can be deleted; Approved/Rejected return 400 |

### 2.4 Key Testing Principles

- Do not rely solely on unit tests — they bypass middleware and model binding
- Every write operation (POST/PUT/DELETE) must be explicitly tested
- Invalid input tests verify the API returns 400 with a descriptive message, not 500
- Status transition rules are tested for both valid and invalid transitions

---

## 3. E2E Tests — `Expenses.Web.E2E`

### 3.1 Prerequisites

Install Playwright browsers before running E2E tests:

```powershell
dotnet build output/Src/Expenses.Web.E2E
pwsh output/Src/Expenses.Web.E2E/bin/Debug/net10.0/playwright.ps1 install
```

### 3.2 How to Run

```powershell
dotnet test output/Src/Expenses.Web.E2E
```

### 3.3 Framework

- **NUnit** test framework with **Playwright** for browser automation
- Tests run in **Development** mode with SQLite

### 3.4 Architecture

The E2E tests spin up both the API App and Web App as child processes and share a single Playwright browser instance:

```
┌──────────────────────────────┐
│       Test Runner (NUnit)     │
│                               │
│  [OneTimeSetUp]               │
│    ├─ Start API App (:5201)   │
│    ├─ Start Web App (:5200)   │
│    ├─ Wait for /ready         │
│    └─ Launch Playwright       │
│                               │
│  [Test] → Browser automation  │
│                               │
│  [OneTimeTearDown]            │
│    ├─ Close Playwright        │
│    └─ Kill child processes    │
│       (entireProcessTree)     │
└──────────────────────────────┘
```

### 3.5 Architecture Requirements

| Requirement | Implementation | Reason |
|---|---|---|
| **Sequential execution** | `[assembly: NonParallelizable]` | Parallel fixtures cause race conditions (CONNECTION_REFUSED) when multiple browser contexts hit the same localhost apps |
| **Health verification** | Each test's `[SetUp]` checks the Web App is responding | If an earlier test crashes the app, subsequent tests fail fast with a clear message instead of waiting for Playwright timeouts |
| **Navigation waits** | `WaitForURLAsync` after form submissions | POST → redirect → GET cycles need URL-based waits, not just `WaitForLoadStateAsync`, to ensure the redirect completed before asserting |
| **Test data independence** | Tests that modify data create their own records | Earlier tests may have already modified seed data; shared seed data is not reliable for mutation tests |
| **Process cleanup** | `[OneTimeTearDown]` kills with `entireProcessTree: true` | Prevents port conflicts (5200/5201) on subsequent test runs |

### 3.6 What They Cover

| Area | Tests |
|---|---|
| **Page rendering** | All list/index pages render with data from the API |
| **Create workflows** | Create expense form submits successfully and the new record appears in the list |
| **Edit workflows** | Edit forms load with pre-populated data and save successfully |
| **Submit workflow** | Draft expenses can be submitted for approval |
| **Approve/Reject** | Manager can approve or reject submitted expenses |
| **Delete workflow** | Draft expenses can be deleted |
| **User switching** | User selector switches application context |
| **Manager views** | Pending Approvals page shows submitted expenses from direct reports |

### 3.7 Important: Tests Are Written Against Real HTML

E2E tests are written AFTER inspecting the actual running application. Playwright selectors, assertions, and expected text are based on what the app actually renders — not assumed HTML structure.

---

## 4. Test Coverage Summary

### 4.1 API Layer

| Endpoint | GET | POST | PUT | DELETE | Validation |
|---|---|---|---|---|---|
| `/live`, `/ready` | ✅ | — | — | — | — |
| `/api/roles` | ✅ | — | — | — | — |
| `/api/users` | ✅ | — | — | — | — |
| `/api/categories` | ✅ | — | — | — | — |
| `/api/statuses` | ✅ | — | — | — | — |
| `/api/expenses` | ✅ | ✅ | ✅ | ✅ | ✅ (400 for invalid input) |
| `/api/expenses/{id}/submit` | — | — | ✅ | — | ✅ (status transition) |
| `/api/expenses/{id}/approve` | — | — | ✅ | — | ✅ (manager-only) |
| `/api/expenses/{id}/reject` | — | — | ✅ | — | ✅ (manager-only) |
| `/api/expenses/pending-approvals` | ✅ | — | — | — | — |
| `/api/expenses/summary` | ✅ | — | — | — | — |

### 4.2 Web App Layer

| Page | Renders | Create | Edit | Actions |
|---|---|---|---|---|
| Dashboard (`/`) | ✅ | — | — | — |
| My Expenses (`/expenses`) | ✅ | — | — | Submit, Delete |
| Create Expense (`/expenses/create`) | — | ✅ | — | — |
| Edit Expense (`/expenses/{id}/edit`) | — | — | ✅ | — |
| Pending Approvals (`/approvals`) | ✅ | — | — | Approve, Reject |

---

## 5. Writing New Tests

### 5.1 Adding Unit Tests

1. Add test classes to the `Expenses.Api.Tests` project
2. Use NUnit attributes: `[TestFixture]`, `[Test]`, `[SetUp]`, `[TearDown]`
3. Follow the existing pattern for arranging test data and asserting responses
4. Test both success and failure paths for every operation
5. Always test validation: verify 400 responses for invalid input, not just happy paths

Example structure:

```csharp
[TestFixture]
public class NewFeatureTests
{
    [SetUp]
    public void SetUp()
    {
        // Arrange test data
    }

    [Test]
    public async Task Should_ReturnExpectedResult_When_ValidInput()
    {
        // Act
        // Assert
    }

    [Test]
    public async Task Should_Return400_When_InvalidInput()
    {
        // Act
        // Assert 400 Bad Request with descriptive message
    }
}
```

### 5.2 Adding E2E Tests

1. Add test classes to the `Expenses.Web.E2E` project
2. Use the shared Playwright browser instance and app processes from the test setup
3. **Do NOT write selectors against assumed HTML** — start the apps, inspect the actual pages, then write selectors
4. Follow sequential execution — do not add `[Parallelizable]` attributes
5. Create your own test data for mutation tests (submit, approve, reject, delete)
6. Use `WaitForURLAsync` after form submissions, not just `WaitForLoadStateAsync`
7. Add health verification in `[SetUp]` to fail fast if apps are down

Example structure:

```csharp
[TestFixture]
public class NewWorkflowTests
{
    [SetUp]
    public async Task SetUp()
    {
        // Verify Web App is still responding
    }

    [Test]
    public async Task Should_CompleteWorkflow_Successfully()
    {
        // Navigate to page
        // Fill form
        // Submit and wait for redirect
        await Page.WaitForURLAsync("**/expenses");
        // Assert result
    }
}
```

### 5.3 Test Naming Conventions

Use descriptive test names that explain:
- What is being tested
- Under what conditions
- What the expected outcome is

Pattern: `Should_{ExpectedResult}_When_{Condition}`

---

## 6. CI Integration

### 6.1 GitHub Actions

Tests run automatically in the GitHub Actions CI workflow on:
- Push to main/staging branches
- Pull requests targeting main/staging branches

### 6.2 CI Workflow Steps

1. **Checkout** — clone the repository
2. **Setup .NET** — install .NET 10 SDK
3. **Restore** — `dotnet restore`
4. **Build** — `dotnet build` (zero errors, zero warnings)
5. **Unit Tests** — `dotnet test Expenses.Api.Tests`
6. **Install Playwright** — install required browsers
7. **E2E Tests** — `dotnet test Expenses.Web.E2E`

### 6.3 CI Requirements

- All unit tests AND E2E tests must pass before merge
- The solution must build with zero errors and zero warnings
- E2E tests run in Development mode with SQLite

---

## 7. Troubleshooting Tests

### Unit tests fail with TypeLoadException

**Cause**: NuGet package incompatibility with .NET 10.

**Resolution**: Check all packages are compatible with the target .NET version. Update any outdated packages.

### E2E tests fail with CONNECTION_REFUSED

**Cause**: Apps failed to start, or a previous test run left zombie processes holding ports 5200/5201.

**Resolution**:
1. Kill any processes using ports 5200 and 5201
2. Ensure `[OneTimeTearDown]` kills with `entireProcessTree: true`
3. Re-run the tests

### E2E tests fail waiting for elements

**Cause**: Selectors don't match the actual HTML rendered by the app.

**Resolution**: Start the apps locally, inspect the pages in a browser, and update selectors to match the real DOM structure.

### E2E tests are flaky on form submissions

**Cause**: Using `WaitForLoadStateAsync` instead of `WaitForURLAsync` for POST → redirect → GET cycles.

**Resolution**: Replace `WaitForLoadStateAsync` with `WaitForURLAsync` to ensure the redirect has fully completed before asserting on page content.

### Playwright browsers not installed

**Cause**: Browsers need to be installed separately after building the E2E project.

**Resolution**:

```powershell
dotnet build output/Src/Expenses.Web.E2E
pwsh output/Src/Expenses.Web.E2E/bin/Debug/net10.0/playwright.ps1 install
```
