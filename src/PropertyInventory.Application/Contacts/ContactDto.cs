namespace PropertyInventory.Application.Contacts;

public sealed class ContactDto
{
    public Guid Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string PhoneNumber { get; init; }

    public required string Email { get; init; }
}
