using Muyu.Modules.ClimateAlerts.Domain;

namespace Muyu.Modules.ClimateAlerts.Application;

// Define el contrato de persistencia para el agregado de alertas climáticas,
// soportando inserciones por lote, consultas por parcela y control de transacciones.
public interface IAlertRepository
{
    Task AddRangeAsync(IEnumerable<ClimateAlert> alerts, CancellationToken cancellationToken = default);

    Task<ClimateAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
// Recupera las alertas no resueltas asociadas a una parcela para evitar duplicados y facilitar el seguimiento operativo.
    Task<IReadOnlyList<ClimateAlert>> ListOpenByParcelAsync(Guid parcelId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Puerto de infraestructura para consultar condiciones meteorológicas en tiempo real
// mediante coordenadas geográficas, desacoplando la API climática externa del dominio.
public interface IWeatherProvider
{
    Task<WeatherSnapshot> GetCurrentAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
