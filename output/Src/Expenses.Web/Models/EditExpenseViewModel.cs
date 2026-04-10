using System.ComponentModel.DataAnnotations;

namespace Expenses.Web.Models;

public class EditExpenseViewModel
{
    public int ExpenseId { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than zero.")]
    [Display(Name = "Amount (£)")]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateOnly ExpenseDate { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public List<ExpenseCategoryViewModel> Categories { get; set; } = new();

    public int AmountMinor => (int)Math.Round(Amount * 100);
}
