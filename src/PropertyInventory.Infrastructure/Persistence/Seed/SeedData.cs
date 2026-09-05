using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Infrastructure.Persistence.Seed;

/// <summary>
/// Deterministic sample data aligned with the technical-test dashboard examples
/// (Maisonette ownership changes and a Penthouse sale).
/// </summary>
public static class SeedData
{
    public static readonly Guid MaisonetteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PenthouseId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid TownhouseId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid CarmenAttardId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid JoshuaMifsudId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid JoeBorgId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid MariaGaleaId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static IReadOnlyList<Contact> CreateContacts() =>
    [
        new Contact
        {
            Id = CarmenAttardId,
            FirstName = "Carmen",
            LastName = "Attard",
            PhoneNumber = "+356 2100 1001",
            Email = "carmen.attard@example.com"
        },
        new Contact
        {
            Id = JoshuaMifsudId,
            FirstName = "Joshua",
            LastName = "Mifsud",
            PhoneNumber = "+356 2100 1002",
            Email = "joshua.mifsud@example.com"
        },
        new Contact
        {
            Id = JoeBorgId,
            FirstName = "Joe",
            LastName = "Borg",
            PhoneNumber = "+356 2100 1003",
            Email = "joe.borg@example.com"
        },
        new Contact
        {
            Id = MariaGaleaId,
            FirstName = "Maria",
            LastName = "Galea",
            PhoneNumber = "+356 2100 1004",
            Email = "maria.galea@example.com"
        }
    ];

    public static IReadOnlyList<Property> CreateProperties() =>
    [
        new Property
        {
            Id = MaisonetteId,
            Name = "Maisonette",
            Address = "12 Triq il-Kbira, Sliema",
            Price = 130_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2023, 1, 10)
        },
        new Property
        {
            Id = PenthouseId,
            Name = "Penthouse",
            Address = "5 Tower Road, Gzira",
            Price = 430_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2023, 3, 1)
        },
        new Property
        {
            Id = TownhouseId,
            Name = "Townhouse",
            Address = "8 St. Paul's Street, Valletta",
            Price = 275_000m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2022, 11, 20)
        }
    ];

    public static IReadOnlyList<PropertyOwnership> CreateOwnerships() =>
    [
        // Maisonette: sold to Joshua, later to Carmen (matches brief sample rows).
        new PropertyOwnership
        {
            Id = Guid.Parse("e1111111-1111-1111-1111-111111111111"),
            PropertyId = MaisonetteId,
            ContactId = JoshuaMifsudId,
            EffectiveFrom = new DateOnly(2023, 7, 25),
            EffectiveTill = new DateOnly(2024, 1, 15),
            AcquisitionPrice = 100_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 108_733m
        },
        new PropertyOwnership
        {
            Id = Guid.Parse("e2222222-2222-2222-2222-222222222222"),
            PropertyId = MaisonetteId,
            ContactId = CarmenAttardId,
            EffectiveFrom = new DateOnly(2024, 1, 15),
            EffectiveTill = null,
            AcquisitionPrice = 120_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 130_480m
        },
        // Penthouse: current owner Joe Borg.
        new PropertyOwnership
        {
            Id = Guid.Parse("e3333333-3333-3333-3333-333333333333"),
            PropertyId = PenthouseId,
            ContactId = JoeBorgId,
            EffectiveFrom = new DateOnly(2023, 5, 6),
            EffectiveTill = null,
            AcquisitionPrice = 400_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 435_072m
        },
        // Townhouse: current owner Maria Galea.
        new PropertyOwnership
        {
            Id = Guid.Parse("e4444444-4444-4444-4444-444444444444"),
            PropertyId = TownhouseId,
            ContactId = MariaGaleaId,
            EffectiveFrom = new DateOnly(2022, 12, 1),
            EffectiveTill = null,
            AcquisitionPrice = 250_000m,
            AcquisitionCurrency = "EUR",
            AcquisitionPriceUsd = 271_875m
        }
    ];

    public static IReadOnlyList<PropertyPriceHistory> CreatePriceHistory() =>
    [
        new PropertyPriceHistory
        {
            Id = Guid.Parse("f1111111-1111-1111-1111-111111111111"),
            PropertyId = MaisonetteId,
            Amount = 110_000m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2023, 1, 10)
        },
        new PropertyPriceHistory
        {
            Id = Guid.Parse("f2222222-2222-2222-2222-222222222222"),
            PropertyId = MaisonetteId,
            Amount = 130_000m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2024, 1, 10)
        },
        new PropertyPriceHistory
        {
            Id = Guid.Parse("f3333333-3333-3333-3333-333333333333"),
            PropertyId = PenthouseId,
            Amount = 430_000m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2023, 3, 1)
        },
        new PropertyPriceHistory
        {
            Id = Guid.Parse("f4444444-4444-4444-4444-444444444444"),
            PropertyId = TownhouseId,
            Amount = 260_000m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2022, 11, 20)
        },
        new PropertyPriceHistory
        {
            Id = Guid.Parse("f5555555-5555-5555-5555-555555555555"),
            PropertyId = TownhouseId,
            Amount = 275_000m,
            Currency = "EUR",
            EffectiveDate = new DateOnly(2024, 6, 1)
        }
    ];
}
