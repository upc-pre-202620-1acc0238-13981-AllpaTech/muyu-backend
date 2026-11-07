using Microsoft.EntityFrameworkCore;
using Muyu.Shared.Application;
using Muyu.Shared.Domain;

namespace Muyu.Shared.Infrastructure;

public sealed class MuyuDbContext(
    DbContextOptions<MuyuDbContext> options,
    IEnumerable<IModuleEntityConfiguration> moduleConfigurations,
    IDomainEventPublisher domainEventPublisher) : DbContext(options)
{
    public DbSet<MobileAppState> MobileAppStates => Set<MobileAppState>();
    public DbSet<MobilePlotData> MobilePlots => Set<MobilePlotData>();
    public DbSet<MobileVisitData> MobileVisits => Set<MobileVisitData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var configuration in moduleConfigurations)
        {
            configuration.Configure(modelBuilder);
        }

        modelBuilder.Entity<MobileAppState>(entity =>
        {
            entity.ToTable("mobile_app_states");
            entity.HasKey(state => state.UserId);
            entity.Property(state => state.PayloadJson).IsRequired();
            entity.Property(state => state.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<MobilePlotData>(entity =>
        {
            entity.ToTable("mobile_plots");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OwnerId).IsRequired();
            entity.Property(item => item.OwnerEmail).IsRequired();
            entity.Property(item => item.FundedByEmail);
            entity.Property(item => item.PayloadJson).IsRequired();
        });

        modelBuilder.Entity<MobileVisitData>(entity =>
        {
            entity.ToTable("mobile_visits");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FarmerEmail).IsRequired();
            entity.Property(item => item.MerchantEmail).IsRequired();
            entity.Property(item => item.PayloadJson).IsRequired();
        });
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

/// <summary>
/// Stores the complete server-owned mobile state for features that are not yet
/// represented by a dedicated bounded-context aggregate.
/// </summary>
public sealed class MobileAppState
{
    public Guid UserId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MobilePlotData
{
    public int Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerEmail { get; set; } = "";
    public string? FundedByEmail { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MobileVisitData
{
    public int Id { get; set; }
    public string FarmerEmail { get; set; } = "";
    public string MerchantEmail { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
