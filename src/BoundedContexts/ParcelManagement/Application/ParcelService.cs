using Muyu.Modules.ParcelManagement.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.ParcelManagement.Application;

public sealed class RegisterParcelCommandHandler(IParcelRepository repository)
    : ICommandHandler<RegisterParcelCommand, ParcelResponse>
{
    public async Task<ParcelResponse> HandleAsync(
        RegisterParcelCommand command,
        CancellationToken cancellationToken = default)
    {
        var boundary = GPSBoundary.Create(command.BoundaryGeoJson, command.AreaHectares);
        var parcel = Parcel.Register(command.ProducerId, command.Name, boundary);

        await repository.AddAsync(parcel, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ParcelMapping.Map(parcel);
    }
}

public sealed class GetParcelByIdQueryHandler(IParcelRepository repository)
    : IQueryHandler<GetParcelByIdQuery, ParcelResponse?>
{
    public async Task<ParcelResponse?> HandleAsync(
        GetParcelByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var parcel = await repository.GetByIdReadOnlyAsync(query.ParcelId, cancellationToken);
        return parcel is null ? null : ParcelMapping.Map(parcel);
    }
}

public sealed class ListParcelsByProducerQueryHandler(IParcelRepository repository)
    : IQueryHandler<ListParcelsByProducerQuery, IReadOnlyList<ParcelResponse>>
{
    public async Task<IReadOnlyList<ParcelResponse>> HandleAsync(
        ListParcelsByProducerQuery query,
        CancellationToken cancellationToken = default)
    {
        var parcels = await repository.ListByProducerAsync(query.ProducerId, cancellationToken);
        return parcels.Select(ParcelMapping.Map).ToArray();
    }
}

public sealed class ActivateParcelCommandHandler(IParcelRepository repository)
    : ICommandHandler<ActivateParcelCommand, bool>
{
    public async Task<bool> HandleAsync(
        ActivateParcelCommand command,
        CancellationToken cancellationToken = default)
    {
        var parcel = await repository.GetByIdAsync(command.ParcelId, cancellationToken);
        if (parcel is null)
        {
            return false;
        }

        parcel.Activate();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class ParcelMapping
{
    internal static ParcelResponse Map(Parcel parcel) =>
        new(
            parcel.Id,
            parcel.ProducerId,
            parcel.Name,
            parcel.Boundary.GeoJson,
            parcel.Boundary.AreaHectares,
            parcel.Status,
            parcel.CreatedAtUtc);
}
