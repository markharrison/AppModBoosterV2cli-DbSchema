using Microsoft.AspNetCore.Mvc;

namespace Expenses.Web.Controllers;

public class UserContextController : Controller
{
    [HttpPost("set-user")]
    [ValidateAntiForgeryToken]
    public IActionResult SetUser(int userId)
    {
        HttpContext.Session.SetInt32("CurrentUserId", userId);
        var returnUrl = Request.Headers.Referer.ToString();
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = "/";
        return Redirect(returnUrl);
    }
}
