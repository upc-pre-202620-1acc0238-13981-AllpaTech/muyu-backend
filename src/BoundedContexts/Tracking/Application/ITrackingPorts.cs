using Muyu.Modules.Tracking.Domain;

namespace Muyu.Modules.Tracking.Application;

public interface IMilestoneRepository
{
    Task AddAsync(Milestone milestone, CancellationToken cancellationToken = default);

    Task<Milestone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Milestone?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Milestone>> ListByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICloudStorage
{
    Task<StoredFile> StoreAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}

public sealed record StoredFile(string Uri, string Sha256, long SizeBytes);
