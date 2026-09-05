using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Properties;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.UnitTests.Application;

public class PropertyServiceTests
{
    [Fact]
    public async Task CreateAsync_rejects_negative_price()
    {
        await using var db = CreateContext();
        var service = new PropertyService(db);

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
        var service = new PropertyService(db);

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
        var service = new PropertyService(db);

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
    }

    private static PropertyInventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PropertyInventoryDbContext>()
            .UseInMemoryDatabase($"PropertyServiceTests-{Guid.NewGuid()}")
            .Options;
        return new PropertyInventoryDbContext(options);
    }
}
