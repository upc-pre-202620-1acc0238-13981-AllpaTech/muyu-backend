using Muyu.Shared.Domain;

namespace Muyu.Modules.ParcelManagement.Domain;

public sealed record ParcelRegisteredDomainEvent(
    Guid ParcelId,
    Guid ProducerId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ParcelActivatedDomainEvent(
    Guid ParcelId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
