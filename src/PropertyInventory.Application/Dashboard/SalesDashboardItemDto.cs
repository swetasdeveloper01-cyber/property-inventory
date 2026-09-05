namespace PropertyInventory.Application.Dashboard;

/// <summary>
/// One ownership/sale acquisition event for the sales dashboard.
/// </summary>
public sealed class SalesDashboardItemDto
{
    /// <summary>
    /// Ownership record identifier.
    /// </summary>
    public Guid Id { get; init; }

    public required string PropertyName { get; init; }

    /// <summary>
    /// Current property asking price (not the acquisition/sold price).
    /// </summary>
    public decimal AskingPrice { get; init; }

    public required string AskingCurrency { get; init; }

    /// <summary>
    /// Owner display name (contact first + last name).
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    /// Ownership acquisition start date (<c>EffectiveFrom</c>).
    /// </summary>
    public DateOnly DateOfPurchase { get; init; }

    /// <summary>
    /// Price paid by this owner when acquiring the property.
    /// </summary>
    public decimal SoldAtPrice { get; init; }

    public required string SoldAtCurrency { get; init; }

    /// <summary>
    /// Stored deterministic USD equivalent of the acquisition price.
    /// </summary>
    public decimal SoldAtPriceUsd { get; init; }
}
