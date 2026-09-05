using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Common.Models;
using PropertyInventory.Application.Properties;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.IntegrationTests;

public class PropertiesApiTests : IClassFixture<PropertyInventoryApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly PropertyInventoryApiFactory _factory;

    public PropertiesApiTests(PropertyInventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_properties_returns_paginated_list()
    {
        await ResetDatabaseAsync();
        await CreatePropertyAsync(new CreatePropertyRequest
        {
            Name = "Alpha Flat",
            Address = "1 Main Street",
            Price = 100_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });
        await CreatePropertyAsync(new CreatePropertyRequest
        {
            Name = "Beta House",
            Address = "2 Side Road",
            Price = 200_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 2, 1)
        });

        var response = await _client.GetAsync("/api/properties?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<PropertyDto>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Equal(1, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Get_properties_supports_filtering()
    {
        await ResetDatabaseAsync();
        await CreatePropertyAsync(new CreatePropertyRequest
        {
            Name = "Maisonette",
            Address = "Sliema Waterfront",
            Price = 130_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });
        await CreatePropertyAsync(new CreatePropertyRequest
        {
            Name = "Penthouse",
            Address = "Gzira Tower",
            Price = 430_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });

        var response = await _client.GetAsync("/api/properties?name=maison&minPrice=100000&maxPrice=200000");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PropertyDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("Maisonette", page.Items[0].Name);
    }

    [Fact]
    public async Task Get_property_by_id_returns_property()
    {
        await ResetDatabaseAsync();
        var created = await CreatePropertyAsync(new CreatePropertyRequest
        {
            Name = "Townhouse",
            Address = "Valletta",
            Price = 275_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2023, 5, 5)
        });

        var response = await _client.GetAsync($"/api/properties/{created.Id}");
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(property);
        Assert.Equal(created.Id, property.Id);
        Assert.Equal("Townhouse", property.Name);
    }

    [Fact]
    public async Task Get_property_by_id_returns_404_when_missing()
    {
        await ResetDatabaseAsync();
        var response = await _client.GetAsync($"/api/properties/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_property_returns_201()
    {
        await ResetDatabaseAsync();
        var response = await _client.PostAsJsonAsync("/api/properties", new CreatePropertyRequest
        {
            Name = "Studio",
            Address = "Msida",
            Price = 90_000m,
            Currency = "eur",
            DateOfRegistration = new DateOnly(2024, 3, 3)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>(JsonOptions);
        Assert.NotNull(property);
        Assert.NotEqual(Guid.Empty, property.Id);
        Assert.Equal("EUR", property.Currency);
    }

    [Fact]
    public async Task Create_property_returns_400_for_validation_failure()
    {
        await ResetDatabaseAsync();
        var response = await _client.PostAsJsonAsync("/api/properties", new CreatePropertyRequest
        {
            Name = "",
            Address = "",
            Price = -10m,
            Currency = "EURO",
            DateOfRegistration = default
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_property_returns_updated_values()
    {
        await ResetDatabaseAsync();
        var created = await CreatePropertyAsync(new CreatePropertyRequest
        {
            Name = "Old Name",
            Address = "Old Address",
            Price = 100_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });

        var response = await _client.PutAsJsonAsync($"/api/properties/{created.Id}", new UpdatePropertyRequest
        {
            Name = "New Name",
            Address = "New Address",
            Price = 120_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });

        var updated = await response.Content.ReadFromJsonAsync<PropertyDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(120_000m, updated.Price);
    }

    [Fact]
    public async Task Batch_create_properties_is_atomic_for_validation()
    {
        await ResetDatabaseAsync();
        var response = await _client.PostAsJsonAsync("/api/properties/batch", new[]
        {
            new CreatePropertyRequest
            {
                Name = "Valid",
                Address = "Address",
                Price = 10_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 1)
            },
            new CreatePropertyRequest
            {
                Name = "",
                Address = "Address",
                Price = 10_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 1)
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var list = await _client.GetFromJsonAsync<PagedResult<PropertyDto>>("/api/properties", JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(0, list.TotalCount);
    }

    [Fact]
    public async Task Batch_create_and_update_properties_succeed()
    {
        await ResetDatabaseAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/properties/batch", new[]
        {
            new CreatePropertyRequest
            {
                Name = "One",
                Address = "A1",
                Price = 11_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 1)
            },
            new CreatePropertyRequest
            {
                Name = "Two",
                Address = "A2",
                Price = 22_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 2)
            }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<List<PropertyDto>>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(2, created.Count);

        var updateResponse = await _client.PutAsJsonAsync("/api/properties/batch", new[]
        {
            new UpdatePropertyBatchItem
            {
                Id = created[0].Id,
                Name = "One Updated",
                Address = "A1",
                Price = 15_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 1)
            },
            new UpdatePropertyBatchItem
            {
                Id = created[1].Id,
                Name = "Two Updated",
                Address = "A2",
                Price = 25_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 2)
            }
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<List<PropertyDto>>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Contains(updated, item => item.Name == "One Updated" && item.Price == 15_000m);
        Assert.Contains(updated, item => item.Name == "Two Updated" && item.Price == 25_000m);
    }

    [Fact]
    public async Task Batch_update_returns_404_when_any_property_missing()
    {
        await ResetDatabaseAsync();
        var created = await CreatePropertyAsync(new CreatePropertyRequest
        {
            Name = "Only",
            Address = "Addr",
            Price = 10_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });

        var response = await _client.PutAsJsonAsync("/api/properties/batch", new[]
        {
            new UpdatePropertyBatchItem
            {
                Id = created.Id,
                Name = "Only",
                Address = "Addr",
                Price = 12_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 1)
            },
            new UpdatePropertyBatchItem
            {
                Id = Guid.NewGuid(),
                Name = "Missing",
                Address = "Addr",
                Price = 12_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2024, 1, 1)
            }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<PropertyDto> CreatePropertyAsync(CreatePropertyRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/properties", request);
        response.EnsureSuccessStatusCode();
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>(JsonOptions);
        Assert.NotNull(property);
        return property;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyInventoryDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
