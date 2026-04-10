using System.ComponentModel.DataAnnotations;

namespace Expenses.Api.Models;

public class CreateExpenseRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required, Range(1, int.MaxValue, ErrorMessage = "AmountMinor must be greater than 0.")]
    public int AmountMinor { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    [Required]
    public DateOnly ExpenseDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ReceiptFile { get; set; }
}
