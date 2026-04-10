using System.ComponentModel.DataAnnotations;

namespace Expenses.Api.Models;

public class Expense
{
    public int ExpenseId { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public int StatusId { get; set; }

    [Required]
    public int AmountMinor { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    public DateOnly ExpenseDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ReceiptFile { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ExpenseCategory Category { get; set; } = null!;
    public ExpenseStatus Status { get; set; } = null!;
    public User? Reviewer { get; set; }
}
