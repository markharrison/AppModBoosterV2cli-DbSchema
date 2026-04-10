using Expenses.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Expenses.Web.Controllers;

public class ApprovalsController : Controller
{
    private readonly ApiService _api;

    public ApprovalsController(ApiService api)
    {
        _api = api;
    }

    private int? GetCurrentUserId() => HttpContext.Session.GetInt32("CurrentUserId");

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Index", "Home");

        var pending = await _api.GetPendingApprovalsAsync(userId.Value);
        return View(pending);
    }

    [HttpPost("approvals/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Index", "Home");

        await _api.ApproveExpenseAsync(id, userId.Value);
        TempData["SuccessMessage"] = "Expense approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("approvals/{id:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Index", "Home");

        await _api.RejectExpenseAsync(id, userId.Value, reason);
        TempData["SuccessMessage"] = "Expense rejected.";
        return RedirectToAction(nameof(Index));
    }
}
