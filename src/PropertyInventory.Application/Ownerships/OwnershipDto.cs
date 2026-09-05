namespace PropertyInventory.Application.Ownerships;

public sealed class OwnershipDto
{
    public Guid Id { get; init; }

    public Guid PropertyId { get; init; }

    public Guid ContactId { get; init; }

    public required string OwnerFirstName { get; init; }

    public required string OwnerLastName { get; init; }

    public required string OwnerEmail { get; init; }

    public DateOnly EffectiveFrom { get; init; }

    public DateOnly? EffectiveTill { get; init; }

    public decimal AcquisitionPrice { get; init; }

    public required string AcquisitionCurrency { get; init; }

    public decimal AcquisitionPriceUsd { get; init; }

    public bool IsCurrent => EffectiveTill is null;
}
