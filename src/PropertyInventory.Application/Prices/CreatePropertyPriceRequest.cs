namespace PropertyInventory.Application.Prices;

public sealed class CreatePropertyPriceRequest
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateOnly EffectiveDate { get; set; }
}
