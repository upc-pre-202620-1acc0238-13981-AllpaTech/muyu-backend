using System.ComponentModel.DataAnnotations;
using Muyu.Modules.ParcelManagement.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.ParcelManagement.Application;

public sealed record RegisterParcelCommand(
    Guid ProducerId,
    [Required, MaxLength(120)] string Name,
    [Required] string BoundaryGeoJson,
    [Range(0.0001, double.MaxValue)] decimal AreaHectares) : ICommand<ParcelResponse>;

public sealed record GetParcelByIdQuery(Guid ParcelId) : IQuery<ParcelResponse?>;

public sealed record ListParcelsByProducerQuery(Guid ProducerId) : IQuery<IReadOnlyList<ParcelResponse>>;

public sealed record ActivateParcelCommand(Guid ParcelId) : ICommand<bool>;

public sealed record ParcelResponse(
    Guid Id,
    Guid ProducerId,
    string Name,
    string BoundaryGeoJson,
    decimal AreaHectares,
    ParcelStatus Status,
    DateTimeOffset CreatedAtUtc);
