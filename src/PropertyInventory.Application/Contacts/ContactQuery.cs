using PropertyInventory.Application.Common.Models;

namespace PropertyInventory.Application.Contacts;

public sealed class ContactQuery : PaginationParameters
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }
}
