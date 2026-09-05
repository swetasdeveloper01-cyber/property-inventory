using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.UnitTests.Application;

/// <summary>
/// Forces SaveChanges to fail when a new ownership row is being inserted,
/// so transfer atomicity can be verified against InMemory.
/// </summary>
internal sealed class FailOnOwnershipInsertInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ThrowIfAddingOwnership(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfAddingOwnership(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ThrowIfAddingOwnership(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var addingOwnership = context.ChangeTracker
            .Entries<PropertyOwnership>()
            .Any(entry => entry.State == EntityState.Added);

        if (addingOwnership)
        {
            throw new InvalidOperationException("Simulated failure while inserting ownership.");
        }
    }
}
