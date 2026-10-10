namespace Muyu.Shared.Domain;

/// <summary>
/// Representa la raíz de un agregado y mantiene los eventos de dominio generados por sus cambios.
/// </summary>
/// <remarks>
/// Los eventos permanecen pendientes hasta que la infraestructura los publica y limpia.
/// </remarks>
public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
