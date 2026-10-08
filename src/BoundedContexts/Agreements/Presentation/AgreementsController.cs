using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.Agreements.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.Agreements.Presentation;

[ApiController]
[Authorize]
[Route("api/agreements")]
public sealed class AgreementsController(
    ICommandHandler<CreateAgreementCommand, AgreementResponse> createHandler,
    IQueryHandler<GetAgreementByIdQuery, AgreementResponse?> getByIdHandler,
    IQueryHandler<ListAgreementsByProducerQuery, IReadOnlyList<AgreementResponse>> listHandler,
    ICommandHandler<ActivateAgreementCommand, bool> activateHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AgreementResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AgreementResponse>> Create(
        CreateAgreementCommand command,
        CancellationToken cancellationToken)
    {
        var agreement = await createHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = agreement.Id }, agreement);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgreementResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var agreement = await getByIdHandler.HandleAsync(new GetAgreementByIdQuery(id), cancellationToken);
        return agreement is null ? NotFound() : Ok(agreement);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgreementResponse>>> ListByProducer(
        [FromQuery] Guid producerId,
        CancellationToken cancellationToken) =>
        Ok(await listHandler.HandleAsync(new ListAgreementsByProducerQuery(producerId), cancellationToken));

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        await activateHandler.HandleAsync(new ActivateAgreementCommand(id), cancellationToken)
            ? NoContent()
            : NotFound();
}
