using Muyu.Modules.ClimateAlerts.Domain;

namespace Muyu.Modules.ClimateAlerts.Application;

public interface IAlertRepository
{
    Task AddRangeAsync(IEnumerable<ClimateAlert> alerts, CancellationToken cancellationToken = default);

    Task<ClimateAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClimateAlert>> ListOpenByParcelAsync(Guid parcelId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IWeatherProvider
{
    Task<WeatherSnapshot> GetCurrentAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
