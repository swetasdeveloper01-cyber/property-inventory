namespace PropertyInventory.Application.Properties;

public sealed class PropertyDto
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Address { get; init; }

    public decimal Price { get; init; }

    public required string Currency { get; init; }

    public DateOnly DateOfRegistration { get; init; }
}
