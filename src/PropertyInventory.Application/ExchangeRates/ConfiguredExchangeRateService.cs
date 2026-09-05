using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Application.Common.Validation;

namespace PropertyInventory.Application.ExchangeRates;

/// <summary>
/// Fixed technical-test FX table. Seeded ownership USD amounts are stored historically and are not recalculated.
/// </summary>
public sealed class ConfiguredExchangeRateService : IExchangeRateService
{
    // EUR rate chosen to align with the brief sample (100,000 EUR → 108,733 USD).
    private static readonly IReadOnlyDictionary<string, decimal> UsdRates =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 1m,
            ["EUR"] = 1.08733m,
            ["GBP"] = 1.27000m
        };

    public decimal ConvertToUsd(decimal amount, string currencyCode)
    {
        if (!InputRules.IsValidCurrency(currencyCode))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["AcquisitionCurrency"] = ["Currency must be a 3-letter ISO 4217 code."]
            });
        }

        var currency = InputRules.NormalizeCurrency(currencyCode);
        if (!UsdRates.TryGetValue(currency, out var rate))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["AcquisitionCurrency"] = [$"No configured USD exchange rate for '{currency}'."]
            });
        }

        return Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
    }
}
