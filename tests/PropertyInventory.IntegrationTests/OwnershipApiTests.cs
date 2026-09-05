using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Contacts;
using PropertyInventory.Application.Ownerships;
using PropertyInventory.Application.Properties;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.IntegrationTests;

public class OwnershipApiTests : IClassFixture<PropertyInventoryApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly PropertyInventoryApiFactory _factory;

    public OwnershipApiTests(PropertyInventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ownerships_returns_chronological_history()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync();
        var ownerA = await CreateContactAsync("A", "One", "a.one@example.com");
        var ownerB = await CreateContactAsync("B", "Two", "b.two@example.com");

        await CreateOwnershipAsync(property.Id, new CreateOwnershipRequest
        {
            ContactId = ownerA.Id,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTill = null,
            AcquisitionPrice = 100_000m,
            AcquisitionCurrency = "EUR"
        });
        await CreateOwnershipAsync(property.Id, new CreateOwnershipRequest
        {
            ContactId = ownerB.Id,
            EffectiveFrom = new DateOnly(2026, 9, 10),
            EffectiveTill = null,
            AcquisitionPrice = 120_000m,
            AcquisitionCurrency = "EUR"
        });

        var response = await _client.GetAsync($"/api/properties/{property.Id}/ownerships");
        var ownerships = await response.Content.ReadFromJsonAsync<List<OwnershipDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(ownerships);
        Assert.Equal(2, ownerships.Count);
        Assert.Equal(ownerA.Id, ownerships[0].ContactId);
        Assert.Equal(new DateOnly(2026, 9, 10), ownerships[0].EffectiveTill);
        Assert.Equal(ownerB.Id, ownerships[1].ContactId);
        Assert.Null(ownerships[1].EffectiveTill);
        Assert.True(ownerships[1].IsCurrent);
    }

    [Fact]
    public async Task Get_ownerships_returns_empty_list_when_property_has_none()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync();

        var response = await _client.GetAsync($"/api/properties/{property.Id}/ownerships");
        var ownerships = await response.Content.ReadFromJsonAsync<List<OwnershipDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(ownerships);
        Assert.Empty(ownerships);
    }

    [Fact]
    public async Task Get_ownerships_returns_404_when_property_missing()
    {
        await ResetDatabaseAsync();
        var response = await _client.GetAsync($"/api/properties/{Guid.NewGuid()}/ownerships");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ownership_returns_404_when_contact_missing()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync();

        var response = await _client.PostAsJsonAsync($"/api/properties/{property.Id}/ownerships", new CreateOwnershipRequest
        {
            ContactId = Guid.NewGuid(),
            EffectiveFrom = new DateOnly(2026, 1, 1),
            AcquisitionPrice = 100_000m,
            AcquisitionCurrency = "EUR"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ownership_returns_404_when_property_missing()
    {
        await ResetDatabaseAsync();
        var contact = await CreateContactAsync("C", "Three", "c.three@example.com");

        var response = await _client.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/ownerships", new CreateOwnershipRequest
        {
            ContactId = contact.Id,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            AcquisitionPrice = 100_000m,
            AcquisitionCurrency = "EUR"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ownership_transfer_keeps_single_current_owner_and_asking_price()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync(price: 130_000m);
        var ownerA = await CreateContactAsync("Owner", "A", "owner.a@example.com");
        var ownerB = await CreateContactAsync("Owner", "B", "owner.b@example.com");

        await CreateOwnershipAsync(property.Id, new CreateOwnershipRequest
        {
            ContactId = ownerA.Id,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTill = null,
            AcquisitionPrice = 110_000m,
            AcquisitionCurrency = "EUR"
        });

        var transferResponse = await _client.PostAsJsonAsync($"/api/properties/{property.Id}/ownerships", new CreateOwnershipRequest
        {
            ContactId = ownerB.Id,
            EffectiveFrom = new DateOnly(2026, 9, 10),
            EffectiveTill = null,
            AcquisitionPrice = 120_000m,
            AcquisitionCurrency = "EUR"
        });

        Assert.Equal(HttpStatusCode.Created, transferResponse.StatusCode);

        var ownerships = await _client.GetFromJsonAsync<List<OwnershipDto>>(
            $"/api/properties/{property.Id}/ownerships",
            JsonOptions);
        Assert.NotNull(ownerships);
        Assert.Equal(2, ownerships.Count);
        Assert.Equal(new DateOnly(2026, 9, 10), ownerships[0].EffectiveTill);
        Assert.Equal(110_000m, ownerships[0].AcquisitionPrice);
        Assert.Equal(new DateOnly(2026, 9, 10), ownerships[1].EffectiveFrom);
        Assert.Null(ownerships[1].EffectiveTill);
        Assert.Equal(120_000m, ownerships[1].AcquisitionPrice);
        Assert.Single(ownerships, item => item.EffectiveTill is null);

        var refreshedProperty = await _client.GetFromJsonAsync<PropertyDto>($"/api/properties/{property.Id}", JsonOptions);
        Assert.NotNull(refreshedProperty);
        Assert.Equal(130_000m, refreshedProperty.Price);
    }

    [Fact]
    public async Task Create_ownership_returns_409_for_overlap()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync();
        var ownerA = await CreateContactAsync("A", "Overlap", "a.overlap@example.com");
        var ownerB = await CreateContactAsync("B", "Overlap", "b.overlap@example.com");

        await CreateOwnershipAsync(property.Id, new CreateOwnershipRequest
        {
            ContactId = ownerA.Id,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTill = new DateOnly(2026, 6, 1),
            AcquisitionPrice = 100_000m,
            AcquisitionCurrency = "EUR"
        });

        var response = await _client.PostAsJsonAsync($"/api/properties/{property.Id}/ownerships", new CreateOwnershipRequest
        {
            ContactId = ownerB.Id,
            EffectiveFrom = new DateOnly(2026, 5, 1),
            EffectiveTill = new DateOnly(2026, 7, 1),
            AcquisitionPrice = 105_000m,
            AcquisitionCurrency = "EUR"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ownership_returns_400_for_invalid_range()
    {
        await ResetDatabaseAsync();
        var property = await CreatePropertyAsync();
        var contact = await CreateContactAsync("Bad", "Range", "bad.range@example.com");

        var response = await _client.PostAsJsonAsync($"/api/properties/{property.Id}/ownerships", new CreateOwnershipRequest
        {
            ContactId = contact.Id,
            EffectiveFrom = new DateOnly(2026, 9, 10),
            EffectiveTill = new DateOnly(2026, 9, 1),
            AcquisitionPrice = 100_000m,
            AcquisitionCurrency = "EUR"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<PropertyDto> CreatePropertyAsync(decimal price = 100_000m)
    {
        var response = await _client.PostAsJsonAsync("/api/properties", new CreatePropertyRequest
        {
            Name = $"Property-{Guid.NewGuid():N}"[..20],
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

    private async Task<OwnershipDto> CreateOwnershipAsync(Guid propertyId, CreateOwnershipRequest request)
    {
        var response = await _client.PostAsJsonAsync($"/api/properties/{propertyId}/ownerships", request);
        response.EnsureSuccessStatusCode();
        var ownership = await response.Content.ReadFromJsonAsync<OwnershipDto>(JsonOptions);
        Assert.NotNull(ownership);
        return ownership;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyInventoryDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
