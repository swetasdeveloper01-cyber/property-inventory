using PropertyInventory.Application.Ownerships;

namespace PropertyInventory.UnitTests.Application;

public class OwnershipPeriodOverlapTests
{
    [Theory]
    [InlineData("2026-01-01", "2026-09-10", "2026-09-10", null, false)] // contiguous transfer boundary
    [InlineData("2026-01-01", "2026-09-10", "2026-09-11", null, false)] // gap after closed period
    [InlineData("2026-01-01", "2026-09-10", "2026-09-09", null, true)] // overlaps closed period
    [InlineData("2026-01-01", null, "2026-09-10", null, true)] // two current periods
    [InlineData("2026-01-01", "2026-06-01", "2026-03-01", "2026-04-01", true)] // nested historical
    [InlineData("2026-01-01", "2026-03-01", "2026-03-01", "2026-05-01", false)] // contiguous historical
    public void PeriodsOverlap_matches_half_open_boundary_rules(
        string leftFrom,
        string? leftTill,
        string rightFrom,
        string? rightTill,
        bool expectedOverlap)
    {
        var overlap = OwnershipService.PeriodsOverlap(
            DateOnly.Parse(leftFrom),
            leftTill is null ? null : DateOnly.Parse(leftTill),
            DateOnly.Parse(rightFrom),
            rightTill is null ? null : DateOnly.Parse(rightTill));

        Assert.Equal(expectedOverlap, overlap);
    }
}
