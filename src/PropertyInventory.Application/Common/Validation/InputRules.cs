using System.Text.RegularExpressions;

namespace PropertyInventory.Application.Common.Validation;

internal static class InputRules
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex CurrencyRegex = new(
        "^[A-Za-z]{3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

    public static bool IsValidCurrency(string? currency) =>
        !string.IsNullOrWhiteSpace(currency) && CurrencyRegex.IsMatch(currency.Trim());

    public static string NormalizeCurrency(string currency) => currency.Trim().ToUpperInvariant();

    public static string NormalizeEmail(string email) => email.Trim();
}
