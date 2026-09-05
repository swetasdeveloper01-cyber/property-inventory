using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Interfaces;

namespace PropertyInventory.Application.Dashboard;

/// <summary>
/// Read-only sales dashboard: one row per ownership acquisition/sale event.
/// </summary>
public class DashboardService
{
    private readonly IApplicationDbContext _dbContext;

    public DashboardService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns ownership acquisition events with current asking price and sold-at amounts.
    /// Includes current ownership periods because each acquisition is a sale to that owner
    /// (aligned with the client sample that lists current owners such as Carmen Attard / Joe Borg).
    /// </summary>
    public async Task<IReadOnlyList<SalesDashboardItemDto>> GetSalesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PropertyOwnerships
            .AsNoTracking()
            .OrderByDescending(ownership => ownership.EffectiveFrom)
            .ThenBy(ownership => ownership.Property.Name)
            .ThenBy(ownership => ownership.Id)
            .Select(ownership => new SalesDashboardItemDto
            {
                Id = ownership.Id,
                PropertyName = ownership.Property.Name,
                AskingPrice = ownership.Property.Price,
                AskingCurrency = ownership.Property.Currency,
                Owner = ownership.Contact.FirstName + " " + ownership.Contact.LastName,
                DateOfPurchase = ownership.EffectiveFrom,
                SoldAtPrice = ownership.AcquisitionPrice,
                SoldAtCurrency = ownership.AcquisitionCurrency,
                SoldAtPriceUsd = ownership.AcquisitionPriceUsd
            })
            .ToListAsync(cancellationToken);
    }
}
