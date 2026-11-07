using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.ParcelManagement.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.ParcelManagement.Presentation;

[ApiController]
[Authorize]
[Route("api/v1/parcels")]
public sealed class ParcelsController(
    ICommandHandler<RegisterParcelCommand, ParcelResponse> registerHandler,
    IQueryHandler<GetParcelByIdQuery, ParcelResponse?> getByIdHandler,
    IQueryHandler<ListParcelsByProducerQuery, IReadOnlyList<ParcelResponse>> listHandler,
    IQueryHandler<ListAvailableParcelsQuery, IReadOnlyList<ParcelResponse>> catalogHandler,
    ICommandHandler<ActivateParcelCommand, bool> activateHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "AGRICULTOR,ADMIN")]
    [ProducesResponseType<ParcelResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ParcelResponse>> Register(
        RegisterParcelCommand command,
        CancellationToken cancellationToken)
    {
        var ownerId = GetAuthenticatedUserId();
        var parcel = await registerHandler.HandleAsync(
            command with { ProducerId = ownerId },
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = parcel.Id }, parcel);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ParcelResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParcelResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var parcel = await getByIdHandler.HandleAsync(new GetParcelByIdQuery(id), cancellationToken);
        return parcel is null ? NotFound() : Ok(parcel);
    }

    [HttpGet]
    [Authorize(Roles = "AGRICULTOR,ADMIN")]
    public async Task<ActionResult<IReadOnlyList<ParcelResponse>>> ListByProducer(
        [FromQuery] Guid? producerId,
        CancellationToken cancellationToken)
    {
        var requestedProducerId = User.IsInRole("ADMIN") && producerId.HasValue
            ? producerId.Value
            : GetAuthenticatedUserId();

        return Ok(await listHandler.HandleAsync(
            new ListParcelsByProducerQuery(requestedProducerId),
            cancellationToken));
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        await activateHandler.HandleAsync(new ActivateParcelCommand(id), cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpGet("/api/v1/catalog/parcels")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ParcelResponse>>> Catalog(
        CancellationToken cancellationToken) =>
        Ok(await catalogHandler.HandleAsync(new ListAvailableParcelsQuery(), cancellationToken));

    private Guid GetAuthenticatedUserId()
    {
        var identifier = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(identifier, out var userId)
            ? userId
            : throw new InvalidOperationException(
                "El token no contiene un identificador de usuario válido.");
    }
}
