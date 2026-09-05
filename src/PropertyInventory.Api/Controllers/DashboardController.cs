using Microsoft.AspNetCore.Mvc;
using PropertyInventory.Application.Dashboard;

namespace PropertyInventory.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("sales")]
    [ProducesResponseType(typeof(IReadOnlyList<SalesDashboardItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalesDashboardItemDto>>> GetSalesAsync(
        CancellationToken cancellationToken)
    {
        var sales = await _dashboardService.GetSalesAsync(cancellationToken);
        return Ok(sales);
    }
}
