using System.ComponentModel.DataAnnotations;
using Muyu.Modules.Escrow.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.Escrow.Application;

public sealed record OpenEscrowCommand(
    Guid AgreementId,
    [Required, StringLength(3, MinimumLength = 3)] string Currency)
    : ICommand<EscrowAccountResponse>;

public sealed record GetEscrowByIdQuery(Guid EscrowAccountId) : IQuery<EscrowAccountResponse?>;

public sealed record FundEscrowRequest([Range(0.01, double.MaxValue)] decimal Amount);

public sealed record FundEscrowCommand(Guid EscrowAccountId, decimal Amount) : ICommand<bool>;

public sealed record ReleasePayoutRequest(
    Guid RecipientId,
    [Range(0.01, double.MaxValue)] decimal Amount,
    [Required, MaxLength(250)] string Purpose);

public sealed record ReleasePayoutCommand(
    Guid EscrowAccountId,
    Guid RecipientId,
    decimal Amount,
    string Purpose) : ICommand<PayoutResponse?>;

public sealed record PayoutResponse(
    Guid Id,
    Guid RecipientId,
    decimal Amount,
    string Purpose,
    PayoutStatus Status,
    string? GatewayReference,
    DateTimeOffset? ProcessedAtUtc);

public sealed record EscrowAccountResponse(
    Guid Id,
    Guid AgreementId,
    string Currency,
    decimal FundedAmount,
    decimal ReleasedAmount,
    decimal AvailableBalance,
    EscrowAccountStatus Status,
    IReadOnlyList<PayoutResponse> Payouts);
