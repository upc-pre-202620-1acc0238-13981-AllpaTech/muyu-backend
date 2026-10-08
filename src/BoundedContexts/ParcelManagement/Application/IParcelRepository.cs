using Muyu.Modules.ParcelManagement.Domain;

namespace Muyu.Modules.ParcelManagement.Application;

public interface IParcelRepository
{
    Task AddAsync(Parcel parcel, CancellationToken cancellationToken = default);

    Task<Parcel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Parcel?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Parcel>> ListByProducerAsync(Guid producerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Parcel>> ListAvailableAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
