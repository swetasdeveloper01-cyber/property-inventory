namespace PropertyInventory.Application.Prices;

public sealed class PropertyPriceDto
{
    public Guid Id { get; init; }

    public Guid PropertyId { get; init; }

    public decimal Amount { get; init; }

    public required string Currency { get; init; }

    public DateOnly EffectiveDate { get; init; }
}
