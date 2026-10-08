using Muyu.Shared.Domain;

namespace Muyu.Modules.Escrow.Domain;

public enum PayoutStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}

public sealed class PayoutRecord : BaseEntity
{
    private PayoutRecord()
    {
    }

    internal PayoutRecord(Guid recipientId, decimal amount, string purpose)
    {
        if (recipientId == Guid.Empty)
        {
            throw new ArgumentException("El beneficiario es obligatorio.", nameof(recipientId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        RecipientId = recipientId;
        Amount = decimal.Round(amount, 2);
        Purpose = purpose.Trim();
        Status = PayoutStatus.Pending;
    }

    public Guid EscrowAccountId { get; private set; }

    public Guid RecipientId { get; private set; }

    public decimal Amount { get; private set; }

    public string Purpose { get; private set; } = string.Empty;

    public PayoutStatus Status { get; private set; }

    public string? GatewayReference { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    internal void Complete(string gatewayReference, DateTimeOffset processedAtUtc)
    {
        if (Status is not PayoutStatus.Pending)
        {
            throw new InvalidOperationException("El pago ya fue procesado.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayReference);
        GatewayReference = gatewayReference.Trim();
        ProcessedAtUtc = processedAtUtc;
        Status = PayoutStatus.Completed;
        MarkAsUpdated();
    }

    internal void Fail(DateTimeOffset processedAtUtc)
    {
        if (Status is not PayoutStatus.Pending)
        {
            throw new InvalidOperationException("El pago ya fue procesado.");
        }

        Status = PayoutStatus.Failed;
        ProcessedAtUtc = processedAtUtc;
        MarkAsUpdated();
    }
}
