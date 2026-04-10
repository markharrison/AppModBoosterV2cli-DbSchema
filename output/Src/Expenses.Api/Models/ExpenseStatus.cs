using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Expenses.Api.Models;

public class ExpenseStatus
{
    public int StatusId { get; set; }

    [Required, MaxLength(50)]
    public string StatusName { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
