using Muyu.Shared.Domain;

namespace Muyu.Modules.ParcelManagement.Domain;

public sealed class Parcel : AggregateRoot
{
    private Parcel()
    {
    }

    private Parcel(Guid producerId, string name, GPSBoundary boundary)
    {
        if (producerId == Guid.Empty)
        {
            throw new ArgumentException("El productor es obligatorio.", nameof(producerId));
        }

        ProducerId = producerId;
        Name = NormalizeName(name);
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        Status = ParcelStatus.Draft;
    }

    public Guid ProducerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public GPSBoundary Boundary { get; private set; } = null!;

    public ParcelStatus Status { get; private set; }

    public static Parcel Register(Guid producerId, string name, GPSBoundary boundary)
    {
        var parcel = new Parcel(producerId, name, boundary);
        parcel.RaiseDomainEvent(new ParcelRegisteredDomainEvent(
            parcel.Id,
            parcel.ProducerId,
            DateTimeOffset.UtcNow));
        return parcel;
    }

    public void Activate()
    {
        if (Status is ParcelStatus.Archived)
        {
            throw new InvalidOperationException("Una parcela archivada no puede activarse.");
        }

        Status = ParcelStatus.Active;
        MarkAsUpdated();
        RaiseDomainEvent(new ParcelActivatedDomainEvent(Id, DateTimeOffset.UtcNow));
    }

    public void Suspend()
    {
        if (Status is not ParcelStatus.Active)
        {
            throw new InvalidOperationException("Solo una parcela activa puede suspenderse.");
        }

        Status = ParcelStatus.Suspended;
        MarkAsUpdated();
    }

    public void Archive()
    {
        Status = ParcelStatus.Archived;
        MarkAsUpdated();
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
