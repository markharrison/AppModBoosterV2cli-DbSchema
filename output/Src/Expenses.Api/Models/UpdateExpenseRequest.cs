using System.ComponentModel.DataAnnotations;

namespace Expenses.Api.Models;

public class UpdateExpenseRequest
{
    public int? CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AmountMinor must be greater than 0.")]
    public int? AmountMinor { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; }

    public DateOnly? ExpenseDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ReceiptFile { get; set; }
}
