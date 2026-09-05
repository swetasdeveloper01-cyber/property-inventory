using PropertyInventory.Domain.Entities;
using PropertyInventory.Infrastructure.Persistence.Seed;

namespace PropertyInventory.UnitTests.Domain;

public class SeedDataTests
{
    [Fact]
    public void Seed_ownerships_include_maisonette_transfer_matching_brief_sample()
    {
        var ownerships = SeedData.CreateOwnerships()
            .Where(ownership => ownership.PropertyId == SeedData.MaisonetteId)
            .OrderBy(ownership => ownership.EffectiveFrom)
            .ToList();

        Assert.Equal(2, ownerships.Count);

        Assert.Equal(SeedData.JoshuaMifsudId, ownerships[0].ContactId);
        Assert.Equal(new DateOnly(2023, 7, 25), ownerships[0].EffectiveFrom);
        Assert.Equal(new DateOnly(2024, 1, 15), ownerships[0].EffectiveTill);
        Assert.Equal(100_000m, ownerships[0].AcquisitionPrice);
        Assert.Equal(108_733m, ownerships[0].AcquisitionPriceUsd);

        Assert.Equal(SeedData.CarmenAttardId, ownerships[1].ContactId);
        Assert.Equal(new DateOnly(2024, 1, 15), ownerships[1].EffectiveFrom);
        Assert.Null(ownerships[1].EffectiveTill);
        Assert.Equal(120_000m, ownerships[1].AcquisitionPrice);
        Assert.Equal(130_480m, ownerships[1].AcquisitionPriceUsd);
    }

    [Fact]
    public void Seed_keeps_at_most_one_current_ownership_per_property()
    {
        var currentByProperty = SeedData.CreateOwnerships()
            .Where(ownership => ownership.EffectiveTill is null)
            .GroupBy(ownership => ownership.PropertyId);

        Assert.All(currentByProperty, group => Assert.Single(group));
    }

    [Fact]
    public void Property_price_is_decimal_not_floating_point()
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Address = "Address",
            Price = 100_000.50m,
            Currency = "EUR",
            DateOfRegistration = new DateOnly(2024, 1, 1)
        };

        Assert.Equal(typeof(decimal), property.Price.GetType());
        Assert.Equal(100_000.50m, property.Price);
    }
}
