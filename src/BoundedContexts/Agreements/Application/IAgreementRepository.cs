using Muyu.Modules.Agreements.Domain;

namespace Muyu.Modules.Agreements.Application;

public interface IAgreementRepository
{
    Task AddAsync(Agreement agreement, CancellationToken cancellationToken = default);

    Task<Agreement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Agreement?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Agreement>> ListByProducerAsync(Guid producerId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
