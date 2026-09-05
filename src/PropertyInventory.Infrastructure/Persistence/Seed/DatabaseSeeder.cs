using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PropertyInventory.Infrastructure.Persistence.Seed;

/// <summary>
/// Applies pending migrations and inserts the sample dataset when the database is empty.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PropertyInventoryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PropertyInventoryDbContext>>();

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            // Non-relational providers (for example, EF InMemory in tests) do not support migrations.
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (await dbContext.Properties.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already contains data; skipping seed.");
            return;
        }

        logger.LogInformation("Seeding Property Inventory sample data.");

        dbContext.Contacts.AddRange(SeedData.CreateContacts());
        dbContext.Properties.AddRange(SeedData.CreateProperties());
        dbContext.PropertyOwnerships.AddRange(SeedData.CreateOwnerships());
        dbContext.PropertyPriceHistories.AddRange(SeedData.CreatePriceHistory());

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
