using System.Text.Json.Serialization;
using Expenses.Api.Controllers;
using Expenses.Api.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// JSON serialization — handle circular references
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Database context — SQLite for Development, SQL Server for Staging/Production
var environment = builder.Environment.EnvironmentName;
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ExpensesDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=expenses.db"));
}
else
{
    builder.Services.AddDbContext<ExpensesDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// OpenApi + Scalar for Development and Staging
if (builder.Environment.IsDevelopment() || environment == "Staging")
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

// NOT-ready middleware — return 503 for data endpoints before startup completes
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (!HealthController.IsReady
        && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 503;
        await context.Response.WriteAsJsonAsync(new { error = "Service is starting up. Please try again shortly." });
        return;
    }
    await next();
});

// OpenApi + Scalar middleware
if (app.Environment.IsDevelopment() || environment == "Staging")
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

// Database initialization with retry logic
await InitializeDatabaseAsync(app);

HealthController.IsReady = true;

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    const int maxRetries = 5;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ExpensesDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            if (env.IsDevelopment())
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
            }

            // Seed data for Development and Staging
            if (env.IsDevelopment() || env.EnvironmentName == "Staging")
            {
                await SeedData.SeedAsync(context);
            }

            return;
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries)
            {
                Console.Error.WriteLine($"Database initialization failed after {maxRetries} attempts: {ex.Message}");
                throw;
            }

            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            Console.WriteLine($"Database initialization attempt {attempt} failed: {ex.Message}. Retrying in {delay.TotalSeconds}s...");
            await Task.Delay(delay);
        }
    }
}
