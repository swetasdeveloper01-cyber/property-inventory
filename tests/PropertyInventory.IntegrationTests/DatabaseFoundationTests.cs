using Microsoft.EntityFrameworkCore;
using PropertyInventory.Domain.Entities;
using PropertyInventory.Infrastructure.Persistence;
using PropertyInventory.Infrastructure.Persistence.Seed;

namespace PropertyInventory.IntegrationTests;

/// <summary>
/// Foundation persistence checks against EF InMemory (no SQL Server required).
/// </summary>
public class DatabaseFoundationTests
{
    private static PropertyInventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PropertyInventoryDbContext>()
            .UseInMemoryDatabase($"PropertyInventoryTests-{Guid.NewGuid()}")
            .Options;

        return new PropertyInventoryDbContext(options);
    }

    [Fact]
    public async Task DbContext_can_persist_and_query_a_contact()
    {
        await using var dbContext = CreateContext();

        var contactId = Guid.NewGuid();
        dbContext.Contacts.Add(new Contact
        {
            Id = contactId,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+356 2000 0000",
            Email = $"test.user.{contactId:N}@example.com"
        });

        await dbContext.SaveChangesAsync();

        var stored = await dbContext.Contacts.SingleAsync(contact => contact.Id == contactId);
        Assert.Equal("Test", stored.FirstName);
        Assert.Equal("User", stored.LastName);
    }

    [Fact]
    public async Task DbContext_can_persist_seed_graph_with_ownership_and_price_history()
    {
        await using var dbContext = CreateContext();

        dbContext.Contacts.AddRange(SeedData.CreateContacts());
        dbContext.Properties.AddRange(SeedData.CreateProperties());
        dbContext.PropertyOwnerships.AddRange(SeedData.CreateOwnerships());
        dbContext.PropertyPriceHistories.AddRange(SeedData.CreatePriceHistory());
        await dbContext.SaveChangesAsync();

        var maisonette = await dbContext.Properties
            .Include(property => property.Ownerships)
            .Include(property => property.PriceHistory)
            .SingleAsync(property => property.Id == SeedData.MaisonetteId);

        Assert.Equal("Maisonette", maisonette.Name);
        Assert.Equal(2, maisonette.Ownerships.Count);
        Assert.Equal(2, maisonette.PriceHistory.Count);
        Assert.Contains(maisonette.Ownerships, ownership => ownership.EffectiveTill is null);
    }
}
