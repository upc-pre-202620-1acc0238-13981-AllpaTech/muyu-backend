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

    /// <summary>
    /// Obtiene el identificador único de la entidad.
    /// </summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();

    /// <summary>
    /// Obtiene la fecha y hora UTC en que se creó la entidad.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; protected init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Obtiene la fecha y hora UTC de la última actualización, o <see langword="null"/>
    /// si la entidad aún no se ha marcado como actualizada.
    /// </summary>
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Marca la entidad con la fecha y hora UTC actuales como última actualización.
    /// </summary>
    protected void MarkAsUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}