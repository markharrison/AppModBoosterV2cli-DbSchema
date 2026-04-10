using Expenses.Api.Controllers;
using Expenses.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Expenses.Api.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ExpensesDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Also remove any DbContext registrations
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ExpensesDbContext));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // Open a persistent SQLite in-memory connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<ExpensesDbContext>(options =>
                options.UseSqlite(_connection));

            // Build the service provider and initialize the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ExpensesDbContext>();
            db.Database.EnsureCreated();
            SeedData.SeedAsync(db).GetAwaiter().GetResult();

            // Mark as ready so the middleware doesn't block API calls
            HealthController.IsReady = true;
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
