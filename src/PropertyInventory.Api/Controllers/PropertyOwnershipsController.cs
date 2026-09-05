using Microsoft.AspNetCore.Mvc;
using PropertyInventory.Application.Ownerships;

namespace PropertyInventory.Api.Controllers;

[ApiController]
[Route("api/properties/{propertyId:guid}/ownerships")]
public class PropertyOwnershipsController : ControllerBase
{
    private readonly OwnershipService _ownershipService;

    public PropertyOwnershipsController(OwnershipService ownershipService)
    {
        _ownershipService = ownershipService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OwnershipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OwnershipDto>>> GetAsync(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var ownerships = await _ownershipService.GetByPropertyIdAsync(propertyId, cancellationToken);
        return Ok(ownerships);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OwnershipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OwnershipDto>> CreateAsync(
        Guid propertyId,
        [FromBody] CreateOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var ownership = await _ownershipService.CreateAsync(propertyId, request, cancellationToken);
        return Created($"/api/properties/{propertyId}/ownerships/{ownership.Id}", ownership);
    }
}
