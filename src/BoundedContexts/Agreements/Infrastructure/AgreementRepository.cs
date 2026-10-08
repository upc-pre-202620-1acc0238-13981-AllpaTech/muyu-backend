using Microsoft.EntityFrameworkCore;
using Muyu.Modules.Agreements.Application;
using Muyu.Modules.Agreements.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Agreements.Infrastructure;

public sealed class AgreementRepository(MuyuDbContext dbContext) : IAgreementRepository
{
    public Task AddAsync(Agreement agreement, CancellationToken cancellationToken = default) =>
        dbContext.Set<Agreement>().AddAsync(agreement, cancellationToken).AsTask();

    public Task<Agreement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Agreement>()
            .Include(item => item.DisbursementPlans)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Agreement?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Agreement>()
            .AsNoTracking()
            .Include(item => item.DisbursementPlans)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Agreement>> ListByProducerAsync(
        Guid producerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<Agreement>()
            .AsNoTracking()
            .Include(item => item.DisbursementPlans)
            .Where(item => item.ProducerId == producerId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ = await dbContext.SaveChangesAsync(cancellationToken);
}
