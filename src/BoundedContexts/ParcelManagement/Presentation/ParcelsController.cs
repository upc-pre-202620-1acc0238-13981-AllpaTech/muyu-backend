using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.ParcelManagement.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.ParcelManagement.Presentation;

[ApiController]
[Authorize]
[Route("api/parcels")]
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
        var parcel = await registerHandler.HandleAsync(command, cancellationToken);
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
    public async Task<ActionResult<IReadOnlyList<ParcelResponse>>> ListByProducer(
        [FromQuery] Guid producerId,
        CancellationToken cancellationToken) =>
        Ok(await listHandler.HandleAsync(new ListParcelsByProducerQuery(producerId), cancellationToken));

    [HttpPost("{id:guid}/activate")]
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
}
