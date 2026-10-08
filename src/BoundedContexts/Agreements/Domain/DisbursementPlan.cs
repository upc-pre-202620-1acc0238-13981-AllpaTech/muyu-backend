using Muyu.Shared.Domain;

namespace Muyu.Modules.Agreements.Domain;

public enum DisbursementPlanStatus
{
    Pending = 0,
    Released = 1,
    Cancelled = 2
}

public sealed class DisbursementPlan : BaseEntity
{
    private DisbursementPlan()
    {
    }

    internal DisbursementPlan(int sequence, decimal amount, DateOnly scheduledDate)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "El desembolso debe ser mayor que cero.");
        }

        Sequence = sequence;
        Amount = decimal.Round(amount, 2);
        ScheduledDate = scheduledDate;
        Status = DisbursementPlanStatus.Pending;
    }

    public Guid AgreementId { get; private set; }

    public int Sequence { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly ScheduledDate { get; private set; }

    public DisbursementPlanStatus Status { get; private set; }

    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    internal void MarkAsReleased(DateTimeOffset occurredAtUtc)
    {
        if (Status is not DisbursementPlanStatus.Pending)
        {
            throw new InvalidOperationException("El desembolso ya fue procesado.");
        }

        Status = DisbursementPlanStatus.Released;
        ReleasedAtUtc = occurredAtUtc;
        MarkAsUpdated();
    }
}
