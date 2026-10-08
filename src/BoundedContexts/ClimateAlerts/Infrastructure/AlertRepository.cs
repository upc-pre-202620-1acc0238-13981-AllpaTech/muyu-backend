using Microsoft.EntityFrameworkCore;
using Muyu.Modules.ClimateAlerts.Application;
using Muyu.Modules.ClimateAlerts.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.ClimateAlerts.Infrastructure;

public sealed class AlertRepository(MuyuDbContext dbContext) : IAlertRepository
{
    public Task AddRangeAsync(IEnumerable<ClimateAlert> alerts, CancellationToken cancellationToken = default) =>
        dbContext.Set<ClimateAlert>().AddRangeAsync(alerts, cancellationToken);

    public Task<ClimateAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ClimateAlert>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ClimateAlert>> ListOpenByParcelAsync(
        Guid parcelId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<ClimateAlert>()
            .AsNoTracking()
            .Where(item => item.ParcelId == parcelId && item.Status != ClimateAlertStatus.Resolved)
            .OrderByDescending(item => item.ObservedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ = await dbContext.SaveChangesAsync(cancellationToken);
}
