using Muyu.Modules.Escrow.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.Escrow.Application;

public sealed class OpenEscrowCommandHandler(IEscrowRepository repository)
    : ICommandHandler<OpenEscrowCommand, EscrowAccountResponse>
{
    public async Task<EscrowAccountResponse> HandleAsync(
        OpenEscrowCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsForAgreementAsync(command.AgreementId, cancellationToken))
        {
            throw new InvalidOperationException("El acuerdo ya tiene una cuenta de custodia.");
        }

        var account = EscrowAccount.Open(command.AgreementId, command.Currency);
        await repository.AddAsync(account, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return EscrowMapping.Map(account);
    }
}

public sealed class GetEscrowByIdQueryHandler(IEscrowRepository repository)
    : IQueryHandler<GetEscrowByIdQuery, EscrowAccountResponse?>
{
    public async Task<EscrowAccountResponse?> HandleAsync(
        GetEscrowByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var account = await repository.GetByIdReadOnlyAsync(query.EscrowAccountId, cancellationToken);
        return account is null ? null : EscrowMapping.Map(account);
    }
}

public sealed class FundEscrowCommandHandler(IEscrowRepository repository)
    : ICommandHandler<FundEscrowCommand, bool>
{
    public async Task<bool> HandleAsync(
        FundEscrowCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = await repository.GetByIdAsync(command.EscrowAccountId, cancellationToken);
        if (account is null)
        {
            return false;
        }

        account.Fund(command.Amount);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ReleasePayoutCommandHandler(
    IEscrowRepository repository,
    IPaymentGateway paymentGateway) : ICommandHandler<ReleasePayoutCommand, PayoutResponse?>
{
    public async Task<PayoutResponse?> HandleAsync(
        ReleasePayoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = await repository.GetByIdAsync(command.EscrowAccountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var payout = account.RequestPayout(command.RecipientId, command.Amount, command.Purpose);
        await repository.SaveChangesAsync(cancellationToken);

        var result = await paymentGateway.TransferAsync(
            payout.RecipientId,
            payout.Amount,
            account.Currency,
            payout.Id.ToString("N"),
            cancellationToken);

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Reference))
        {
            account.FailPayout(payout.Id, DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(result.FailureReason ?? "La pasarela rechazó el pago.");
        }

        account.CompletePayout(payout.Id, result.Reference, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return EscrowMapping.Map(payout);
    }
}

internal static class EscrowMapping
{
    internal static EscrowAccountResponse Map(EscrowAccount account) =>
        new(
            account.Id,
            account.AgreementId,
            account.Currency,
            account.FundedAmount,
            account.ReleasedAmount,
            account.AvailableBalance,
            account.Status,
            account.Payouts.OrderBy(item => item.CreatedAtUtc).Select(Map).ToArray());

    internal static PayoutResponse Map(PayoutRecord payout) =>
        new(
            payout.Id,
            payout.RecipientId,
            payout.Amount,
            payout.Purpose,
            payout.Status,
            payout.GatewayReference,
            payout.ProcessedAtUtc);
}
