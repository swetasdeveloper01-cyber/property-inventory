using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PropertyInventory.Application.Common.Models;
using PropertyInventory.Application.Contacts;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.IntegrationTests;

public class ContactsApiTests : IClassFixture<PropertyInventoryApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private readonly PropertyInventoryApiFactory _factory;

    public ContactsApiTests(PropertyInventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_contacts_returns_paginated_list()
    {
        await ResetDatabaseAsync();
        await CreateContactAsync("Anna", "Azzopardi", "anna@example.com", "+356 1000");
        await CreateContactAsync("Ben", "Borg", "ben@example.com", "+356 2000");

        var response = await _client.GetAsync("/api/contacts?page=1&pageSize=1");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ContactDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Get_contacts_supports_filtering()
    {
        await ResetDatabaseAsync();
        await CreateContactAsync("Carmen", "Attard", "carmen.attard@example.com", "+356 2100 1001");
        await CreateContactAsync("Joshua", "Mifsud", "joshua.mifsud@example.com", "+356 2100 1002");

        var response = await _client.GetAsync("/api/contacts?lastName=att&email=carmen&phone=2100");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ContactDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("Carmen", page.Items[0].FirstName);
    }

    [Fact]
    public async Task Get_contact_by_id_returns_contact()
    {
        await ResetDatabaseAsync();
        var created = await CreateContactAsync("Joe", "Borg", "joe.borg@example.com", "+356 3000");

        var response = await _client.GetAsync($"/api/contacts/{created.Id}");
        var contact = await response.Content.ReadFromJsonAsync<ContactDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(contact);
        Assert.Equal(created.Id, contact.Id);
        Assert.Equal("Joe", contact.FirstName);
    }

    [Fact]
    public async Task Get_contact_by_id_returns_404_when_missing()
    {
        await ResetDatabaseAsync();
        var response = await _client.GetAsync($"/api/contacts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_contact_returns_201()
    {
        await ResetDatabaseAsync();
        var response = await _client.PostAsJsonAsync("/api/contacts", new CreateContactRequest
        {
            FirstName = "Maria",
            LastName = "Galea",
            PhoneNumber = "+356 4000",
            Email = " maria.galea@example.com "
        });

        var contact = await response.Content.ReadFromJsonAsync<ContactDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(contact);
        Assert.Equal("maria.galea@example.com", contact.Email);
    }

    [Fact]
    public async Task Create_contact_returns_400_for_validation_failure()
    {
        await ResetDatabaseAsync();
        var response = await _client.PostAsJsonAsync("/api/contacts", new CreateContactRequest
        {
            FirstName = "",
            LastName = "",
            PhoneNumber = "",
            Email = "not-an-email"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_contact_returns_409_for_duplicate_email()
    {
        await ResetDatabaseAsync();
        await CreateContactAsync("One", "User", "shared@example.com", "+356 1");

        var response = await _client.PostAsJsonAsync("/api/contacts", new CreateContactRequest
        {
            FirstName = "Two",
            LastName = "User",
            PhoneNumber = "+356 2",
            Email = "SHARED@example.com"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_contact_returns_updated_values()
    {
        await ResetDatabaseAsync();
        var created = await CreateContactAsync("Old", "Name", "old@example.com", "+356 1");

        var response = await _client.PutAsJsonAsync($"/api/contacts/{created.Id}", new UpdateContactRequest
        {
            FirstName = "New",
            LastName = "Name",
            PhoneNumber = "+356 9",
            Email = "new@example.com"
        });

        var updated = await response.Content.ReadFromJsonAsync<ContactDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("New", updated.FirstName);
        Assert.Equal("new@example.com", updated.Email);
    }

    [Fact]
    public async Task Batch_create_and_update_contacts_succeed()
    {
        await ResetDatabaseAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/contacts/batch", new[]
        {
            new CreateContactRequest
            {
                FirstName = "A",
                LastName = "One",
                PhoneNumber = "+356 1",
                Email = "a.one@example.com"
            },
            new CreateContactRequest
            {
                FirstName = "B",
                LastName = "Two",
                PhoneNumber = "+356 2",
                Email = "b.two@example.com"
            }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<List<ContactDto>>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(2, created.Count);

        var updateResponse = await _client.PutAsJsonAsync("/api/contacts/batch", new[]
        {
            new UpdateContactBatchItem
            {
                Id = created[0].Id,
                FirstName = "A2",
                LastName = "One",
                PhoneNumber = "+356 1",
                Email = "a.one@example.com"
            },
            new UpdateContactBatchItem
            {
                Id = created[1].Id,
                FirstName = "B2",
                LastName = "Two",
                PhoneNumber = "+356 2",
                Email = "b.two@example.com"
            }
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<List<ContactDto>>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Contains(updated, item => item.FirstName == "A2");
        Assert.Contains(updated, item => item.FirstName == "B2");
    }

    [Fact]
    public async Task Batch_update_returns_404_when_any_contact_missing()
    {
        await ResetDatabaseAsync();
        var created = await CreateContactAsync("Only", "Contact", "only@example.com", "+356 1");

        var response = await _client.PutAsJsonAsync("/api/contacts/batch", new[]
        {
            new UpdateContactBatchItem
            {
                Id = created.Id,
                FirstName = "Only",
                LastName = "Contact",
                PhoneNumber = "+356 1",
                Email = "only@example.com"
            },
            new UpdateContactBatchItem
            {
                Id = Guid.NewGuid(),
                FirstName = "Missing",
                LastName = "Contact",
                PhoneNumber = "+356 2",
                Email = "missing@example.com"
            }
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ContactDto> CreateContactAsync(
        string firstName,
        string lastName,
        string email,
        string phone)
    {
        var response = await _client.PostAsJsonAsync("/api/contacts", new CreateContactRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phone
        });
        response.EnsureSuccessStatusCode();
        var contact = await response.Content.ReadFromJsonAsync<ContactDto>(JsonOptions);
        Assert.NotNull(contact);
        return contact;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyInventoryDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
