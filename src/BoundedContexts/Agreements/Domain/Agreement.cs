using Muyu.Shared.Domain;

namespace Muyu.Modules.Agreements.Domain;

public enum AgreementStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed class Agreement : AggregateRoot
{
    private readonly List<DisbursementPlan> _disbursementPlans = [];

    private Agreement()
    {
    }

    private Agreement(
        Guid parcelId,
        Guid producerId,
        decimal principalAmount,
        string currency,
        DateOnly startsOn,
        DateOnly endsOn,
        IEnumerable<DisbursementPlan> plans)
    {
        if (parcelId == Guid.Empty || producerId == Guid.Empty)
        {
            throw new ArgumentException("La parcela y el productor son obligatorios.");
        }

        if (principalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(principalAmount));
        }

        if (endsOn <= startsOn)
        {
            throw new ArgumentException("La fecha final debe ser posterior a la inicial.");
        }

        ParcelId = parcelId;
        ProducerId = producerId;
        PrincipalAmount = decimal.Round(principalAmount, 2);
        Currency = NormalizeCurrency(currency);
        StartsOn = startsOn;
        EndsOn = endsOn;
        Status = AgreementStatus.Draft;

        _disbursementPlans.AddRange(plans);
        ValidatePlans();
    }

    public Guid ParcelId { get; private set; }

    public Guid ProducerId { get; private set; }

    public decimal PrincipalAmount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public DateOnly StartsOn { get; private set; }

    public DateOnly EndsOn { get; private set; }

    public AgreementStatus Status { get; private set; }

    public IReadOnlyCollection<DisbursementPlan> DisbursementPlans => _disbursementPlans.AsReadOnly();

    public static Agreement Create(
        Guid parcelId,
        Guid producerId,
        decimal principalAmount,
        string currency,
        DateOnly startsOn,
        DateOnly endsOn,
        IEnumerable<(decimal Amount, DateOnly ScheduledDate)> schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var plans = schedule
            .Select((item, index) => new DisbursementPlan(index + 1, item.Amount, item.ScheduledDate))
            .ToArray();

        var agreement = new Agreement(
            parcelId,
            producerId,
            principalAmount,
            currency,
            startsOn,
            endsOn,
            plans);

        agreement.RaiseDomainEvent(new AgreementCreatedDomainEvent(
            agreement.Id,
            agreement.ParcelId,
            agreement.PrincipalAmount,
            agreement.Currency,
            DateTimeOffset.UtcNow));

        return agreement;
    }

    public void Activate()
    {
        if (Status is not AgreementStatus.Draft)
        {
            throw new InvalidOperationException("Solo un acuerdo en borrador puede activarse.");
        }

        Status = AgreementStatus.Active;
        MarkAsUpdated();
        RaiseDomainEvent(new AgreementActivatedDomainEvent(Id, DateTimeOffset.UtcNow));
    }

    public void ReleaseDisbursement(int sequence, DateTimeOffset occurredAtUtc)
    {
        if (Status is not AgreementStatus.Active)
        {
            throw new InvalidOperationException("El acuerdo debe estar activo.");
        }

        var plan = _disbursementPlans.SingleOrDefault(item => item.Sequence == sequence)
            ?? throw new InvalidOperationException("El desembolso no existe.");

        var previousPending = _disbursementPlans.Any(item =>
            item.Sequence < sequence && item.Status is DisbursementPlanStatus.Pending);

        if (previousPending)
        {
            throw new InvalidOperationException("Los desembolsos deben liberarse en orden.");
        }

        plan.MarkAsReleased(occurredAtUtc);
        RaiseDomainEvent(new DisbursementReleasedDomainEvent(
            Id,
            plan.Sequence,
            plan.Amount,
            occurredAtUtc));

        if (_disbursementPlans.All(item => item.Status is DisbursementPlanStatus.Released))
        {
            Status = AgreementStatus.Completed;
        }

        MarkAsUpdated();
    }

    private void ValidatePlans()
    {
        if (_disbursementPlans.Count == 0)
        {
            throw new InvalidOperationException("El acuerdo requiere al menos un desembolso.");
        }

        if (_disbursementPlans.Any(plan => plan.ScheduledDate < StartsOn || plan.ScheduledDate > EndsOn))
        {
            throw new InvalidOperationException("Todos los desembolsos deben estar dentro de la vigencia.");
        }

        if (_disbursementPlans.Sum(plan => plan.Amount) != PrincipalAmount)
        {
            throw new InvalidOperationException("La suma de desembolsos debe ser igual al monto principal.");
        }
    }

    private static string NormalizeCurrency(string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var normalized = currency.Trim().ToUpperInvariant();

        if (normalized.Length != 3)
        {
            throw new ArgumentException("La moneda debe usar el código ISO de tres letras.", nameof(currency));
        }

        return normalized;
    }
}
