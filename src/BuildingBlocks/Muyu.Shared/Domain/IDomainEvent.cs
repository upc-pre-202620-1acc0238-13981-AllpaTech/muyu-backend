namespace Muyu.Shared.Domain;

/// <summary>Hecho de negocio inmutable producido por un agregado.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
