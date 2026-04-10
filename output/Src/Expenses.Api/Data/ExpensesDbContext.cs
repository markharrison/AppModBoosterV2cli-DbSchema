using Expenses.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Expenses.Api.Data;

public class ExpensesDbContext : DbContext
{
    public ExpensesDbContext(DbContextOptions<ExpensesDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<ExpenseStatus> ExpenseStatuses => Set<ExpenseStatus>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Manager)
                  .WithMany(u => u.DirectReports)
                  .HasForeignKey(e => e.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ExpenseCategory
        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.HasIndex(e => e.CategoryName).IsUnique();
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // ExpenseStatus
        modelBuilder.Entity<ExpenseStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);
            entity.HasIndex(e => e.StatusName).IsUnique();
            entity.Property(e => e.StatusName).IsRequired().HasMaxLength(50);
        });

        // Expense
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId);
            entity.Property(e => e.AmountMinor).IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("GBP");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ReceiptFile).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Expenses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Expenses)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Status)
                  .WithMany(s => s.Expenses)
                  .HasForeignKey(e => e.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reviewer)
                  .WithMany(u => u.ReviewedExpenses)
                  .HasForeignKey(e => e.ReviewedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.UserId, e.StatusId })
                  .HasDatabaseName("IX_Expenses_UserId_StatusId");

            entity.HasIndex(e => e.SubmittedAt)
                  .HasDatabaseName("IX_Expenses_SubmittedAt");
        });
    }
}
