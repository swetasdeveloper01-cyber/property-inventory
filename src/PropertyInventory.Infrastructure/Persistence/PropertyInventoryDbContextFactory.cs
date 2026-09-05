using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyInventory.Infrastructure.Persistence;

/// <summary>
/// Enables EF Core design-time tools (migrations) without running the full API host.
/// Uses the same LocalDB default as Development appsettings.
/// </summary>
public class PropertyInventoryDbContextFactory : IDesignTimeDbContextFactory<PropertyInventoryDbContext>
{
    public PropertyInventoryDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=(localdb)\\mssqllocaldb;Database=PropertyInventory;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<PropertyInventoryDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new PropertyInventoryDbContext(optionsBuilder.Options);
    }
}
