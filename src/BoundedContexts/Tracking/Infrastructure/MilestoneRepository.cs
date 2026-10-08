using Microsoft.EntityFrameworkCore;
using Muyu.Modules.Tracking.Application;
using Muyu.Modules.Tracking.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Tracking.Infrastructure;

public sealed class MilestoneRepository(MuyuDbContext dbContext) : IMilestoneRepository
{
    public Task AddAsync(Milestone milestone, CancellationToken cancellationToken = default) =>
        dbContext.Set<Milestone>().AddAsync(milestone, cancellationToken).AsTask();

    public Task<Milestone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Milestone>()
            .Include(item => item.Evidences)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Milestone?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Milestone>()
            .AsNoTracking()
            .Include(item => item.Evidences)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Milestone>> ListByAgreementAsync(
        Guid agreementId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<Milestone>()
            .AsNoTracking()
            .Include(item => item.Evidences)
            .Where(item => item.AgreementId == agreementId)
            .OrderBy(item => item.DueDate)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ = await dbContext.SaveChangesAsync(cancellationToken);
}
