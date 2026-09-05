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

    /// <summary>
    /// Begins a database transaction when the provider supports it; otherwise returns null.
    /// Ownership transfer uses this so close+create persists atomically on relational providers.
    /// </summary>
    Task<IApplicationTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
