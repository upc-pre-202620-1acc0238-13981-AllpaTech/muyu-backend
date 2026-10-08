using Muyu.Shared.Domain;

namespace Muyu.Modules.IAM.Domain;

public sealed record UserCreatedDomainEvent(
    Guid UserId,
    string Email,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserDeactivatedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
