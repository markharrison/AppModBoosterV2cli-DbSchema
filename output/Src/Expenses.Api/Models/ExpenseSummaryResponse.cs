namespace Expenses.Api.Models;

public class ExpenseSummaryResponse
{
    public int Draft { get; set; }
    public int Submitted { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
}
