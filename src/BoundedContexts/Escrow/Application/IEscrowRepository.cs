using Muyu.Modules.Escrow.Domain;

namespace Muyu.Modules.Escrow.Application;

public interface IEscrowRepository
{
    Task AddAsync(EscrowAccount account, CancellationToken cancellationToken = default);

    Task<EscrowAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EscrowAccount?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPaymentGateway
{
    Task<PaymentResult> TransferAsync(
        Guid recipientId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentResult(bool Succeeded, string? Reference, string? FailureReason);
