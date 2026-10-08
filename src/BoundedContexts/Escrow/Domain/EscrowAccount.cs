using Muyu.Shared.Domain;

namespace Muyu.Modules.Escrow.Domain;

public enum EscrowAccountStatus
{
    Open = 0,
    Closed = 1
}

public sealed class EscrowAccount : AggregateRoot
{
    private readonly List<PayoutRecord> _payouts = [];

    private EscrowAccount()
    {
    }

    private EscrowAccount(Guid agreementId, string currency)
    {
        if (agreementId == Guid.Empty)
        {
            throw new ArgumentException("El acuerdo es obligatorio.", nameof(agreementId));
        }

        AgreementId = agreementId;
        Currency = NormalizeCurrency(currency);
        Status = EscrowAccountStatus.Open;
    }

    public Guid AgreementId { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal FundedAmount { get; private set; }

    public decimal ReleasedAmount { get; private set; }

    public decimal ReservedAmount => _payouts
        .Where(item => item.Status is PayoutStatus.Pending)
        .Sum(item => item.Amount);

    public decimal AvailableBalance => FundedAmount - ReleasedAmount - ReservedAmount;

    public EscrowAccountStatus Status { get; private set; }

    public IReadOnlyCollection<PayoutRecord> Payouts => _payouts.AsReadOnly();

    public static EscrowAccount Open(Guid agreementId, string currency)
    {
        var account = new EscrowAccount(agreementId, currency);
        account.RaiseDomainEvent(new EscrowOpenedDomainEvent(
            account.Id,
            account.AgreementId,
            account.Currency,
            DateTimeOffset.UtcNow));
        return account;
    }

    public void Fund(decimal amount)
    {
        EnsureOpen();
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        FundedAmount += decimal.Round(amount, 2);
        MarkAsUpdated();
        RaiseDomainEvent(new EscrowFundedDomainEvent(Id, decimal.Round(amount, 2), DateTimeOffset.UtcNow));
    }

    public PayoutRecord RequestPayout(Guid recipientId, decimal amount, string purpose)
    {
        EnsureOpen();
        var payout = new PayoutRecord(recipientId, amount, purpose);

        if (payout.Amount > AvailableBalance)
        {
            throw new InvalidOperationException("La cuenta de custodia no tiene saldo suficiente.");
        }

        _payouts.Add(payout);
        MarkAsUpdated();
        return payout;
    }

    public void CompletePayout(Guid payoutId, string gatewayReference, DateTimeOffset processedAtUtc)
    {
        var payout = _payouts.SingleOrDefault(item => item.Id == payoutId)
            ?? throw new InvalidOperationException("El pago no pertenece a esta cuenta.");

        payout.Complete(gatewayReference, processedAtUtc);
        ReleasedAmount += payout.Amount;
        MarkAsUpdated();
        RaiseDomainEvent(new PayoutCompletedDomainEvent(
            Id,
            payout.Id,
            payout.Amount,
            gatewayReference,
            processedAtUtc));
    }

    public void FailPayout(Guid payoutId, DateTimeOffset processedAtUtc)
    {
        var payout = _payouts.SingleOrDefault(item => item.Id == payoutId)
            ?? throw new InvalidOperationException("El pago no pertenece a esta cuenta.");

        payout.Fail(processedAtUtc);
        MarkAsUpdated();
    }

    public void Close()
    {
        if (AvailableBalance != 0 || _payouts.Any(item => item.Status is PayoutStatus.Pending))
        {
            throw new InvalidOperationException("La cuenta solo puede cerrarse sin saldo ni pagos pendientes.");
        }

        Status = EscrowAccountStatus.Closed;
        MarkAsUpdated();
    }

    private void EnsureOpen()
    {
        if (Status is not EscrowAccountStatus.Open)
        {
            throw new InvalidOperationException("La cuenta de custodia está cerrada.");
        }
    }

    private static string NormalizeCurrency(string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var normalized = currency.Trim().ToUpperInvariant();
        return normalized.Length == 3
            ? normalized
            : throw new ArgumentException("La moneda debe usar un código ISO de tres letras.", nameof(currency));
    }
}
