using Muyu.Shared.Domain;

namespace Muyu.Shared.Application;

public interface ICommand<out TResult>;

/// <summary>
/// Define el contrato para procesar un comando específico dentro del patrón CQRS.
/// Se encarga de ejecutar la lógica de negocio o mutaciones de estado y retornar un resultado.
/// </summary>
/// <typeparam name="TCommand">Tipo de comando que implementa <see cref="ICommand{TResult}"/>.</typeparam>
/// <typeparam name="TResult">Tipo de dato retornado tras la ejecución del comando.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQuery<out TResult>;

/// <summary>
/// Define el contrato para gestionar y resolver una consulta (Query) específica en el flujo de lectura de CQRS.
/// </summary>
/// <typeparam name="TQuery">Tipo de consulta que implementa <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">Tipo de dato devuelto por la consulta.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Define el mecanismo para publicar y despachar eventos de dominio ocurridos en los agregados hacia sus respectivos manejadores.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Define el contrato para consumir y reaccionar a un evento de dominio específico (side-effects, notificaciones o proyecciones).
/// </summary>
/// <typeparam name="TDomainEvent">Tipo de evento de dominio que implementa <see cref="IDomainEvent"/>.</typeparam>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
