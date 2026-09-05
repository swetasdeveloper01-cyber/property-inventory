using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.UnitTests.Application;

internal sealed class FailOnPriceHistoryInsertInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ThrowIfAddingPriceHistory(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfAddingPriceHistory(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ThrowIfAddingPriceHistory(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var adding = context.ChangeTracker
            .Entries<PropertyPriceHistory>()
            .Any(entry => entry.State == EntityState.Added);

        if (adding)
        {
            throw new InvalidOperationException("Simulated failure while inserting price history.");
        }
    }
}
