using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.Escrow.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.Escrow.Presentation;

[ApiController]
[Authorize]
[Route("api/escrow")]
[Route("api/v1/escrow")]
public sealed class EscrowController(
    ICommandHandler<OpenEscrowCommand, EscrowAccountResponse> openHandler,
    IQueryHandler<GetEscrowByIdQuery, EscrowAccountResponse?> getByIdHandler,
    ICommandHandler<FundEscrowCommand, bool> fundHandler,
    ICommandHandler<ReleasePayoutCommand, PayoutResponse?> releaseHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "COMERCIANTE,ADMIN")]
    public async Task<ActionResult<EscrowAccountResponse>> Open(
        OpenEscrowCommand command,
        CancellationToken cancellationToken)
    {
        var account = await openHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpGet("{id:guid}")]
    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<EscrowAccountResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await getByIdHandler.HandleAsync(new GetEscrowByIdQuery(id), cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost("{id:guid}/fund")]
    [Authorize(Roles = "COMERCIANTE,ADMIN")]
    public async Task<IActionResult> Fund(
        Guid id,
        FundEscrowRequest request,
        CancellationToken cancellationToken) =>
        await fundHandler.HandleAsync(new FundEscrowCommand(id, request.Amount), cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpPost("fund")]
    [Authorize(Roles = "COMERCIANTE,ADMIN")]
    public async Task<IActionResult> FundByAccount(
        FundEscrowByAccountRequest request,
        CancellationToken cancellationToken) =>
        await fundHandler.HandleAsync(
            new FundEscrowCommand(request.EscrowAccountId, request.Amount),
            cancellationToken)
                ? NoContent()
                : NotFound();

    [HttpPost("{id:guid}/payouts")]
    [HttpPost("{id:guid}/release")]
    [Authorize(Roles = "COMERCIANTE,ADMIN")]
    [ProducesResponseType<PayoutResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PayoutResponse>> Release(
        Guid id,
        ReleasePayoutRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReleasePayoutCommand(
            id,
            request.RecipientId,
            request.Amount,
            request.Purpose);
        var payout = await releaseHandler.HandleAsync(command, cancellationToken);
        return payout is null ? NotFound() : Ok(payout);
    }
}
