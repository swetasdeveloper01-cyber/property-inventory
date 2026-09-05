using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.ExchangeRates;
using PropertyInventory.Application.Ownerships;
using PropertyInventory.Domain.Entities;
using PropertyInventory.Infrastructure.Persistence;

namespace PropertyInventory.UnitTests.Application;

public class OwnershipServiceTests
{
    [Fact]
    public async Task CreateAsync_transfers_current_owner_and_leaves_asking_price_unchanged()
    {
        await using var db = CreateContext();
        var propertyId = Guid.NewGuid();
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();

        db.Properties.Add(new Property
        {
            Id = propertyId,
            Name = "Villa",
            Address = "1 Road",
            Price = 130_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2025, 1, 1)
        });
        db.Contacts.AddRange(
            new Contact
            {
                Id = ownerAId,
                FirstName = "Owner",
                LastName = "A",
                PhoneNumber = "1",
                Email = "a@example.com"
            },
            new Contact
            {
                Id = ownerBId,
                FirstName = "Owner",
                LastName = "B",
                PhoneNumber = "2",
                Email = "b@example.com"
            });
        db.PropertyOwnerships.Add(new PropertyOwnership
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            ContactId = ownerAId,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTill = null,
            AcquisitionPrice = 110_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 119_606.30m
        });
        await db.SaveChangesAsync();

        var service = new OwnershipService(db, new ConfiguredExchangeRateService());
        var created = await service.CreateAsync(propertyId, new CreateOwnershipRequest
        {
            ContactId = ownerBId,
            EffectiveFrom = new DateOnly(2026, 9, 10),
            EffectiveTill = null,
            AcquisitionPrice = 120_000m,
            AcquisitionCurrency = "EUR"
        });

        var ownerships = await db.PropertyOwnerships
            .Where(item => item.PropertyId == propertyId)
            .OrderBy(item => item.EffectiveFrom)
            .ToListAsync();

        Assert.Equal(2, ownerships.Count);
        Assert.Equal(new DateOnly(2026, 9, 10), ownerships[0].EffectiveTill);
        Assert.Equal(ownerAId, ownerships[0].ContactId);
        Assert.Equal(110_000m, ownerships[0].AcquisitionPrice);

        Assert.Equal(ownerBId, ownerships[1].ContactId);
        Assert.Equal(new DateOnly(2026, 9, 10), ownerships[1].EffectiveFrom);
        Assert.Null(ownerships[1].EffectiveTill);
        Assert.Equal(120_000m, ownerships[1].AcquisitionPrice);
        Assert.Equal(130_479.60m, ownerships[1].AcquisitionPriceUsd);
        Assert.Equal(created.Id, ownerships[1].Id);

        var property = await db.Properties.SingleAsync(item => item.Id == propertyId);
        Assert.Equal(130_000m, property.Price);
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_date_range()
    {
        await using var db = CreateContext();
        var (propertyId, contactId) = await SeedPropertyAndContactAsync(db);
        var service = new OwnershipService(db, new ConfiguredExchangeRateService());

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(propertyId, new CreateOwnershipRequest
        {
            ContactId = contactId,
            EffectiveFrom = new DateOnly(2026, 9, 10),
            EffectiveTill = new DateOnly(2026, 9, 10),
            AcquisitionPrice = 1m,
            AcquisitionCurrency = "EUR"
        }));
    }

    [Fact]
    public async Task CreateAsync_rejects_overlapping_historical_period()
    {
        await using var db = CreateContext();
        var propertyId = Guid.NewGuid();
        var contactA = Guid.NewGuid();
        var contactB = Guid.NewGuid();

        db.Properties.Add(new Property
        {
            Id = propertyId,
            Name = "Flat",
            Address = "Addr",
            Price = 90_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });
        db.Contacts.AddRange(
            CreateContact(contactA, "a@example.com"),
            CreateContact(contactB, "b@example.com"));
        db.PropertyOwnerships.Add(new PropertyOwnership
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            ContactId = contactA,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTill = new DateOnly(2026, 6, 1),
            AcquisitionPrice = 80_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 87_000m
        });
        await db.SaveChangesAsync();

        var service = new OwnershipService(db, new ConfiguredExchangeRateService());
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(propertyId, new CreateOwnershipRequest
        {
            ContactId = contactB,
            EffectiveFrom = new DateOnly(2026, 5, 1),
            EffectiveTill = new DateOnly(2026, 7, 1),
            AcquisitionPrice = 85_000m,
            AcquisitionCurrency = "EUR"
        }));
    }

    [Fact]
    public async Task CreateAsync_allows_contiguous_historical_period()
    {
        await using var db = CreateContext();
        var propertyId = Guid.NewGuid();
        var contactA = Guid.NewGuid();
        var contactB = Guid.NewGuid();

        db.Properties.Add(new Property
        {
            Id = propertyId,
            Name = "Flat",
            Address = "Addr",
            Price = 90_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });
        db.Contacts.AddRange(
            CreateContact(contactA, "a@example.com"),
            CreateContact(contactB, "b@example.com"));
        db.PropertyOwnerships.Add(new PropertyOwnership
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            ContactId = contactA,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTill = new DateOnly(2026, 6, 1),
            AcquisitionPrice = 80_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 87_000m
        });
        await db.SaveChangesAsync();

        var service = new OwnershipService(db, new ConfiguredExchangeRateService());
        var created = await service.CreateAsync(propertyId, new CreateOwnershipRequest
        {
            ContactId = contactB,
            EffectiveFrom = new DateOnly(2026, 6, 1),
            EffectiveTill = new DateOnly(2026, 8, 1),
            AcquisitionPrice = 85_000m,
            AcquisitionCurrency = "EUR"
        });

        Assert.Equal(new DateOnly(2026, 6, 1), created.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 8, 1), created.EffectiveTill);
        Assert.False(created.IsCurrent);
    }

    [Fact]
    public async Task CreateAsync_does_not_persist_partial_transfer_when_save_fails()
    {
        var databaseName = $"OwnershipAtomic-{Guid.NewGuid()}";
        var propertyId = Guid.NewGuid();
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();

        await using (var setup = CreateContext(databaseName))
        {
            setup.Properties.Add(new Property
            {
                Id = propertyId,
                Name = "Villa",
                Address = "1 Road",
                Price = 130_000m,
                Currency = "EUR",
                DateOfRegistration = new DateOnly(2025, 1, 1)
            });
            setup.Contacts.AddRange(
                CreateContact(ownerAId, "a@example.com"),
                CreateContact(ownerBId, "b@example.com"));
            setup.PropertyOwnerships.Add(new PropertyOwnership
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                ContactId = ownerAId,
                EffectiveFrom = new DateOnly(2026, 1, 1),
                EffectiveTill = null,
                AcquisitionPrice = 110_000m,
                AcquisitionCurrency = "EUR",
                AcquisitionPriceUsd = 119_606.30m
            });
            await setup.SaveChangesAsync();
        }

        await using (var act = CreateContext(databaseName, failOnOwnershipInsert: true))
        {
            var service = new OwnershipService(act, new ConfiguredExchangeRateService());
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(propertyId, new CreateOwnershipRequest
            {
                ContactId = ownerBId,
                EffectiveFrom = new DateOnly(2026, 9, 10),
                EffectiveTill = null,
                AcquisitionPrice = 120_000m,
                AcquisitionCurrency = "EUR"
            }));
        }

        await using var verify = CreateContext(databaseName);
        var ownerships = await verify.PropertyOwnerships
            .Where(item => item.PropertyId == propertyId)
            .ToListAsync();

        Assert.Single(ownerships);
        Assert.Equal(ownerAId, ownerships[0].ContactId);
        Assert.Null(ownerships[0].EffectiveTill);
    }

    private static async Task<(Guid PropertyId, Guid ContactId)> SeedPropertyAndContactAsync(
        PropertyInventoryDbContext db)
    {
        var propertyId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        db.Properties.Add(new Property
        {
            Id = propertyId,
            Name = "X",
            Address = "Y",
            Price = 1m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        });
        db.Contacts.Add(CreateContact(contactId, $"{contactId:N}@example.com"));
        await db.SaveChangesAsync();
        return (propertyId, contactId);
    }

    private static Contact CreateContact(Guid id, string email) => new()
    {
        Id = id,
        FirstName = "Test",
        LastName = "User",
        PhoneNumber = "1",
        Email = email
    };

    private static PropertyInventoryDbContext CreateContext(
        string? databaseName = null,
        bool failOnOwnershipInsert = false)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PropertyInventoryDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"OwnershipServiceTests-{Guid.NewGuid()}");

        if (failOnOwnershipInsert)
        {
            optionsBuilder.AddInterceptors(new FailOnOwnershipInsertInterceptor());
        }

        return new PropertyInventoryDbContext(optionsBuilder.Options);
    }
}
