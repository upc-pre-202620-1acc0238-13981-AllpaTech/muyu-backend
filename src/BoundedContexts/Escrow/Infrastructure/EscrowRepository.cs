using Microsoft.EntityFrameworkCore;
using Muyu.Modules.Escrow.Application;
using Muyu.Modules.Escrow.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Escrow.Infrastructure;

public sealed class EscrowRepository(MuyuDbContext dbContext) : IEscrowRepository
{
    public Task AddAsync(EscrowAccount account, CancellationToken cancellationToken = default) =>
        dbContext.Set<EscrowAccount>().AddAsync(account, cancellationToken).AsTask();

    public Task<EscrowAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<EscrowAccount>()
            .Include(item => item.Payouts)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<EscrowAccount?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<EscrowAccount>()
            .AsNoTracking()
            .Include(item => item.Payouts)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> ExistsForAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default) =>
        dbContext.Set<EscrowAccount>().AnyAsync(item => item.AgreementId == agreementId, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ = await dbContext.SaveChangesAsync(cancellationToken);
}
