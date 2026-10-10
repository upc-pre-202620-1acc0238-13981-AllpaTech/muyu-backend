namespace Muyu.Shared.Domain;

/// <summary>
/// Proporciona propiedades de identidad y marcas de tiempo comunes a las entidades de Muyu.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Inicializa una entidad con un identificador generado automáticamente.
    /// </summary>
    protected BaseEntity()
    {
    }

    /// <summary>
    /// Inicializa una entidad con el identificador especificado.
    /// </summary>
    /// <param name="id">Identificador único de la entidad.</param>
    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; protected init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; protected init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    protected void MarkAsUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
