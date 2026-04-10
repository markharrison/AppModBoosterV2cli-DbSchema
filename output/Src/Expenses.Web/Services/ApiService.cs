using System.Net.Http.Json;
using Expenses.Web.Models;

namespace Expenses.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    // Roles
    public async Task<List<RoleViewModel>> GetRolesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<RoleViewModel>>("api/roles") ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    // Users
    public async Task<List<UserViewModel>> GetUsersAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserViewModel>>("api/users") ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    public async Task<UserViewModel?> GetUserAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<UserViewModel>($"api/users/{id}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // Categories
    public async Task<List<ExpenseCategoryViewModel>> GetCategoriesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ExpenseCategoryViewModel>>("api/categories") ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    // Statuses
    public async Task<List<ExpenseStatusViewModel>> GetStatusesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ExpenseStatusViewModel>>("api/statuses") ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    // Expenses
    public async Task<List<ExpenseViewModel>> GetExpensesAsync(int userId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ExpenseViewModel>>($"api/expenses?userId={userId}") ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    public async Task<ExpenseViewModel?> GetExpenseAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ExpenseViewModel>($"api/expenses/{id}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ExpenseViewModel?> CreateExpenseAsync(int userId, int categoryId, int amountMinor, DateOnly expenseDate, string? description)
    {
        try
        {
            var request = new
            {
                UserId = userId,
                CategoryId = categoryId,
                AmountMinor = amountMinor,
                Currency = "GBP",
                ExpenseDate = expenseDate,
                Description = description
            };
            var response = await _http.PostAsJsonAsync("api/expenses", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ExpenseViewModel>();
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> UpdateExpenseAsync(int id, int? categoryId, int? amountMinor, DateOnly? expenseDate, string? description)
    {
        try
        {
            var request = new
            {
                CategoryId = categoryId,
                AmountMinor = amountMinor,
                ExpenseDate = expenseDate,
                Description = description
            };
            var response = await _http.PutAsJsonAsync($"api/expenses/{id}", request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int id)
    {
        try
        {
            var response = await _http.PutAsync($"api/expenses/{id}/submit", null);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int id, int managerId)
    {
        try
        {
            var response = await _http.PutAsync($"api/expenses/{id}/approve?managerId={managerId}", null);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> RejectExpenseAsync(int id, int managerId, string? reason)
    {
        try
        {
            var request = new { Reason = reason };
            var response = await _http.PutAsJsonAsync($"api/expenses/{id}/reject?managerId={managerId}", request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int id, int userId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/expenses/{id}?userId={userId}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    // Pending Approvals
    public async Task<List<ExpenseViewModel>> GetPendingApprovalsAsync(int managerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ExpenseViewModel>>($"api/expenses/pending-approvals?managerId={managerId}") ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    // Summary
    public async Task<ExpenseSummaryViewModel?> GetExpenseSummaryAsync(int userId)
    {
        try
        {
            return await _http.GetFromJsonAsync<ExpenseSummaryViewModel>($"api/expenses/summary?userId={userId}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // Health
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _http.GetAsync("ready");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
