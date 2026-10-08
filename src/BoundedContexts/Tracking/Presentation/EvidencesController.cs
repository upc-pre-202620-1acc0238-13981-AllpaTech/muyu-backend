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
[Route("api/v1/milestones")]
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
    [Authorize(Roles = "AGRICULTOR,ADMIN")]
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
            request.Latitude,
            request.Longitude,
            request.Note);

        var evidence = await storeEvidenceHandler.HandleAsync(command, cancellationToken);
        return evidence is null ? NotFound() : Ok(evidence);
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "AGRICULTOR,ADMIN")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken) =>
        await submitHandler.HandleAsync(new SubmitMilestoneCommand(id), cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "COMERCIANTE,ADMIN")]
    public async Task<IActionResult> Review(
        Guid id,
        ReviewMilestoneRequest request,
        CancellationToken cancellationToken) =>
        await reviewHandler.HandleAsync(
            new ReviewMilestoneCommand(id, request.Approved, request.Comment),
            cancellationToken)
                ? NoContent()
                : NotFound();

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "COMERCIANTE,ADMIN")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        await reviewHandler.HandleAsync(
            new ReviewMilestoneCommand(id, true, null),
            cancellationToken)
                ? NoContent()
                : NotFound();

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "COMERCIANTE,ADMIN")]
    public async Task<IActionResult> Reject(
        Guid id,
        RejectMilestoneRequest request,
        CancellationToken cancellationToken) =>
        await reviewHandler.HandleAsync(
            new ReviewMilestoneCommand(id, false, request.Comment),
            cancellationToken)
                ? NoContent()
                : NotFound();
}

public sealed class EvidenceUploadRequest
{
    [Required]
    public IFormFile File { get; init; } = null!;

    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }

    [MaxLength(500)]
    public string? Note { get; init; }
}

public sealed record RejectMilestoneRequest([Required, MaxLength(500)] string Comment);
