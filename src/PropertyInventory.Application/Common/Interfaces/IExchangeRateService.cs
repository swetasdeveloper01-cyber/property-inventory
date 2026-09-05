namespace PropertyInventory.Application.Common.Interfaces;

/// <summary>
/// Deterministic currency conversion used when recording acquisition prices in USD.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Converts an amount to USD using configured technical-test rates (no live FX API).
    /// </summary>
    decimal ConvertToUsd(decimal amount, string currencyCode);
}
