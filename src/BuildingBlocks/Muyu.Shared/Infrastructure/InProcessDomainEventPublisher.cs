using System.Collections;
using Muyu.Shared.Application;
using Muyu.Shared.Domain;

namespace Muyu.Shared.Infrastructure;

/// <summary>
/// Publica eventos dentro del proceso sin introducir dependencias entre bounded contexts.
/// Puede reemplazarse por un outbox/broker sin modificar el dominio.
/// </summary>
public sealed class InProcessDomainEventPublisher(IServiceProvider serviceProvider) : IDomainEventPublisher
{
    public async Task PublishAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var collectionType = typeof(IEnumerable<>).MakeGenericType(handlerType);
        var handlers = serviceProvider.GetService(collectionType) as IEnumerable;

        if (handlers is null)
        {
            return;
        }

        var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
            ?? throw new InvalidOperationException($"No se encontró HandleAsync para {handlerType.Name}.");

        foreach (var handler in handlers)
        {
            var task = handleMethod.Invoke(handler, [domainEvent, cancellationToken]) as Task
                ?? throw new InvalidOperationException($"El manejador {handler?.GetType().Name} no devolvió Task.");

            await task.ConfigureAwait(false);
        }
    }
}
