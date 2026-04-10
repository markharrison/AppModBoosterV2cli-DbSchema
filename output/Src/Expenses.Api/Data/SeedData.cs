using Expenses.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Expenses.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(ExpensesDbContext context)
    {
        var isSqlServer = context.Database.IsSqlServer();

        // Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Roles] ON");
            context.Roles.AddRange(
                new Role { RoleId = 1, RoleName = "Employee", Description = "Regular employee who can submit expenses" },
                new Role { RoleId = 2, RoleName = "Manager", Description = "Can view and approve/reject submitted expenses" }
            );
            await context.SaveChangesAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Roles] OFF");
            await transaction.CommitAsync();
        }

        // Seed Expense Categories
        if (!await context.ExpenseCategories.AnyAsync())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [ExpenseCategories] ON");
            context.ExpenseCategories.AddRange(
                new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
                new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
                new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
                new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
                new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
            );
            await context.SaveChangesAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [ExpenseCategories] OFF");
            await transaction.CommitAsync();
        }

        // Seed Expense Statuses
        if (!await context.ExpenseStatuses.AnyAsync())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [ExpenseStatuses] ON");
            context.ExpenseStatuses.AddRange(
                new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
                new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
                new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
                new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
            );
            await context.SaveChangesAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [ExpenseStatuses] OFF");
            await transaction.CommitAsync();
        }

        // Seed Users
        if (!await context.Users.AnyAsync())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Users] ON");
            context.Users.AddRange(
                new User
                {
                    UserId = 1,
                    UserName = "Alice Example",
                    Email = "alice@example.co.uk",
                    RoleId = 1,
                    ManagerId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserId = 2,
                    UserName = "Bob Manager",
                    Email = "bob.manager@example.co.uk",
                    RoleId = 2,
                    ManagerId = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Users] OFF");
            await transaction.CommitAsync();
        }

        // Seed Sample Expenses (all belonging to Alice)
        if (!await context.Expenses.AnyAsync())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Expenses] ON");
            context.Expenses.AddRange(
                new Expense
                {
                    ExpenseId = 1,
                    UserId = 1,
                    CategoryId = 1,
                    StatusId = 2,
                    AmountMinor = 2540,
                    Currency = "GBP",
                    ExpenseDate = new DateOnly(2025, 10, 20),
                    Description = "Taxi from airport to client",
                    ReceiptFile = "/receipts/alice/taxi_oct20.jpg",
                    SubmittedAt = DateTime.UtcNow,
                    ReviewedBy = null,
                    ReviewedAt = null,
                    CreatedAt = DateTime.UtcNow
                },
                new Expense
                {
                    ExpenseId = 2,
                    UserId = 1,
                    CategoryId = 2,
                    StatusId = 3,
                    AmountMinor = 1425,
                    Currency = "GBP",
                    ExpenseDate = new DateOnly(2025, 9, 15),
                    Description = "Client lunch meeting",
                    ReceiptFile = "/receipts/alice/lunch_sep15.jpg",
                    SubmittedAt = new DateTime(2025, 9, 16, 10, 15, 0, DateTimeKind.Utc),
                    ReviewedBy = 2,
                    ReviewedAt = new DateTime(2025, 9, 16, 14, 30, 0, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                },
                new Expense
                {
                    ExpenseId = 3,
                    UserId = 1,
                    CategoryId = 3,
                    StatusId = 1,
                    AmountMinor = 799,
                    Currency = "GBP",
                    ExpenseDate = new DateOnly(2025, 11, 1),
                    Description = "Office stationery",
                    ReceiptFile = null,
                    SubmittedAt = null,
                    ReviewedBy = null,
                    ReviewedAt = null,
                    CreatedAt = DateTime.UtcNow
                },
                new Expense
                {
                    ExpenseId = 4,
                    UserId = 1,
                    CategoryId = 4,
                    StatusId = 3,
                    AmountMinor = 12300,
                    Currency = "GBP",
                    ExpenseDate = new DateOnly(2025, 8, 10),
                    Description = "Hotel during client visit",
                    ReceiptFile = "/receipts/alice/hotel_aug10.jpg",
                    SubmittedAt = new DateTime(2025, 8, 11, 9, 0, 0, DateTimeKind.Utc),
                    ReviewedBy = 2,
                    ReviewedAt = new DateTime(2025, 8, 12, 14, 30, 0, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();
            if (isSqlServer) await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Expenses] OFF");
            await transaction.CommitAsync();
        }
    }
}
