using Muyu.Modules.Tracking.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.Tracking.Application;

public sealed class ScheduleMilestoneCommandHandler(IMilestoneRepository repository)
    : ICommandHandler<ScheduleMilestoneCommand, MilestoneResponse>
{
    public async Task<MilestoneResponse> HandleAsync(
        ScheduleMilestoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var milestone = Milestone.Schedule(
            command.AgreementId,
            command.ParcelId,
            command.Title,
            command.DueDate);

        await repository.AddAsync(milestone, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return TrackingMapping.ToResponse(milestone);
    }
}

public sealed class GetMilestoneByIdQueryHandler(IMilestoneRepository repository)
    : IQueryHandler<GetMilestoneByIdQuery, MilestoneResponse?>
{
    public async Task<MilestoneResponse?> HandleAsync(
        GetMilestoneByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var milestone = await repository.GetByIdReadOnlyAsync(query.MilestoneId, cancellationToken);
        return milestone is null ? null : TrackingMapping.ToResponse(milestone);
    }
}

public sealed class ListMilestonesByAgreementQueryHandler(IMilestoneRepository repository)
    : IQueryHandler<ListMilestonesByAgreementQuery, IReadOnlyList<MilestoneResponse>>
{
    public async Task<IReadOnlyList<MilestoneResponse>> HandleAsync(
        ListMilestonesByAgreementQuery query,
        CancellationToken cancellationToken = default) =>
        (await repository.ListByAgreementAsync(query.AgreementId, cancellationToken))
            .Select(TrackingMapping.ToResponse)
            .ToArray();
}

public sealed class StoreEvidenceCommandHandler(
    IMilestoneRepository repository,
    ICloudStorage cloudStorage) : ICommandHandler<StoreEvidenceCommand, EvidenceResponse?>
{
    public async Task<EvidenceResponse?> HandleAsync(
        StoreEvidenceCommand command,
        CancellationToken cancellationToken = default)
    {
        var milestone = await repository.GetByIdAsync(command.MilestoneId, cancellationToken);
        if (milestone is null)
        {
            return null;
        }

        var storedFile = await cloudStorage.StoreAsync(
            command.Content,
            command.FileName,
            command.ContentType,
            cancellationToken);

        var evidence = milestone.AddEvidence(
            command.FileName,
            command.ContentType,
            storedFile.Uri,
            storedFile.Sha256,
            storedFile.SizeBytes,
            command.CapturedAtUtc,
            command.Note);

        await repository.SaveChangesAsync(cancellationToken);
        return TrackingMapping.ToResponse(evidence);
    }
}

public sealed class SubmitMilestoneCommandHandler(IMilestoneRepository repository)
    : ICommandHandler<SubmitMilestoneCommand, bool>
{
    public async Task<bool> HandleAsync(
        SubmitMilestoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var milestone = await repository.GetByIdAsync(command.MilestoneId, cancellationToken);
        if (milestone is null)
        {
            return false;
        }

        milestone.Submit();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ReviewMilestoneCommandHandler(IMilestoneRepository repository)
    : ICommandHandler<ReviewMilestoneCommand, bool>
{
    public async Task<bool> HandleAsync(
        ReviewMilestoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var milestone = await repository.GetByIdAsync(command.MilestoneId, cancellationToken);
        if (milestone is null)
        {
            return false;
        }

        milestone.Review(command.Approved, command.Comment);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class TrackingMapping
{
    internal static MilestoneResponse ToResponse(Milestone milestone) =>
        new(
            milestone.Id,
            milestone.AgreementId,
            milestone.ParcelId,
            milestone.Title,
            milestone.DueDate,
            milestone.Status,
            milestone.ReviewComment,
            milestone.Evidences
                .OrderBy(item => item.CapturedAtUtc)
                .Select(ToResponse)
                .ToArray());

    internal static EvidenceResponse ToResponse(Evidence evidence) =>
        new(
            evidence.Id,
            evidence.FileName,
            evidence.ContentType,
            evidence.StorageUri,
            evidence.Sha256,
            evidence.SizeBytes,
            evidence.CapturedAtUtc,
            evidence.Note);
}
