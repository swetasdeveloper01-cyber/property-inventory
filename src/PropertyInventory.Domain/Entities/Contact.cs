namespace PropertyInventory.Domain.Entities;

/// <summary>
/// A person who may own (or have owned) one or more properties over time.
/// </summary>
public class Contact
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ICollection<PropertyOwnership> Ownerships { get; set; } = new List<PropertyOwnership>();
}
