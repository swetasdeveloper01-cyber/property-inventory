using Microsoft.AspNetCore.Mvc;
using PropertyInventory.Application.Contacts;

namespace PropertyInventory.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly ContactService _contactService;

    public ContactsController(ContactService contactService)
    {
        _contactService = contactService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] ContactQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _contactService.GetAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var contact = await _contactService.GetByIdAsync(id, cancellationToken);
        return Ok(contact);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContactDto>> CreateAsync(
        [FromBody] CreateContactRequest request,
        CancellationToken cancellationToken)
    {
        var contact = await _contactService.CreateAsync(request, cancellationToken);
        return Created($"/api/contacts/{contact.Id}", contact);
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(IReadOnlyList<ContactDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<ContactDto>>> CreateBatchAsync(
        [FromBody] List<CreateContactRequest> requests,
        CancellationToken cancellationToken)
    {
        var contacts = await _contactService.CreateBatchAsync(requests, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, contacts);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContactDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateContactRequest request,
        CancellationToken cancellationToken)
    {
        var contact = await _contactService.UpdateAsync(id, request, cancellationToken);
        return Ok(contact);
    }

    [HttpPut("batch")]
    [ProducesResponseType(typeof(IReadOnlyList<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<ContactDto>>> UpdateBatchAsync(
        [FromBody] List<UpdateContactBatchItem> requests,
        CancellationToken cancellationToken)
    {
        var contacts = await _contactService.UpdateBatchAsync(requests, cancellationToken);
        return Ok(contacts);
    }
}
