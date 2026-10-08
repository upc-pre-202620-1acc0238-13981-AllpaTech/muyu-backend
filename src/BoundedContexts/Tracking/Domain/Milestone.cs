using Muyu.Shared.Domain;

namespace Muyu.Modules.Tracking.Domain;

public enum MilestoneStatus
{
    Planned = 0,
    InProgress = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4
}

public sealed class Milestone : AggregateRoot
{
    private readonly List<Evidence> _evidences = [];

    private Milestone()
    {
    }

    private Milestone(Guid agreementId, Guid parcelId, string title, DateOnly dueDate)
    {
        if (agreementId == Guid.Empty || parcelId == Guid.Empty)
        {
            throw new ArgumentException("El acuerdo y la parcela son obligatorios.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        AgreementId = agreementId;
        ParcelId = parcelId;
        Title = title.Trim();
        DueDate = dueDate;
        Status = MilestoneStatus.Planned;
    }

    public Guid AgreementId { get; private set; }

    public Guid ParcelId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateOnly DueDate { get; private set; }

    public MilestoneStatus Status { get; private set; }

    public string? ReviewComment { get; private set; }

    public IReadOnlyCollection<Evidence> Evidences => _evidences.AsReadOnly();

    public static Milestone Schedule(Guid agreementId, Guid parcelId, string title, DateOnly dueDate)
    {
        var milestone = new Milestone(agreementId, parcelId, title, dueDate);
        milestone.RaiseDomainEvent(new MilestoneScheduledDomainEvent(
            milestone.Id,
            milestone.AgreementId,
            milestone.ParcelId,
            DateTimeOffset.UtcNow));
        return milestone;
    }

    public Evidence AddEvidence(
        string fileName,
        string contentType,
        string storageUri,
        string sha256,
        long sizeBytes,
        DateTimeOffset capturedAtUtc,
        string? note)
    {
        if (Status is MilestoneStatus.Approved)
        {
            throw new InvalidOperationException("No se puede agregar evidencia a un hito aprobado.");
        }

        if (_evidences.Any(item => item.Sha256 == sha256.ToLowerInvariant()))
        {
            throw new InvalidOperationException("La misma evidencia ya fue registrada.");
        }

        var evidence = new Evidence(
            fileName,
            contentType,
            storageUri,
            sha256,
            sizeBytes,
            capturedAtUtc,
            note);

        _evidences.Add(evidence);
        Status = MilestoneStatus.InProgress;
        MarkAsUpdated();
        RaiseDomainEvent(new EvidenceAddedDomainEvent(
            Id,
            evidence.Id,
            evidence.Sha256,
            DateTimeOffset.UtcNow));
        return evidence;
    }

    public void Submit()
    {
        if (_evidences.Count == 0)
        {
            throw new InvalidOperationException("El hito requiere al menos una evidencia.");
        }

        if (Status is MilestoneStatus.Approved)
        {
            throw new InvalidOperationException("El hito ya fue aprobado.");
        }

        Status = MilestoneStatus.Submitted;
        ReviewComment = null;
        MarkAsUpdated();
        RaiseDomainEvent(new MilestoneSubmittedDomainEvent(Id, DateTimeOffset.UtcNow));
    }

    public void Review(bool approved, string? comment)
    {
        if (Status is not MilestoneStatus.Submitted)
        {
            throw new InvalidOperationException("Solo un hito enviado puede revisarse.");
        }

        if (!approved && string.IsNullOrWhiteSpace(comment))
        {
            throw new InvalidOperationException("El rechazo requiere un comentario.");
        }

        Status = approved ? MilestoneStatus.Approved : MilestoneStatus.Rejected;
        ReviewComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        MarkAsUpdated();
        RaiseDomainEvent(new MilestoneReviewedDomainEvent(Id, approved, DateTimeOffset.UtcNow));
    }
}
