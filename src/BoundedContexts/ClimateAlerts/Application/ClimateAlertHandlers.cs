using Muyu.Modules.ClimateAlerts.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.ClimateAlerts.Application;

public sealed class MonitorParcelCommandHandler(
    IAlertRepository repository,
    IWeatherProvider weatherProvider) : ICommandHandler<MonitorParcelCommand, MonitoringResult>
{
    public async Task<MonitoringResult> HandleAsync(
        MonitorParcelCommand command,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await weatherProvider.GetCurrentAsync(
            command.Latitude,
            command.Longitude,
            cancellationToken);

        var alerts = ClimateAlertPolicy.Evaluate(command, snapshot).ToArray();
        if (alerts.Length > 0)
        {
            await repository.AddRangeAsync(alerts, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new MonitoringResult(
            snapshot,
            alerts.Select(ClimateAlertMapping.ToResponse).ToArray());
    }
}

public sealed class ListOpenClimateAlertsQueryHandler(IAlertRepository repository)
    : IQueryHandler<ListOpenClimateAlertsQuery, IReadOnlyList<ClimateAlertResponse>>
{
    public async Task<IReadOnlyList<ClimateAlertResponse>> HandleAsync(
        ListOpenClimateAlertsQuery query,
        CancellationToken cancellationToken = default) =>
        (await repository.ListOpenByParcelAsync(query.ParcelId, cancellationToken))
            .Select(ClimateAlertMapping.ToResponse)
            .ToArray();
}

public sealed class AcknowledgeClimateAlertCommandHandler(IAlertRepository repository)
    : ICommandHandler<AcknowledgeClimateAlertCommand, bool>
{
    public async Task<bool> HandleAsync(
        AcknowledgeClimateAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        var alert = await repository.GetByIdAsync(command.AlertId, cancellationToken);
        if (alert is null)
        {
            return false;
        }

        alert.Acknowledge(DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ResolveClimateAlertCommandHandler(IAlertRepository repository)
    : ICommandHandler<ResolveClimateAlertCommand, bool>
{
    public async Task<bool> HandleAsync(
        ResolveClimateAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        var alert = await repository.GetByIdAsync(command.AlertId, cancellationToken);
        if (alert is null)
        {
            return false;
        }

        alert.Resolve(DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class ClimateAlertPolicy
{
    internal static IEnumerable<ClimateAlert> Evaluate(
        MonitorParcelCommand command,
        WeatherSnapshot snapshot)
    {
        if (snapshot.TemperatureCelsius <= command.FrostThresholdCelsius)
        {
            yield return ClimateAlert.Raise(
                command.ParcelId,
                ClimateHazard.Frost,
                AlertSeverity.Critical,
                "Riesgo de helada detectado.",
                snapshot.TemperatureCelsius,
                "°C",
                snapshot.ObservedAtUtc);
        }

        if (snapshot.TemperatureCelsius >= command.HeatThresholdCelsius)
        {
            yield return ClimateAlert.Raise(
                command.ParcelId,
                ClimateHazard.ExtremeHeat,
                AlertSeverity.Warning,
                "Temperatura extrema detectada.",
                snapshot.TemperatureCelsius,
                "°C",
                snapshot.ObservedAtUtc);
        }

        if (snapshot.RainLastHourMillimeters >= command.HeavyRainThresholdMillimeters)
        {
            yield return ClimateAlert.Raise(
                command.ParcelId,
                ClimateHazard.HeavyRain,
                AlertSeverity.Warning,
                "Precipitación intensa detectada.",
                snapshot.RainLastHourMillimeters,
                "mm/h",
                snapshot.ObservedAtUtc);
        }

        if (snapshot.WindKilometersPerHour >= command.StrongWindThresholdKilometersPerHour)
        {
            yield return ClimateAlert.Raise(
                command.ParcelId,
                ClimateHazard.StrongWind,
                AlertSeverity.Warning,
                "Viento fuerte detectado.",
                snapshot.WindKilometersPerHour,
                "km/h",
                snapshot.ObservedAtUtc);
        }
    }
}

internal static class ClimateAlertMapping
{
    internal static ClimateAlertResponse ToResponse(ClimateAlert alert) =>
        new(
            alert.Id,
            alert.ParcelId,
            alert.Hazard,
            alert.Severity,
            alert.Message,
            alert.MeasuredValue,
            alert.Unit,
            alert.ObservedAtUtc,
            alert.Status);
}
