using Muyu.Shared.Domain;

namespace Muyu.Modules.Tracking.Domain;

public sealed record MilestoneScheduledDomainEvent(
    Guid MilestoneId,
    Guid AgreementId,
    Guid ParcelId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record EvidenceAddedDomainEvent(
    Guid MilestoneId,
    Guid EvidenceId,
    string Sha256,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record MilestoneSubmittedDomainEvent(
    Guid MilestoneId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record MilestoneReviewedDomainEvent(
    Guid MilestoneId,
    bool Approved,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
