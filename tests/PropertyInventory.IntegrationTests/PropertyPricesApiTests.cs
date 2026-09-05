using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Ownerships;
using PropertyInventory.Application.Prices;
using PropertyInventory.Application.Properties;
using PropertyInventory.Domain.Entities;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.IntegrationTests;

public class PropertyPricesApiTests : IClassFixture<PropertyInventoryApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly PropertyInventoryApiFactory _factory;

    public PropertyPricesApiTests(PropertyInventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_prices_returns_chronological_history()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync(100_000m);

        await PostPriceAsync(property.Id, 110_000m, "EUR", new DateOnly(2026, 2, 1));
        await PostPriceAsync(property.Id, 120_000m, "EUR", new DateOnly(2026, 1, 15));

        var response = await _client.GetAsync($"/api/properties/{property.Id}/prices");
        var prices = await response.Content.ReadFromJsonAsync<List<PropertyPriceDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(prices);
        Assert.Equal(3, prices.Count);
        Assert.True(prices[0].EffectiveDate <= prices[1].EffectiveDate);
        Assert.True(prices[1].EffectiveDate <= prices[2].EffectiveDate);
        Assert.Equal(new DateOnly(2025, 1, 1), prices[0].EffectiveDate);
        Assert.Equal(new DateOnly(2026, 1, 15), prices[1].EffectiveDate);
        Assert.Equal(new DateOnly(2026, 2, 1), prices[2].EffectiveDate);
    }

    [Fact]
    public async Task Get_prices_returns_404_when_property_missing()
    {
        await ResetDatabaseAsync();
        var response = await _client.GetAsync($"/api/properties/{Guid.NewGuid()}/prices");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_prices_returns_empty_when_no_history()
    {
        await ResetDatabaseAsync();
        var propertyId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyInventoryDbContext>();
            db.Properties.Add(new Property
            {
                Id = propertyId,
                Name = "NoHistory",
                Address = "Addr",
                Price = 10_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2025, 1, 1)
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/properties/{propertyId}/prices");
        var prices = await response.Content.ReadFromJsonAsync<List<PropertyPriceDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(prices);
        Assert.Empty(prices);
    }

    [Fact]
    public async Task Post_price_updates_current_property_and_history()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync(100_000m);

        var response = await _client.PostAsJsonAsync($"/api/properties/{property.Id}/prices", new CreatePropertyPriceRequest
        {
            Amount = 130_000m,
            Currency = "eur",
            EffectiveDate = new DateOnly(2026, 5, 1)
        });

        var created = await response.Content.ReadFromJsonAsync<PropertyPriceDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(130_000m, created.Amount);
        Assert.Equal("EUR", created.Currency);

        var refreshed = await _client.GetFromJsonAsync<PropertyDto>($"/api/properties/{property.Id}", JsonOptions);
        Assert.NotNull(refreshed);
        Assert.Equal(130_000m, refreshed.Price);
        Assert.Equal("EUR", refreshed.Currency);
    }

    [Fact]
    public async Task Post_price_returns_404_when_property_missing()
    {
        await ResetDatabaseAsync();
        var response = await _client.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/prices", new CreatePropertyPriceRequest
        {
            Amount = 10_000m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2026, 1, 1)
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_price_returns_400_for_invalid_amount_currency_and_date()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync(100_000m);

        var response = await _client.PostAsJsonAsync($"/api/properties/{property.Id}/prices", new CreatePropertyPriceRequest
        {
            Amount = 0m,
            Currency = "EURO",
            EffectiveDate = default
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_price_does_not_change_ownership_acquisition()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync(130_000m);
        var contactResponse = await _client.PostAsJsonAsync("/api/contacts", new
        {
            FirstName = "Owner",
            LastName = "One",
            PhoneNumber = "+356 1",
            Email = "price.owner@example.com"
        });
        contactResponse.EnsureSuccessStatusCode();
        var contact = await contactResponse.Content.ReadFromJsonAsync<Application.Contacts.ContactDto>(JsonOptions);
        Assert.NotNull(contact);

        await _client.PostAsJsonAsync($"/api/properties/{property.Id}/ownerships", new CreateOwnershipRequest
        {
            ContactId = contact.Id,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            AcquisitionPrice = 120_000m,
            AcquisitionCurrency = "EUR"
        });

        await PostPriceAsync(property.Id, 150_000m, "EUR", new DateOnly(2026, 6, 1));

        var ownerships = await _client.GetFromJsonAsync<List<OwnershipDto>>(
            $"/api/properties/{property.Id}/ownerships",
            JsonOptions);
        Assert.NotNull(ownerships);
        Assert.Single(ownerships);
        Assert.Equal(120_000m, ownerships[0].AcquisitionPrice);
        Assert.Equal(130_479.60m, ownerships[0].AcquisitionPriceUsd);

        var refreshed = await _client.GetFromJsonAsync<PropertyDto>($"/api/properties/{property.Id}", JsonOptions);
        Assert.NotNull(refreshed);
        Assert.Equal(150_000m, refreshed.Price);
    }

    [Fact]
    public async Task Put_property_price_change_adds_one_history_entry()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync(100_000m);

        var putResponse = await _client.PutAsJsonAsync($"/api/properties/{property.Id}", new UpdatePropertyRequest
        {
            Name = property.Name,
            Address = property.Address,
            Price = 115_000m,
            Currency = "EUR",
            DateOfRegistration = property.DateOfRegistration
        });
        putResponse.EnsureSuccessStatusCode();

        var prices = await _client.GetFromJsonAsync<List<PropertyPriceDto>>(
            $"/api/properties/{property.Id}/prices",
            JsonOptions);
        Assert.NotNull(prices);
        Assert.Equal(2, prices.Count);
        Assert.Equal(115_000m, prices[^1].Amount);
    }

    [Fact]
    public async Task Put_property_without_price_change_does_not_add_history()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync(100_000m);

        var putResponse = await _client.PutAsJsonAsync($"/api/properties/{property.Id}", new UpdatePropertyRequest
        {
            Name = "Renamed",
            Address = property.Address,
            Price = 100_000m,
            Currency = "EUR",
            DateOfRegistration = property.DateOfRegistration
        });
        putResponse.EnsureSuccessStatusCode();

        var prices = await _client.GetFromJsonAsync<List<PropertyPriceDto>>(
            $"/api/properties/{property.Id}/prices",
            JsonOptions);
        Assert.NotNull(prices);
        Assert.Single(prices);
    }

    private async Task<PropertyDto> CreatePropertyAsync(decimal price)
    {
        var response = await _client.PostAsJsonAsync("/api/properties", new CreatePropertyRequest
        {
            Name = $"P-{Guid.NewGuid():N}"[..12],
            Address = "Test Address",
            Price = price,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2025, 1, 1)
        });
        response.EnsureSuccessStatusCode();
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>(JsonOptions);
        Assert.NotNull(property);
        return property;
    }

    private async Task PostPriceAsync(Guid propertyId, decimal amount, string currency, DateOnly effectiveDate)
    {
        var response = await _client.PostAsJsonAsync($"/api/properties/{propertyId}/prices", new CreatePropertyPriceRequest
        {
            Amount = amount,
            Currency = currency,
            EffectiveDate = effectiveDate
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
