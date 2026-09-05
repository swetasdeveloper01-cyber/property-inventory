using Microsoft.AspNetCore.Mvc;
using PropertyInventory.Application.Common.Models;
using PropertyInventory.Application.Properties;

namespace PropertyInventory.Api.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly PropertyService _propertyService;

    public PropertiesController(PropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PropertyDto>>> GetAsync(
        [FromQuery] PropertyQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var property = await _propertyService.GetByIdAsync(id, cancellationToken);
        return Ok(property);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PropertyDto>> CreateAsync(
        [FromBody] CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var property = await _propertyService.CreateAsync(request, cancellationToken);
        return Created($"/api/properties/{property.Id}", property);
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> CreateBatchAsync(
        [FromBody] List<CreatePropertyRequest> requests,
        CancellationToken cancellationToken)
    {
        var properties = await _propertyService.CreateBatchAsync(requests, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, properties);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        var property = await _propertyService.UpdateAsync(id, request, cancellationToken);
        return Ok(property);
    }

    [HttpPut("batch")]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> UpdateBatchAsync(
        [FromBody] List<UpdatePropertyBatchItem> requests,
        CancellationToken cancellationToken)
    {
        var properties = await _propertyService.UpdateBatchAsync(requests, cancellationToken);
        return Ok(properties);
    }
}
