namespace Muyu.Shared.Domain;

/// <summary>
/// Base de todas las entidades persistentes de Muyu.
/// </summary>
public abstract class BaseEntity
{
    protected BaseEntity()
    {
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; protected init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; protected init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    protected void MarkAsUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
