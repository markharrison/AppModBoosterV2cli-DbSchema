using Microsoft.AspNetCore.Mvc;

namespace Expenses.Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    public static bool IsReady { get; set; }

    [HttpGet("/live")]
    public IActionResult Live() => Ok(new { status = "alive" });

    [HttpGet("/ready")]
    public IActionResult Ready()
    {
        if (IsReady)
            return Ok(new { status = "ready" });

        return StatusCode(503, new { status = "not ready" });
    }
}
