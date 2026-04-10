using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Expenses.Api.Models;

public class ExpenseCategory
{
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
