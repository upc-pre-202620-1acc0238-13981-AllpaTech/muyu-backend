using Microsoft.EntityFrameworkCore;
using Muyu.Shared.Application;
using Muyu.Shared.Domain;

namespace Muyu.Shared.Infrastructure;

public sealed class MuyuDbContext(
    DbContextOptions<MuyuDbContext> options,
    IEnumerable<IModuleEntityConfiguration> moduleConfigurations,
    IDomainEventPublisher domainEventPublisher) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var configuration in moduleConfigurations)
        {
            configuration.Configure(modelBuilder);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RejectDirectModificationOfCreationDates();

        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.GetDomainEvents().Count > 0)
            .ToArray();

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.GetDomainEvents())
            .ToArray();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }

    private void RejectDirectModificationOfCreationDates()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
            }
        }
    }
}
