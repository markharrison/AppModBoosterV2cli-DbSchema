using Expenses.Api.Data;
using Expenses.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expenses.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly ExpensesDbContext _context;

    public ExpensesController(ExpensesDbContext context) => _context = context;

    // GET /api/expenses?userId={id}
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? userId)
    {
        var query = _context.Expenses
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Status)
            .Include(e => e.Reviewer)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        var expenses = await query.ToListAsync();
        return Ok(expenses);
    }

    // GET /api/expenses/pending-approvals?managerId={id}
    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] int managerId)
    {
        var directReportIds = await _context.Users
            .Where(u => u.ManagerId == managerId)
            .Select(u => u.UserId)
            .ToListAsync();

        var expenses = await _context.Expenses
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Status)
            .Where(e => directReportIds.Contains(e.UserId) && e.Status.StatusName == "Submitted")
            .ToListAsync();

        return Ok(expenses);
    }

    // GET /api/expenses/summary?userId={id}
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int userId)
    {
        var expenses = await _context.Expenses
            .Include(e => e.Status)
            .Where(e => e.UserId == userId)
            .ToListAsync();

        var summary = new ExpenseSummaryResponse
        {
            Draft = expenses.Count(e => e.Status.StatusName == "Draft"),
            Submitted = expenses.Count(e => e.Status.StatusName == "Submitted"),
            Approved = expenses.Count(e => e.Status.StatusName == "Approved"),
            Rejected = expenses.Count(e => e.Status.StatusName == "Rejected")
        };

        return Ok(summary);
    }

    // GET /api/expenses/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var expense = await _context.Expenses
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Status)
            .Include(e => e.Reviewer)
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

        if (expense == null)
            return NotFound();

        return Ok(expense);
    }

    // POST /api/expenses
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return BadRequest(new { error = "User not found." });

        var category = await _context.ExpenseCategories.FindAsync(request.CategoryId);
        if (category == null)
            return BadRequest(new { error = "Category not found." });

        var draftStatus = await _context.ExpenseStatuses.FirstOrDefaultAsync(s => s.StatusName == "Draft");
        if (draftStatus == null)
            return StatusCode(500, new { error = "Draft status not found in database." });

        var expense = new Expense
        {
            UserId = request.UserId,
            CategoryId = request.CategoryId,
            StatusId = draftStatus.StatusId,
            AmountMinor = request.AmountMinor,
            Currency = request.Currency ?? "GBP",
            ExpenseDate = request.ExpenseDate,
            Description = request.Description,
            ReceiptFile = request.ReceiptFile,
            CreatedAt = DateTime.UtcNow
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        // Reload with navigations
        var created = await _context.Expenses
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Status)
            .FirstAsync(e => e.ExpenseId == expense.ExpenseId);

        return CreatedAtAction(nameof(GetById), new { id = created.ExpenseId }, created);
    }

    // PUT /api/expenses/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var expense = await _context.Expenses
            .Include(e => e.Status)
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Reviewer)
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

        if (expense == null)
            return NotFound();

        if (expense.Status.StatusName != "Draft" && expense.Status.StatusName != "Rejected")
            return BadRequest(new { error = "Only Draft or Rejected expenses can be updated." });

        // Apply updates
        if (request.CategoryId.HasValue)
        {
            var category = await _context.ExpenseCategories.FindAsync(request.CategoryId.Value);
            if (category == null)
                return BadRequest(new { error = "Category not found." });
            expense.CategoryId = request.CategoryId.Value;
        }

        if (request.AmountMinor.HasValue)
            expense.AmountMinor = request.AmountMinor.Value;

        if (request.Currency != null)
            expense.Currency = request.Currency;

        if (request.ExpenseDate.HasValue)
            expense.ExpenseDate = request.ExpenseDate.Value;

        if (request.Description != null)
            expense.Description = request.Description;

        if (request.ReceiptFile != null)
            expense.ReceiptFile = request.ReceiptFile;

        await _context.SaveChangesAsync();

        return Ok(expense);
    }

    // PUT /api/expenses/{id}/submit
    [HttpPut("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var expense = await _context.Expenses
            .Include(e => e.Status)
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Reviewer)
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

        if (expense == null)
            return NotFound();

        if (expense.Status.StatusName != "Draft" && expense.Status.StatusName != "Rejected")
            return BadRequest(new { error = "Only Draft or Rejected expenses can be submitted." });

        var submittedStatus = await _context.ExpenseStatuses.FirstOrDefaultAsync(s => s.StatusName == "Submitted");
        if (submittedStatus == null)
            return StatusCode(500, new { error = "Submitted status not found in database." });

        expense.StatusId = submittedStatus.StatusId;
        expense.SubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload status navigation
        await _context.Entry(expense).Reference(e => e.Status).LoadAsync();

        return Ok(expense);
    }

    // PUT /api/expenses/{id}/approve
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromQuery] int managerId)
    {
        var expense = await _context.Expenses
            .Include(e => e.Status)
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Reviewer)
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

        if (expense == null)
            return NotFound();

        if (expense.Status.StatusName != "Submitted")
            return BadRequest(new { error = "Only Submitted expenses can be approved." });

        // Verify the manager is the owner's manager
        if (expense.User.ManagerId != managerId)
            return StatusCode(403, new { error = "Only the manager of the expense owner can approve." });

        var approvedStatus = await _context.ExpenseStatuses.FirstOrDefaultAsync(s => s.StatusName == "Approved");
        if (approvedStatus == null)
            return StatusCode(500, new { error = "Approved status not found in database." });

        expense.StatusId = approvedStatus.StatusId;
        expense.ReviewedBy = managerId;
        expense.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _context.Entry(expense).Reference(e => e.Status).LoadAsync();
        await _context.Entry(expense).Reference(e => e.Reviewer).LoadAsync();

        return Ok(expense);
    }

    // PUT /api/expenses/{id}/reject
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromQuery] int managerId, [FromBody] RejectExpenseRequest? request)
    {
        var expense = await _context.Expenses
            .Include(e => e.Status)
            .Include(e => e.User)
            .Include(e => e.Category)
            .Include(e => e.Reviewer)
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

        if (expense == null)
            return NotFound();

        if (expense.Status.StatusName != "Submitted")
            return BadRequest(new { error = "Only Submitted expenses can be rejected." });

        if (expense.User.ManagerId != managerId)
            return StatusCode(403, new { error = "Only the manager of the expense owner can reject." });

        var rejectedStatus = await _context.ExpenseStatuses.FirstOrDefaultAsync(s => s.StatusName == "Rejected");
        if (rejectedStatus == null)
            return StatusCode(500, new { error = "Rejected status not found in database." });

        expense.StatusId = rejectedStatus.StatusId;
        expense.ReviewedBy = managerId;
        expense.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _context.Entry(expense).Reference(e => e.Status).LoadAsync();
        await _context.Entry(expense).Reference(e => e.Reviewer).LoadAsync();

        return Ok(expense);
    }

    // DELETE /api/expenses/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int userId)
    {
        var expense = await _context.Expenses
            .Include(e => e.Status)
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

        if (expense == null)
            return NotFound();

        if (expense.UserId != userId)
            return StatusCode(403, new { error = "Only the owner can delete an expense." });

        if (expense.Status.StatusName != "Draft")
        {
            if (expense.Status.StatusName == "Approved" || expense.Status.StatusName == "Rejected")
                return BadRequest(new { error = $"{expense.Status.StatusName} expenses cannot be deleted." });
            return BadRequest(new { error = "Only Draft expenses can be deleted." });
        }

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
