using Muyu.Shared.Domain;

namespace Muyu.Modules.Escrow.Domain;

public sealed record EscrowOpenedDomainEvent(
    Guid EscrowAccountId,
    Guid AgreementId,
    string Currency,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record EscrowFundedDomainEvent(
    Guid EscrowAccountId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record PayoutCompletedDomainEvent(
    Guid EscrowAccountId,
    Guid PayoutId,
    decimal Amount,
    string GatewayReference,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
