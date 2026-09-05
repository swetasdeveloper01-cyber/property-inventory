namespace PropertyInventory.Domain.Entities;

/// <summary>
/// A historical asking-price point for a property. Created whenever the property price changes.
/// </summary>
public class PropertyPriceHistory
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Property Property { get; set; } = null!;

    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code for <see cref="Amount"/>.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    public DateOnly EffectiveDate { get; set; }
}
