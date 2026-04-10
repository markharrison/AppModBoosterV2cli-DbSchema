using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Expenses.Api.Models;

public class User
{
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public int? ManagerId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Role Role { get; set; } = null!;

    [JsonIgnore]
    public User? Manager { get; set; }

    [JsonIgnore]
    public ICollection<User> DirectReports { get; set; } = new List<User>();

    [JsonIgnore]
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    [JsonIgnore]
    public ICollection<Expense> ReviewedExpenses { get; set; } = new List<Expense>();
}
