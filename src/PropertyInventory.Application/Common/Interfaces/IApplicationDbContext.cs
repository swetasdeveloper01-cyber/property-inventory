using Microsoft.EntityFrameworkCore;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Application.Common.Interfaces;

/// <summary>
/// Persistence port used by application services. Implemented by EF Core in Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Property> Properties { get; }

    DbSet<Contact> Contacts { get; }

    DbSet<PropertyOwnership> PropertyOwnerships { get; }

    DbSet<PropertyPriceHistory> PropertyPriceHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
