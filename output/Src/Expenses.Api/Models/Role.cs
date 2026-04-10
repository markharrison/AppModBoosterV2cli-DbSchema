using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Expenses.Api.Models;

public class Role
{
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    [JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();
}
