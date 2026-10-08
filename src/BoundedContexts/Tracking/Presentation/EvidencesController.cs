using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.Tracking.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.Tracking.Presentation;

[ApiController]
[Authorize]
[Route("api/milestones")]
public sealed class EvidencesController(
    ICommandHandler<ScheduleMilestoneCommand, MilestoneResponse> scheduleHandler,
    IQueryHandler<GetMilestoneByIdQuery, MilestoneResponse?> getByIdHandler,
    IQueryHandler<ListMilestonesByAgreementQuery, IReadOnlyList<MilestoneResponse>> listHandler,
    ICommandHandler<StoreEvidenceCommand, EvidenceResponse?> storeEvidenceHandler,
    ICommandHandler<SubmitMilestoneCommand, bool> submitHandler,
    ICommandHandler<ReviewMilestoneCommand, bool> reviewHandler) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MilestoneResponse>> Schedule(
        ScheduleMilestoneCommand command,
        CancellationToken cancellationToken)
    {
        var milestone = await scheduleHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = milestone.Id }, milestone);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MilestoneResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var milestone = await getByIdHandler.HandleAsync(new GetMilestoneByIdQuery(id), cancellationToken);
        return milestone is null ? NotFound() : Ok(milestone);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MilestoneResponse>>> ListByAgreement(
        [FromQuery] Guid agreementId,
        CancellationToken cancellationToken) =>
        Ok(await listHandler.HandleAsync(new ListMilestonesByAgreementQuery(agreementId), cancellationToken));

    [HttpPost("{id:guid}/evidences")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<EvidenceResponse>> AddEvidence(
        Guid id,
        [FromForm] EvidenceUploadRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();
        var command = new StoreEvidenceCommand(
            id,
            request.File.FileName,
            request.File.ContentType,
            stream,
            request.File.Length,
            request.CapturedAtUtc,
            request.Note);

        var evidence = await storeEvidenceHandler.HandleAsync(command, cancellationToken);
        return evidence is null ? NotFound() : Ok(evidence);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken) =>
        await submitHandler.HandleAsync(new SubmitMilestoneCommand(id), cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Review(
        Guid id,
        ReviewMilestoneRequest request,
        CancellationToken cancellationToken) =>
        await reviewHandler.HandleAsync(
            new ReviewMilestoneCommand(id, request.Approved, request.Comment),
            cancellationToken)
                ? NoContent()
                : NotFound();
}

public sealed class EvidenceUploadRequest
{
    [Required]
    public IFormFile File { get; init; } = null!;

    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    [MaxLength(500)]
    public string? Note { get; init; }
}
