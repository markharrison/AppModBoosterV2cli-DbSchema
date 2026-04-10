namespace Expenses.Web.Models;

public class DashboardViewModel
{
    public ExpenseSummaryViewModel Summary { get; set; } = new();
    public int PendingApprovalCount { get; set; }
    public bool IsManager { get; set; }
    public UserViewModel? CurrentUser { get; set; }
    public bool ApiAvailable { get; set; } = true;
}
