using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Contacts;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.UnitTests.Application;

public class ContactServiceTests
{
    [Fact]
    public async Task CreateAsync_rejects_invalid_email()
    {
        await using var db = CreateContext();
        var service = new ContactService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateContactRequest
        {
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "1",
            Email = "bad"
        }));
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_email()
    {
        await using var db = CreateContext();
        var service = new ContactService(db);

        await service.CreateAsync(new CreateContactRequest
        {
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "1",
            Email = "dup@example.com"
        });

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateContactRequest
        {
            FirstName = "C",
            LastName = "D",
            PhoneNumber = "2",
            Email = "DUP@example.com"
        }));
    }

    private static PropertyInventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PropertyInventoryDbContext>()
            .UseInMemoryDatabase($"ContactServiceTests-{Guid.NewGuid()}")
            .Options;
        return new PropertyInventoryDbContext(options);
    }
}
