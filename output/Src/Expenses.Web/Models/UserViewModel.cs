namespace Expenses.Web.Models;

public class UserViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
    public RoleViewModel? Role { get; set; }
    public UserViewModel? Manager { get; set; }

    public bool IsManager => Role?.RoleName == "Manager";
}
