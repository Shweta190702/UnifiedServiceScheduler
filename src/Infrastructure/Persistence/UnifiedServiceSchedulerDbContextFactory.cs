using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UnifiedServiceScheduler.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI tooling (dotnet ef migrations / database update).
/// Ensures reliable DbContext instantiation during CLI operations.
/// </summary>
public sealed class UnifiedServiceSchedulerDbContextFactory : IDesignTimeDbContextFactory<UnifiedServiceSchedulerDbContext>
{
    public UnifiedServiceSchedulerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UnifiedServiceSchedulerDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=unified_scheduler;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(UnifiedServiceSchedulerDbContext).Assembly.FullName);
        });

        return new UnifiedServiceSchedulerDbContext(optionsBuilder.Options);
    }
}
