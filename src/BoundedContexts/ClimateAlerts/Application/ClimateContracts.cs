using System.ComponentModel.DataAnnotations;
using Muyu.Modules.ClimateAlerts.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.ClimateAlerts.Application;

// Define la solicitud de monitoreo con validación de rango geográfico (lat/lon)
// y umbrales agronómicos predeterminados para disparar alertas climáticas.
public sealed record MonitorParcelCommand(
    Guid ParcelId,
    [Range(-90, 90)] double Latitude,
    [Range(-180, 180)] double Longitude,
    decimal FrostThresholdCelsius = 2,
    decimal HeatThresholdCelsius = 38,
    decimal HeavyRainThresholdMillimeters = 25,
    decimal StrongWindThresholdKilometersPerHour = 60) : ICommand<MonitoringResult>;

// Define las operaciones CQRS para consultar alertas abiertas y mutar
// el estado del ciclo de vida de una alerta (reconocimiento o resolución).
public sealed record ListOpenClimateAlertsQuery(
    Guid ParcelId) : IQuery<IReadOnlyList<ClimateAlertResponse>>;

public sealed record AcknowledgeClimateAlertCommand(Guid AlertId) : ICommand<bool>;

public sealed record ResolveClimateAlertCommand(Guid AlertId) : ICommand<bool>;

// Encapsula las lecturas meteorológicas observadas y las alertas derivadas,
// sirviendo como contrato inmutable de respuesta hacia capas externas.
public sealed record WeatherSnapshot(
    decimal TemperatureCelsius,
    decimal RainLastHourMillimeters,
    decimal WindKilometersPerHour,
    int HumidityPercent,
    DateTimeOffset ObservedAtUtc);

public sealed record ClimateAlertResponse(
    Guid Id,
    Guid ParcelId,
    ClimateHazard Hazard,
    AlertSeverity Severity,
    string Message,
    decimal MeasuredValue,
    string Unit,
    DateTimeOffset ObservedAtUtc,
    ClimateAlertStatus Status);

public sealed record MonitoringResult(
    WeatherSnapshot Snapshot,
    IReadOnlyList<ClimateAlertResponse> RaisedAlerts);
