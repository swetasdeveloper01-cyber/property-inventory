using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Contacts;
using PropertyInventory.Application.Dashboard;
using PropertyInventory.Application.Ownerships;
using PropertyInventory.Application.Properties;
using PropertyInventory.Infrastructure.Persistence;
using PropertyInventory.Infrastructure.Persistence.Seed;

namespace PropertyInventory.IntegrationTests;

public class DashboardApiTests : IClassFixture<PropertyInventoryApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly PropertyInventoryApiFactory _factory;

    public DashboardApiTests(PropertyInventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_sales_returns_200_and_empty_when_no_data()
    {
        await ResetDatabaseAsync();

        var response = await _client.GetAsync("/api/dashboard/sales");
        var sales = await response.Content.ReadFromJsonAsync<List<SalesDashboardItemDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(sales);
        Assert.Empty(sales);
    }

    [Fact]
    public async Task Get_sales_returns_one_row_per_ownership_for_same_property()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync("Property X", 200_000m);
        var ownerA = await CreateContactAsync("Owner", "A", "owner.a@example.com");
        var ownerB = await CreateContactAsync("Owner", "B", "owner.b@example.com");
        var ownerC = await CreateContactAsync("Owner", "C", "owner.c@example.com");

        await CreateOwnershipAsync(property.Id, ownerA.Id, new DateOnly(2023, 1, 1), 150_000m);
        await CreateOwnershipAsync(property.Id, ownerB.Id, new DateOnly(2024, 1, 1), 170_000m);
        await CreateOwnershipAsync(property.Id, ownerC.Id, new DateOnly(2025, 1, 1), 190_000m);

        var response = await _client.GetAsync("/api/dashboard/sales");
        var sales = await response.Content.ReadFromJsonAsync<List<SalesDashboardItemDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(sales);
        Assert.Equal(3, sales.Count);
        Assert.All(sales, item => Assert.Equal("Property X", item.PropertyName));
        Assert.All(sales, item => Assert.Equal(200_000m, item.AskingPrice));
        Assert.Equal(
            ["Owner C", "Owner B", "Owner A"],
            sales.Select(item => item.Owner).ToArray());
    }

    [Fact]
    public async Task Get_sales_maps_seed_style_fields_and_keeps_usd_deterministic()
    {
        await ResetDatabaseAsync();
        await SeedClientSampleAsync();

        var response = await _client.GetAsync("/api/dashboard/sales");
        var sales = await response.Content.ReadFromJsonAsync<List<SalesDashboardItemDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(sales);

        var carmen = Assert.Single(sales, item => item.Owner == "Carmen Attard");
        Assert.Equal("Maisonette", carmen.PropertyName);
        Assert.Equal(130_000m, carmen.AskingPrice);
        Assert.Equal("EUR", carmen.AskingCurrency);
        Assert.Equal(new DateOnly(2024, 1, 15), carmen.DateOfPurchase);
        Assert.Equal(120_000m, carmen.SoldAtPrice);
        Assert.Equal("EUR", carmen.SoldAtCurrency);
        Assert.Equal(130_480m, carmen.SoldAtPriceUsd);

        var joshua = Assert.Single(sales, item => item.Owner == "Joshua Mifsud");
        Assert.Equal(100_000m, joshua.SoldAtPrice);
        Assert.Equal(108_733m, joshua.SoldAtPriceUsd);
        // Asking price is current property asking price for every Maisonette row.
        Assert.Equal(130_000m, joshua.AskingPrice);

        var joe = Assert.Single(sales, item => item.Owner == "Joe Borg");
        Assert.Equal(400_000m, joe.SoldAtPrice);
        Assert.Equal(435_072m, joe.SoldAtPriceUsd);
        Assert.Equal(430_000m, joe.AskingPrice);
    }

    [Fact]
    public async Task Get_sales_orders_by_date_of_purchase_descending()
    {
        await ResetDatabaseAsync();
        await SeedClientSampleAsync();

        var sales = await _client.GetFromJsonAsync<List<SalesDashboardItemDto>>(
            "/api/dashboard/sales",
            JsonOptions);

        Assert.NotNull(sales);
        Assert.True(sales.Count >= 3);
        for (var index = 1; index < sales.Count; index++)
        {
            Assert.True(sales[index - 1].DateOfPurchase >= sales[index].DateOfPurchase);
        }
    }

    private async Task SeedClientSampleAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyInventoryDbContext>();
        db.Contacts.AddRange(SeedData.CreateContacts());
        db.Properties.AddRange(SeedData.CreateProperties());
        db.PropertyOwnerships.AddRange(SeedData.CreateOwnerships());
        db.PropertyPriceHistories.AddRange(SeedData.CreatePriceHistory());
        await db.SaveChangesAsync();
    }

    private async Task<PropertyDto> CreatePropertyAsync(string name, decimal price)
    {
        var response = await _client.PostAsJsonAsync("/api/properties", new CreatePropertyRequest
        {
            Name = name,
            Address = "Test Address",
            Price = price,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2022, 1, 1)
        });
        response.EnsureSuccessStatusCode();
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>(JsonOptions);
        Assert.NotNull(property);
        return property;
    }

    private async Task<ContactDto> CreateContactAsync(string firstName, string lastName, string email)
    {
        var response = await _client.PostAsJsonAsync("/api/contacts", new CreateContactRequest
        {
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = "+356 1000",
            Email = email
        });
        response.EnsureSuccessStatusCode();
        var contact = await response.Content.ReadFromJsonAsync<ContactDto>(JsonOptions);
        Assert.NotNull(contact);
        return contact;
    }

    private async Task CreateOwnershipAsync(
        Guid propertyId,
        Guid contactId,
        DateOnly effectiveFrom,
        decimal acquisitionPrice)
    {
        var response = await _client.PostAsJsonAsync($"/api/properties/{propertyId}/ownerships", new CreateOwnershipRequest
        {
            ContactId = contactId,
            EffectiveFrom = effectiveFrom,
            EffectiveTill = null,
            AcquisitionPrice = acquisitionPrice,
            AcquisitionCurrency = "EUR"
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyInventoryDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
