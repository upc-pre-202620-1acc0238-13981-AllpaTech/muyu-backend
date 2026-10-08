using Muyu.Shared.Domain;

namespace Muyu.Modules.ClimateAlerts.Domain;

public sealed record ClimateAlertRaisedDomainEvent(
    Guid AlertId,
    Guid ParcelId,
    ClimateHazard Hazard,
    AlertSeverity Severity,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ClimateAlertAcknowledgedDomainEvent(
    Guid AlertId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ClimateAlertResolvedDomainEvent(
    Guid AlertId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
