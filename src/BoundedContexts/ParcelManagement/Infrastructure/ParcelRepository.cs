using Microsoft.EntityFrameworkCore;
using Muyu.Modules.ParcelManagement.Application;
using Muyu.Modules.ParcelManagement.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.ParcelManagement.Infrastructure;

public sealed class ParcelRepository(MuyuDbContext dbContext) : IParcelRepository
{
    public Task AddAsync(Parcel parcel, CancellationToken cancellationToken = default) =>
        dbContext.Set<Parcel>().AddAsync(parcel, cancellationToken).AsTask();

    public Task<Parcel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Parcel>().SingleOrDefaultAsync(parcel => parcel.Id == id, cancellationToken);

    public Task<Parcel?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Parcel>()
            .AsNoTracking()
            .SingleOrDefaultAsync(parcel => parcel.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Parcel>> ListByProducerAsync(
        Guid producerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<Parcel>()
            .AsNoTracking()
            .Where(parcel => parcel.ProducerId == producerId)
            .OrderBy(parcel => parcel.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Parcel>> ListAvailableAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<Parcel>()
            .AsNoTracking()
            .Where(parcel => parcel.Status == ParcelStatus.Active)
            .OrderBy(parcel => parcel.Name)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ = await dbContext.SaveChangesAsync(cancellationToken);
}
