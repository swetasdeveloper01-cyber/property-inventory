namespace PropertyInventory.Application.Properties;

public sealed class UpdatePropertyBatchItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateOnly DateOfRegistration { get; set; }
}
