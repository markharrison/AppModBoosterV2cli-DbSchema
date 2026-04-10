using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Expenses.Api.Data;

/// <summary>
/// Design-time factory that forces EF Core to use SQL Server for migrations,
/// ensuring generated column types are correct for Staging/Production.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ExpensesDbContext>
{
    public ExpensesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExpensesDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ExpensesDesignTime;Trusted_Connection=True;");
        return new ExpensesDbContext(optionsBuilder.Options);
    }
}
