namespace Expenses.Web.Models;

public class ExpenseViewModel
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int StatusId { get; set; }
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "GBP";
    public DateOnly ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserViewModel? User { get; set; }
    public ExpenseCategoryViewModel? Category { get; set; }
    public ExpenseStatusViewModel? Status { get; set; }
    public UserViewModel? Reviewer { get; set; }

    public string FormattedAmount => $"\u00a3{AmountMinor / 100m:F2}";
    public string StatusName => Status?.StatusName ?? "Unknown";
    public string CategoryName => Category?.CategoryName ?? "Unknown";
    public string UserName => User?.UserName ?? "Unknown";

    public string StatusBadgeClass => StatusName switch
    {
        "Draft" => "bg-secondary",
        "Submitted" => "bg-primary",
        "Approved" => "bg-success",
        "Rejected" => "bg-danger",
        _ => "bg-secondary"
    };

    public bool CanEdit => StatusName is "Draft" or "Rejected";
    public bool CanSubmit => StatusName == "Draft";
    public bool CanDelete => StatusName == "Draft";
}
