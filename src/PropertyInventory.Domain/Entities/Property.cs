namespace PropertyInventory.Domain.Entities;

/// <summary>
/// A property held in inventory, including its current asking price and registration date.
/// Historical prices and ownership are tracked via related collections, not overwritten in place.
/// </summary>
public class Property
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Current asking/list price. Distinct from acquisition (sold-at) amounts on ownership periods.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// ISO 4217 currency code for <see cref="Price"/> (for example, EUR).
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    public DateOnly DateOfRegistration { get; set; }

    public ICollection<PropertyOwnership> Ownerships { get; set; } = new List<PropertyOwnership>();

    public ICollection<PropertyPriceHistory> PriceHistory { get; set; } = new List<PropertyPriceHistory>();
}
