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

    /// <summary>
    /// Inicializa una raíz de agregado con un identificador generado automáticamente.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Inicializa una raíz de agregado con el identificador especificado.
    /// </summary>
    /// <param name="id">Identificador único de la raíz de agregado.</param>
    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    /// <summary>
    /// Obtiene una vista de solo lectura de los eventos de dominio pendientes.
    /// </summary>
    /// <returns>Vista de solo lectura de los eventos registrados en esta raíz.</returns>
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// Elimina todos los eventos de dominio pendientes de esta raíz.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Registra un evento de dominio generado por un cambio en esta raíz.
    /// </summary>
    /// <param name="domainEvent">Evento que se agrega a la colección de eventos pendientes.</param>
    /// <exception cref="ArgumentNullException">
    /// Se produce si <paramref name="domainEvent"/> es <see langword="null"/>.
    /// </exception>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}