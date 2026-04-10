using Expenses.Web.Models;
using Expenses.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Expenses.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiService _api;

    public HomeController(ApiService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (userId == null)
        {
            // Try to set default user
            var users = await _api.GetUsersAsync();
            if (users.Count == 0)
            {
                return View(new DashboardViewModel { ApiAvailable = false });
            }
            userId = users[0].UserId;
            HttpContext.Session.SetInt32("CurrentUserId", userId.Value);
        }

        var user = await _api.GetUserAsync(userId.Value);
        if (user == null)
        {
            return View(new DashboardViewModel { ApiAvailable = false });
        }

        var summary = await _api.GetExpenseSummaryAsync(userId.Value);
        var model = new DashboardViewModel
        {
            Summary = summary ?? new ExpenseSummaryViewModel(),
            CurrentUser = user,
            IsManager = user.IsManager,
            ApiAvailable = true
        };

        if (user.IsManager)
        {
            var pending = await _api.GetPendingApprovalsAsync(userId.Value);
            model.PendingApprovalCount = pending.Count;
        }

        return View(model);
    }
}
