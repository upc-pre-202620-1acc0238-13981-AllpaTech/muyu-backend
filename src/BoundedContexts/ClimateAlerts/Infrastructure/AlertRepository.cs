using Microsoft.EntityFrameworkCore;
using Muyu.Modules.ClimateAlerts.Application;
using Muyu.Modules.ClimateAlerts.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.ClimateAlerts.Infrastructure;

// Implementación de infraestructura para IAlertRepository respaldada por Entity Framework Core,
// gestionando el ciclo de vida y la persistencia de alertas mediante MuyuDbContext.
public sealed class AlertRepository(MuyuDbContext dbContext) : IAlertRepository
{
    public Task AddRangeAsync(IEnumerable<ClimateAlert> alerts, CancellationToken cancellationToken = default) =>
        dbContext.Set<ClimateAlert>().AddRangeAsync(alerts, cancellationToken);

    public Task<ClimateAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ClimateAlert>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

// Consulta sin seguimiento (AsNoTracking) para optimizar memoria, filtrando alertas pendientes
    // de una parcela específica y ordenándolas cronológicamente de la más reciente a la más antigua.
    public async Task<IReadOnlyList<ClimateAlert>> ListOpenByParcelAsync(
        Guid parcelId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<ClimateAlert>()
            .AsNoTracking()
            .Where(item => item.ParcelId == parcelId && item.Status != ClimateAlertStatus.Resolved)
            .OrderByDescending(item => item.ObservedAtUtc)
            .ToListAsync(cancellationToken);

// Confirma de forma asíncrona todas las mutaciones pendientes en el contexto de la base de datos dentro de la unidad de trabajo actual.
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ = await dbContext.SaveChangesAsync(cancellationToken);
}
