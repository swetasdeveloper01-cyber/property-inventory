using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Dashboard;
using PropertyInventory.Domain.Entities;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.UnitTests.Application;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSalesAsync_returns_one_row_per_ownership_including_current()
    {
        await using var db = CreateContext();
        await SeedThreeOwnersAsync(db);

        var service = new DashboardService(db);
        var sales = await service.GetSalesAsync();

        Assert.Equal(3, sales.Count);
        Assert.Equal(
            ["Owner C", "Owner B", "Owner A"],
            sales.Select(item => item.Owner).ToArray());
    }

    [Fact]
    public async Task GetSalesAsync_maps_asking_price_separately_from_sold_at()
    {
        await using var db = CreateContext();
        var propertyId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        db.Properties.Add(new Property
        {
            Id = propertyId,
            Name = "Maisonette",
            Address = "Addr",
            Price = 130_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2023, 1, 1)
        });
        db.Contacts.Add(new Contact
        {
            Id = contactId,
            FirstName = "Carmen",
            LastName = "Attard",
            PhoneNumber = "1",
            Email = "carmen@example.com"
        });
        db.PropertyOwnerships.Add(new PropertyOwnership
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            ContactId = contactId,
            EffectiveFrom = new DateOnly(2024, 1, 15),
            EffectiveTill = null,
            AcquisitionPrice = 120_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 130_480m
        });
        await db.SaveChangesAsync();

        var sales = await new DashboardService(db).GetSalesAsync();
        var row = Assert.Single(sales);

        Assert.Equal("Maisonette", row.PropertyName);
        Assert.Equal(130_000m, row.AskingPrice);
        Assert.Equal("EUR", row.AskingCurrency);
        Assert.Equal("Carmen Attard", row.Owner);
        Assert.Equal(new DateOnly(2024, 1, 15), row.DateOfPurchase);
        Assert.Equal(120_000m, row.SoldAtPrice);
        Assert.Equal("EUR", row.SoldAtCurrency);
        Assert.Equal(130_480m, row.SoldAtPriceUsd);
        Assert.NotEqual(row.AskingPrice, row.SoldAtPrice);
    }

    [Fact]
    public async Task GetSalesAsync_orders_by_purchase_date_descending()
    {
        await using var db = CreateContext();
        await SeedThreeOwnersAsync(db);

        var sales = await new DashboardService(db).GetSalesAsync();

        Assert.Equal(
            [
                new DateOnly(2025, 1, 1),
                new DateOnly(2024, 1, 1),
                new DateOnly(2023, 1, 1)
            ],
            sales.Select(item => item.DateOfPurchase).ToArray());
    }

    [Fact]
    public async Task GetSalesAsync_returns_empty_collection_when_no_ownerships()
    {
        await using var db = CreateContext();
        var sales = await new DashboardService(db).GetSalesAsync();
        Assert.Empty(sales);
    }

    private static async Task SeedThreeOwnersAsync(PropertyInventoryDbContext db)
    {
        var propertyId = Guid.NewGuid();
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var ownerC = Guid.NewGuid();

        db.Properties.Add(new Property
        {
            Id = propertyId,
            Name = "Property X",
            Address = "Addr",
            Price = 200_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2022, 1, 1)
        });
        db.Contacts.AddRange(
            CreateContact(ownerA, "Owner", "A", "a@example.com"),
            CreateContact(ownerB, "Owner", "B", "b@example.com"),
            CreateContact(ownerC, "Owner", "C", "c@example.com"));
        db.PropertyOwnerships.AddRange(
            new PropertyOwnership
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                ContactId = ownerA,
                EffectiveFrom = new DateOnly(2023, 1, 1),
                EffectiveTill = new DateOnly(2024, 1, 1),
                AcquisitionPrice = 150_000m,
                AcquisitionCurrency = "EUR",
                AcquisitionPriceUsd = 163_000m
            },
            new PropertyOwnership
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                ContactId = ownerB,
                EffectiveFrom = new DateOnly(2024, 1, 1),
                EffectiveTill = new DateOnly(2025, 1, 1),
                AcquisitionPrice = 170_000m,
                AcquisitionCurrency = "EUR",
                AcquisitionPriceUsd = 185_000m
            },
            new PropertyOwnership
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                ContactId = ownerC,
                EffectiveFrom = new DateOnly(2025, 1, 1),
                EffectiveTill = null,
                AcquisitionPrice = 190_000m,
                AcquisitionCurrency = "EUR",
                AcquisitionPriceUsd = 206_000m
            });
        await db.SaveChangesAsync();
    }

    private static Contact CreateContact(Guid id, string firstName, string lastName, string email) => new()
    {
        Id = id,
        FirstName = firstName,
        LastName = lastName,
        PhoneNumber = "1",
        Email = email
    };

    private static PropertyInventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PropertyInventoryDbContext>()
            .UseInMemoryDatabase($"DashboardServiceTests-{Guid.NewGuid()}")
            .Options;
        return new PropertyInventoryDbContext(options);
    }
}
