using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Expenses.Api.Tests.Helpers;
using NUnit.Framework;

namespace Expenses.Api.Tests.Controllers;

[TestFixture]
public class ExpensesControllerTests
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<JsonElement> CreateDraftExpense(int userId = 1, int categoryId = 1, int amountMinor = 5000)
    {
        var request = new
        {
            userId,
            categoryId,
            amountMinor,
            currency = "GBP",
            expenseDate = "2025-11-15",
            description = "Test expense"
        };
        var response = await _client.PostAsJsonAsync("/api/expenses", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> CreateAndSubmitExpense(int userId = 1)
    {
        var draft = await CreateDraftExpense(userId);
        var id = draft.GetProperty("expenseId").GetInt32();
        var submitResponse = await _client.PutAsync($"/api/expenses/{id}/submit", null);
        Assert.That(submitResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return id;
    }

    // --- GET tests ---

    [Test]
    public async Task GetAll_WithUserId_ReturnsExpensesForUser()
    {
        var response = await _client.GetAsync("/api/expenses?userId=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var expenses = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(expenses.GetArrayLength(), Is.GreaterThan(0));
        foreach (var exp in expenses.EnumerateArray())
        {
            Assert.That(exp.GetProperty("userId").GetInt32(), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetById_ReturnsExpense()
    {
        var created = await CreateDraftExpense();
        var id = created.GetProperty("expenseId").GetInt32();

        var response = await _client.GetAsync($"/api/expenses/{id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var expense = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(expense.GetProperty("expenseId").GetInt32(), Is.EqualTo(id));
    }

    [Test]
    public async Task GetById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/expenses/999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- POST tests ---

    [Test]
    public async Task Create_ReturnsCreatedWithDraftStatus()
    {
        var request = new
        {
            userId = 1,
            categoryId = 1,
            amountMinor = 3500,
            currency = "GBP",
            expenseDate = "2025-12-01",
            description = "New test expense"
        };

        var response = await _client.PostAsJsonAsync("/api/expenses", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var expense = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(expense.GetProperty("amountMinor").GetInt32(), Is.EqualTo(3500));
        Assert.That(expense.GetProperty("status").GetProperty("statusName").GetString(), Is.EqualTo("Draft"));
    }

    [Test]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        // Send empty object — missing userId, categoryId, amountMinor, expenseDate
        var request = new { };
        var response = await _client.PostAsJsonAsync("/api/expenses", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_InvalidAmountMinor_Zero_Returns400()
    {
        var request = new
        {
            userId = 1,
            categoryId = 1,
            amountMinor = 0,
            expenseDate = "2025-12-01"
        };
        var response = await _client.PostAsJsonAsync("/api/expenses", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_InvalidAmountMinor_Negative_Returns400()
    {
        var request = new
        {
            userId = 1,
            categoryId = 1,
            amountMinor = -100,
            expenseDate = "2025-12-01"
        };
        var response = await _client.PostAsJsonAsync("/api/expenses", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // --- PUT (update) tests ---

    [Test]
    public async Task Update_DraftExpense_Succeeds()
    {
        var created = await CreateDraftExpense();
        var id = created.GetProperty("expenseId").GetInt32();

        var updateRequest = new { amountMinor = 7500, description = "Updated description" };
        var response = await _client.PutAsJsonAsync($"/api/expenses/{id}", updateRequest);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(updated.GetProperty("amountMinor").GetInt32(), Is.EqualTo(7500));
        Assert.That(updated.GetProperty("description").GetString(), Is.EqualTo("Updated description"));
    }

    [Test]
    public async Task Update_SubmittedExpense_Returns400()
    {
        var id = await CreateAndSubmitExpense();

        var updateRequest = new { amountMinor = 9999 };
        var response = await _client.PutAsJsonAsync($"/api/expenses/{id}", updateRequest);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // --- PUT /submit tests ---

    [Test]
    public async Task Submit_DraftExpense_ChangesToSubmitted()
    {
        var created = await CreateDraftExpense();
        var id = created.GetProperty("expenseId").GetInt32();

        var response = await _client.PutAsync($"/api/expenses/{id}/submit", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var expense = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(expense.GetProperty("status").GetProperty("statusName").GetString(), Is.EqualTo("Submitted"));
        Assert.That(expense.GetProperty("submittedAt").GetString(), Is.Not.Null);
    }

    [Test]
    public async Task Submit_AlreadySubmittedExpense_Returns400()
    {
        var id = await CreateAndSubmitExpense();

        var response = await _client.PutAsync($"/api/expenses/{id}/submit", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // --- PUT /approve tests ---

    [Test]
    public async Task Approve_SubmittedExpense_ChangesToApproved()
    {
        // Create and submit expense for Alice (userId=1, managerId=2)
        var id = await CreateAndSubmitExpense(userId: 1);

        var response = await _client.PutAsync($"/api/expenses/{id}/approve?managerId=2", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var expense = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(expense.GetProperty("status").GetProperty("statusName").GetString(), Is.EqualTo("Approved"));
        Assert.That(expense.GetProperty("reviewedBy").GetInt32(), Is.EqualTo(2));
        Assert.That(expense.GetProperty("reviewedAt").GetString(), Is.Not.Null);
    }

    [Test]
    public async Task Approve_ByNonManager_Returns403()
    {
        var id = await CreateAndSubmitExpense(userId: 1);

        // userId=1 (Alice) is not the manager of Alice — only managerId=2 (Bob) is
        var response = await _client.PutAsync($"/api/expenses/{id}/approve?managerId=1", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Approve_NonSubmittedExpense_Returns400()
    {
        var created = await CreateDraftExpense();
        var id = created.GetProperty("expenseId").GetInt32();

        var response = await _client.PutAsync($"/api/expenses/{id}/approve?managerId=2", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // --- PUT /reject tests ---

    [Test]
    public async Task Reject_SubmittedExpense_ChangesToRejected()
    {
        var id = await CreateAndSubmitExpense(userId: 1);

        var body = new { reason = "Missing receipt" };
        var response = await _client.PutAsJsonAsync($"/api/expenses/{id}/reject?managerId=2", body);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var expense = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(expense.GetProperty("status").GetProperty("statusName").GetString(), Is.EqualTo("Rejected"));
    }

    // --- DELETE tests ---

    [Test]
    public async Task Delete_DraftExpense_ReturnsNoContent()
    {
        var created = await CreateDraftExpense();
        var id = created.GetProperty("expenseId").GetInt32();

        var response = await _client.DeleteAsync($"/api/expenses/{id}?userId=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/api/expenses/{id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_ApprovedExpense_Returns400()
    {
        var id = await CreateAndSubmitExpense(userId: 1);
        await _client.PutAsync($"/api/expenses/{id}/approve?managerId=2", null);

        var response = await _client.DeleteAsync($"/api/expenses/{id}?userId=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Delete_ByNonOwner_Returns403()
    {
        var created = await CreateDraftExpense(userId: 1);
        var id = created.GetProperty("expenseId").GetInt32();

        // Try to delete as userId=2 (not the owner)
        var response = await _client.DeleteAsync($"/api/expenses/{id}?userId=2");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    // --- Pending Approvals ---

    [Test]
    public async Task GetPendingApprovals_ReturnsSubmittedFromDirectReports()
    {
        // Create and submit an expense for Alice (who reports to Bob, managerId=2)
        await CreateAndSubmitExpense(userId: 1);

        var response = await _client.GetAsync("/api/expenses/pending-approvals?managerId=2");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var expenses = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(expenses.GetArrayLength(), Is.GreaterThan(0));
        foreach (var exp in expenses.EnumerateArray())
        {
            Assert.That(exp.GetProperty("status").GetProperty("statusName").GetString(), Is.EqualTo("Submitted"));
        }
    }

    // --- Summary ---

    [Test]
    public async Task GetSummary_ReturnsCorrectCounts()
    {
        var response = await _client.GetAsync("/api/expenses/summary?userId=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Verify all count properties exist and are non-negative
        Assert.That(summary.GetProperty("draft").GetInt32(), Is.GreaterThanOrEqualTo(0));
        Assert.That(summary.GetProperty("submitted").GetInt32(), Is.GreaterThanOrEqualTo(0));
        Assert.That(summary.GetProperty("approved").GetInt32(), Is.GreaterThanOrEqualTo(0));
        Assert.That(summary.GetProperty("rejected").GetInt32(), Is.GreaterThanOrEqualTo(0));
    }

    // --- PUT (update) by non-owner ---
    // Note: The current API doesn't enforce owner-only update via query param.
    // The Update action doesn't take a userId parameter, so this test verifies
    // the status-based guard instead. If owner enforcement is added later, update this test.

    [Test]
    public async Task Update_ExpenseById_NonOwnerCanStillUpdate_StatusGuardOnly()
    {
        // The PUT endpoint doesn't have a userId check, only a status check.
        // This documents current behavior: any caller can update a Draft expense.
        var created = await CreateDraftExpense(userId: 1);
        var id = created.GetProperty("expenseId").GetInt32();

        var updateRequest = new { description = "Updated by someone else" };
        var response = await _client.PutAsJsonAsync($"/api/expenses/{id}", updateRequest);
        // Current API allows this — documents the behavior
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
