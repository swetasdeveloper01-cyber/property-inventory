using Microsoft.AspNetCore.Mvc;
using PropertyInventory.Application.Prices;

namespace PropertyInventory.Api.Controllers;

[ApiController]
[Route("api/properties/{propertyId:guid}/prices")]
public class PropertyPricesController : ControllerBase
{
    private readonly PropertyPriceService _propertyPriceService;

    public PropertyPricesController(PropertyPriceService propertyPriceService)
    {
        _propertyPriceService = propertyPriceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyPriceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PropertyPriceDto>>> GetAsync(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var prices = await _propertyPriceService.GetByPropertyIdAsync(propertyId, cancellationToken);
        return Ok(prices);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PropertyPriceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyPriceDto>> CreateAsync(
        Guid propertyId,
        [FromBody] CreatePropertyPriceRequest request,
        CancellationToken cancellationToken)
    {
        var price = await _propertyPriceService.CreateAsync(propertyId, request, cancellationToken);
        return Created($"/api/properties/{propertyId}/prices/{price.Id}", price);
    }
}
