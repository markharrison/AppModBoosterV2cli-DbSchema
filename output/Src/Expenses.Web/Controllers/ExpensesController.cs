using Expenses.Web.Models;
using Expenses.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Expenses.Web.Controllers;

public class ExpensesController : Controller
{
    private readonly ApiService _api;

    public ExpensesController(ApiService api)
    {
        _api = api;
    }

    private int? GetCurrentUserId() => HttpContext.Session.GetInt32("CurrentUserId");

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Index", "Home");

        var expenses = await _api.GetExpensesAsync(userId.Value);
        return View(expenses);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _api.GetCategoriesAsync();
        var model = new CreateExpenseViewModel
        {
            Categories = categories
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid)
        {
            model.Categories = await _api.GetCategoriesAsync();
            return View(model);
        }

        var result = await _api.CreateExpenseAsync(
            userId.Value,
            model.CategoryId,
            model.AmountMinor,
            model.ExpenseDate,
            model.Description);

        if (result == null)
        {
            ModelState.AddModelError("", "Failed to create expense. Please try again.");
            model.Categories = await _api.GetCategoriesAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Expense created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("expenses/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var expense = await _api.GetExpenseAsync(id);
        if (expense == null) return NotFound();

        var categories = await _api.GetCategoriesAsync();
        var model = new EditExpenseViewModel
        {
            ExpenseId = expense.ExpenseId,
            CategoryId = expense.CategoryId,
            Amount = expense.AmountMinor / 100m,
            ExpenseDate = expense.ExpenseDate,
            Description = expense.Description,
            Categories = categories
        };
        return View(model);
    }

    [HttpPost("expenses/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditExpenseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _api.GetCategoriesAsync();
            return View(model);
        }

        var success = await _api.UpdateExpenseAsync(
            id,
            model.CategoryId,
            model.AmountMinor,
            model.ExpenseDate,
            model.Description);

        if (!success)
        {
            ModelState.AddModelError("", "Failed to update expense. Please try again.");
            model.Categories = await _api.GetCategoriesAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Expense updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("expenses/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var expense = await _api.GetExpenseAsync(id);
        if (expense == null) return NotFound();
        return View(expense);
    }

    [HttpPost("expenses/{id:int}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        await _api.SubmitExpenseAsync(id);
        TempData["SuccessMessage"] = "Expense submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("expenses/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Index", "Home");

        await _api.DeleteExpenseAsync(id, userId.Value);
        TempData["SuccessMessage"] = "Expense deleted.";
        return RedirectToAction(nameof(Index));
    }
}
