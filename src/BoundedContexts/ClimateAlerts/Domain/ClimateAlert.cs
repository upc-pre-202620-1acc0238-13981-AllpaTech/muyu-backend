using Muyu.Shared.Domain;

namespace Muyu.Modules.ClimateAlerts.Domain;

public enum ClimateHazard
{
    Frost = 0,
    ExtremeHeat = 1,
    HeavyRain = 2,
    StrongWind = 3
}

public enum AlertSeverity
{
    Advisory = 0,
    Warning = 1,
    Critical = 2
}

public enum ClimateAlertStatus
{
    Open = 0,
    Acknowledged = 1,
    Resolved = 2
}

public sealed class ClimateAlert : AggregateRoot
{
    private ClimateAlert()
    {
    }

    private ClimateAlert(
        Guid parcelId,
        ClimateHazard hazard,
        AlertSeverity severity,
        string message,
        decimal measuredValue,
        string unit,
        DateTimeOffset observedAtUtc)
    {
        if (parcelId == Guid.Empty)
        {
            throw new ArgumentException("La parcela es obligatoria.", nameof(parcelId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);

        ParcelId = parcelId;
        Hazard = hazard;
        Severity = severity;
        Message = message.Trim();
        MeasuredValue = measuredValue;
        Unit = unit.Trim();
        ObservedAtUtc = observedAtUtc;
        Status = ClimateAlertStatus.Open;
    }

    public Guid ParcelId { get; private set; }

    public ClimateHazard Hazard { get; private set; }

    public AlertSeverity Severity { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public decimal MeasuredValue { get; private set; }

    public string Unit { get; private set; } = string.Empty;

    public DateTimeOffset ObservedAtUtc { get; private set; }

    public ClimateAlertStatus Status { get; private set; }

    public DateTimeOffset? AcknowledgedAtUtc { get; private set; }

    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public static ClimateAlert Raise(
        Guid parcelId,
        ClimateHazard hazard,
        AlertSeverity severity,
        string message,
        decimal measuredValue,
        string unit,
        DateTimeOffset observedAtUtc)
    {
        var alert = new ClimateAlert(
            parcelId,
            hazard,
            severity,
            message,
            measuredValue,
            unit,
            observedAtUtc);

        alert.RaiseDomainEvent(new ClimateAlertRaisedDomainEvent(
            alert.Id,
            alert.ParcelId,
            alert.Hazard,
            alert.Severity,
            observedAtUtc));

        return alert;
    }

    public void Acknowledge(DateTimeOffset occurredAtUtc)
    {
        if (Status is not ClimateAlertStatus.Open)
        {
            throw new InvalidOperationException("Solo una alerta abierta puede ser reconocida.");
        }

        Status = ClimateAlertStatus.Acknowledged;
        AcknowledgedAtUtc = occurredAtUtc;
        MarkAsUpdated();
        RaiseDomainEvent(new ClimateAlertAcknowledgedDomainEvent(Id, occurredAtUtc));
    }

    public void Resolve(DateTimeOffset occurredAtUtc)
    {
        if (Status is ClimateAlertStatus.Resolved)
        {
            throw new InvalidOperationException("La alerta ya está resuelta.");
        }

        Status = ClimateAlertStatus.Resolved;
        ResolvedAtUtc = occurredAtUtc;
        MarkAsUpdated();
        RaiseDomainEvent(new ClimateAlertResolvedDomainEvent(Id, occurredAtUtc));
    }
}
