using Expenses.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expenses.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusesController : ControllerBase
{
    private readonly ExpensesDbContext _context;

    public StatusesController(ExpensesDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var statuses = await _context.ExpenseStatuses.ToListAsync();
        return Ok(statuses);
    }
}
