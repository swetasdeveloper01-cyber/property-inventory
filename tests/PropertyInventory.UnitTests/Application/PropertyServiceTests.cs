using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Prices;
using PropertyInventory.Application.Properties;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.UnitTests.Application;

public class PropertyServiceTests
{
    [Fact]
    public async Task CreateAsync_rejects_negative_price()
    {
        await using var db = CreateContext();
        var service = CreatePropertyService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreatePropertyRequest
        {
            Name = "X",
            Address = "Y",
            Price = -1m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        }));
    }

    [Fact]
    public async Task UpdateAsync_throws_not_found_for_missing_property()
    {
        await using var db = CreateContext();
        var service = CreatePropertyService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), new UpdatePropertyRequest
        {
            Name = "X",
            Address = "Y",
            Price = 1m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        }));
    }

    [Fact]
    public async Task CreateAsync_writes_initial_price_history()
    {
        await using var db = CreateContext();
        var service = CreatePropertyService(db);

        var created = await service.CreateAsync(new CreatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        var history = await db.PropertyPriceHistories.SingleAsync(item => item.PropertyId == created.Id);
        Assert.Equal(50_000m, history.Amount);
        Assert.Equal("EUR", history.Currency);
        Assert.Equal(new DateOnly(2024, 4, 4), history.EffectiveDate);
    }

    [Fact]
    public async Task UpdateAsync_price_change_adds_exactly_one_history_record()
    {
        await using var db = CreateContext();
        var service = CreatePropertyService(db);
        var created = await service.CreateAsync(new CreatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        await service.UpdateAsync(created.Id, new UpdatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 60_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        var histories = await db.PropertyPriceHistories
            .Where(item => item.PropertyId == created.Id)
            .OrderBy(item => item.EffectiveDate)
            .ThenBy(item => item.Id)
            .ToListAsync();

        Assert.Equal(2, histories.Count);
        Assert.Equal(60_000m, histories[^1].Amount);
        Assert.Equal(60_000m, (await db.Properties.SingleAsync(item => item.Id == created.Id)).Price);
    }

    [Fact]
    public async Task UpdateAsync_currency_change_with_price_adds_one_history_record()
    {
        await using var db = CreateContext();
        var service = CreatePropertyService(db);
        var created = await service.CreateAsync(new CreatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        await service.UpdateAsync(created.Id, new UpdatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 55_000m,
            Currency = "USD",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        Assert.Equal(2, await db.PropertyPriceHistories.CountAsync(item => item.PropertyId == created.Id));
        var property = await db.Properties.SingleAsync(item => item.Id == created.Id);
        Assert.Equal(55_000m, property.Price);
        Assert.Equal("USD", property.Currency);
    }

    [Fact]
    public async Task UpdateAsync_non_price_change_does_not_add_history()
    {
        await using var db = CreateContext();
        var service = CreatePropertyService(db);
        var created = await service.CreateAsync(new CreatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        await service.UpdateAsync(created.Id, new UpdatePropertyRequest
        {
            Name = "Flat Updated",
            Address = "New Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        Assert.Equal(1, await db.PropertyPriceHistories.CountAsync(item => item.PropertyId == created.Id));
    }

    [Fact]
    public async Task UpdateAsync_identical_price_update_does_not_duplicate_history()
    {
        await using var db = CreateContext();
        var service = CreatePropertyService(db);
        var created = await service.CreateAsync(new CreatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        await service.UpdateAsync(created.Id, new UpdatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });
        await service.UpdateAsync(created.Id, new UpdatePropertyRequest
        {
            Name = "Flat",
            Address = "Street",
            Price = 50_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 4, 4)
        });

        Assert.Equal(1, await db.PropertyPriceHistories.CountAsync(item => item.PropertyId == created.Id));
    }

    private static PropertyService CreatePropertyService(PropertyInventoryDbContext db) =>
        new(db, new PropertyPriceService(db));

    private static PropertyInventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PropertyInventoryDbContext>()
            .UseInMemoryDatabase($"PropertyServiceTests-{Guid.NewGuid()}")
            .Options;
        return new PropertyInventoryDbContext(options);
    }
}
