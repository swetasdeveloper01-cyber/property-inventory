using PropertyInventory.Application.ExchangeRates;

namespace PropertyInventory.UnitTests.Application;

public class ConfiguredExchangeRateServiceTests
{
    private readonly ConfiguredExchangeRateService _service = new();

    [Fact]
    public void ConvertToUsd_uses_configured_eur_rate()
    {
        var usd = _service.ConvertToUsd(100_000m, "EUR");
        Assert.Equal(108_733m, usd);
    }

    [Fact]
    public void ConvertToUsd_is_case_insensitive_and_deterministic()
    {
        var first = _service.ConvertToUsd(250_000m, "eur");
        var second = _service.ConvertToUsd(250_000m, "EUR");
        Assert.Equal(first, second);
        Assert.Equal(271_832.50m, first);
    }

    [Fact]
    public void ConvertToUsd_leaves_usd_unchanged()
    {
        Assert.Equal(99.99m, _service.ConvertToUsd(99.99m, "USD"));
    }
}
