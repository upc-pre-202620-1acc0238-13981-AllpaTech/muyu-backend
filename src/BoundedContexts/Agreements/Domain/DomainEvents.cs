using Muyu.Shared.Domain;

namespace Muyu.Modules.Agreements.Domain;

public sealed record AgreementCreatedDomainEvent(
    Guid AgreementId,
    Guid ParcelId,
    decimal PrincipalAmount,
    string Currency,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record AgreementActivatedDomainEvent(
    Guid AgreementId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record DisbursementReleasedDomainEvent(
    Guid AgreementId,
    int Sequence,
    decimal Amount,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
