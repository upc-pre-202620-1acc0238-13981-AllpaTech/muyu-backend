using Muyu.Shared.Domain;

namespace Muyu.Modules.IAM.Domain;

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    string Email,
    IdentityDocumentType DocumentType,
    string DocumentNumber,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserAuthenticatedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserDeactivatedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
