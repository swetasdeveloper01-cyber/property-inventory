namespace PropertyInventory.Application.Ownerships;

public sealed class CreateOwnershipRequest
{
    public Guid ContactId { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// When null, the ownership becomes the current period (and closes any existing current owner).
    /// When set, a historical closed period is recorded without changing the current owner,
    /// provided temporal rules are satisfied.
    /// </summary>
    public DateOnly? EffectiveTill { get; set; }

    public decimal AcquisitionPrice { get; set; }

    public string AcquisitionCurrency { get; set; } = string.Empty;
}
