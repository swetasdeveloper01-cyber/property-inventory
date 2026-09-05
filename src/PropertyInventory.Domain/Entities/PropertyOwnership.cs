namespace PropertyInventory.Domain.Entities;

/// <summary>
/// A single ownership period for a property. Overlapping periods for the same property are invalid.
/// <see cref="EffectiveTill"/> is null while this period is the current ownership.
/// </summary>
public class PropertyOwnership
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Property Property { get; set; } = null!;

    public Guid ContactId { get; set; }

    public Contact Contact { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Inclusive end of the ownership period, or null when this is the active ownership.
    /// </summary>
    public DateOnly? EffectiveTill { get; set; }

    /// <summary>
    /// Price paid to acquire the property for this ownership period (sold-at price).
    /// </summary>
    public decimal AcquisitionPrice { get; set; }

    /// <summary>
    /// ISO 4217 currency code for <see cref="AcquisitionPrice"/>.
    /// </summary>
    public string AcquisitionCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Deterministic USD equivalent of <see cref="AcquisitionPrice"/> at acquisition time.
    /// Stored so dashboard history remains stable without calling an external FX service.
    /// </summary>
    public decimal AcquisitionPriceUsd { get; set; }
}
