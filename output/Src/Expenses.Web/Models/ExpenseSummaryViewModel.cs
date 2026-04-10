namespace Expenses.Web.Models;

public class ExpenseSummaryViewModel
{
    public int Draft { get; set; }
    public int Submitted { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }

    public int Total => Draft + Submitted + Approved + Rejected;
}
