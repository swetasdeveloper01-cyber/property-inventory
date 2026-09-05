using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Prices;
using PropertyInventory.Domain.Entities;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.UnitTests.Application;

public class PropertyPriceServiceTests
{
    [Fact]
    public async Task CreateAsync_updates_current_price_and_appends_history()
    {
        await using var db = CreateContext();
        var propertyId = await SeedPropertyAsync(db, 100_000m, "EUR");
        var service = new PropertyPriceService(db);

        var created = await service.CreateAsync(propertyId, new CreatePropertyPriceRequest
        {
            Amount = 125_000m,
            Currency = "eur",
            EffectiveDate = new DateOnly(2026, 3, 1)
        });

        var property = await db.Properties.SingleAsync(item => item.Id == propertyId);
        Assert.Equal(125_000m, property.Price);
        Assert.Equal("EUR", property.Currency);
        Assert.Equal(125_000m, created.Amount);
        Assert.Equal(new DateOnly(2026, 3, 1), created.EffectiveDate);

        var historyCount = await db.PropertyPriceHistories.CountAsync(item => item.PropertyId == propertyId);
        Assert.Equal(2, historyCount);
    }

    [Fact]
    public async Task CreateAsync_does_not_modify_ownership_acquisition_prices()
    {
        await using var db = CreateContext();
        var propertyId = await SeedPropertyAsync(db, 100_000m, "EUR");
        var ownershipId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        db.Contacts.Add(new Contact
        {
            Id = contactId,
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "1",
            Email = "owner@example.com"
        });
        db.PropertyOwnerships.Add(new PropertyOwnership
        {
            Id = ownershipId,
            PropertyId = propertyId,
            ContactId = contactId,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTill = null,
            AcquisitionPrice = 90_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 97_859.70m
        });
        await db.SaveChangesAsync();

        var service = new PropertyPriceService(db);
        await service.CreateAsync(propertyId, new CreatePropertyPriceRequest
        {
            Amount = 140_000m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2026, 4, 1)
        });

        var ownership = await db.PropertyOwnerships.SingleAsync(item => item.Id == ownershipId);
        Assert.Equal(90_000m, ownership.AcquisitionPrice);
        Assert.Equal(97_859.70m, ownership.AcquisitionPriceUsd);
    }

    [Fact]
    public async Task CreateAsync_rejects_non_positive_amount()
    {
        await using var db = CreateContext();
        var propertyId = await SeedPropertyAsync(db, 100_000m, "EUR");
        var service = new PropertyPriceService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(propertyId, new CreatePropertyPriceRequest
        {
            Amount = 0m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2026, 1, 1)
        }));
    }

    [Fact]
    public async Task ApplyAskingPriceChange_skips_history_when_price_unchanged()
    {
        await using var db = CreateContext();
        var propertyId = await SeedPropertyAsync(db, 100_000m, "EUR");
        var property = await db.Properties.SingleAsync(item => item.Id == propertyId);
        var service = new PropertyPriceService(db);

        service.ApplyAskingPriceChange(property, 100_000m, "EUR", new DateOnly(2026, 5, 1), forceRecord: false);
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.PropertyPriceHistories.CountAsync(item => item.PropertyId == propertyId));
    }

    [Fact]
    public async Task CreateAsync_does_not_persist_partial_update_when_save_fails()
    {
        var databaseName = $"PriceAtomic-{Guid.NewGuid()}";
        var propertyId = Guid.NewGuid();

        await using (var setup = CreateContext(databaseName))
        {
            setup.Properties.Add(new Property
            {
                Id = propertyId,
                Name = "Villa",
                Address = "1 Road",
                Price = 100_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2025, 1, 1)
            });
            setup.PropertyPriceHistories.Add(new PropertyPriceHistory
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                Amount = 100_000m,
                Currency = "EUR",
                EffectiveDate = new DateOnly(2025, 1, 1)
            });
            await setup.SaveChangesAsync();
        }

        await using (var act = CreateContext(databaseName, failOnPriceHistoryInsert: true))
        {
            var service = new PropertyPriceService(act);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(propertyId, new CreatePropertyPriceRequest
            {
                Amount = 150_000m,
                Currency = "EUR",
                EffectiveDate = new DateOnly(2026, 6, 1)
            }));
        }

        await using var verify = CreateContext(databaseName);
        var property = await verify.Properties.SingleAsync(item => item.Id == propertyId);
        Assert.Equal(100_000m, property.Price);
        Assert.Equal(1, await verify.PropertyPriceHistories.CountAsync(item => item.PropertyId == propertyId));
    }

    private static async Task<Guid> SeedPropertyAsync(
        PropertyInventoryDbContext db,
        decimal price,
        string currency)
    {
        var propertyId = Guid.NewGuid();
        db.Properties.Add(new Property
        {
            Id = propertyId,
            Name = "Test",
            Address = "Addr",
            Price = price,
            Currency = currency,
            DateOfRegistration = new DateOnly(2025, 1, 1)
        });
        db.PropertyPriceHistories.Add(new PropertyPriceHistory
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            Amount = price,
            Currency = currency,
            EffectiveDate = new DateOnly(2025, 1, 1)
        });
        await db.SaveChangesAsync();
        return propertyId;
    }

    private static PropertyInventoryDbContext CreateContext(
        string? databaseName = null,
        bool failOnPriceHistoryInsert = false)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PropertyInventoryDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"PropertyPriceServiceTests-{Guid.NewGuid()}");

        if (failOnPriceHistoryInsert)
        {
            optionsBuilder.AddInterceptors(new FailOnPriceHistoryInsertInterceptor());
        }

        return new PropertyInventoryDbContext(optionsBuilder.Options);
    }
}
