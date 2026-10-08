using Microsoft.EntityFrameworkCore;
using Muyu.Modules.Tracking.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Tracking.Infrastructure;

public sealed class TrackingEntityConfiguration : IModuleEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var milestone = modelBuilder.Entity<Milestone>();
        milestone.ToTable("tracking_milestones");
        milestone.HasKey(item => item.Id);
        milestone.Property(item => item.Title).HasMaxLength(160).IsRequired();
        milestone.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        milestone.Property(item => item.ReviewComment).HasMaxLength(500);
        milestone.HasIndex(item => item.AgreementId);
        milestone.HasMany(item => item.Evidences)
            .WithOne()
            .HasForeignKey(item => item.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);
        milestone.Navigation(item => item.Evidences).UsePropertyAccessMode(PropertyAccessMode.Field);

        var evidence = modelBuilder.Entity<Evidence>();
        evidence.ToTable("tracking_evidences");
        evidence.HasKey(item => item.Id);
        evidence.Property(item => item.FileName).HasMaxLength(255).IsRequired();
        evidence.Property(item => item.ContentType).HasMaxLength(120).IsRequired();
        evidence.Property(item => item.StorageUri).HasMaxLength(1000).IsRequired();
        evidence.Property(item => item.Sha256).HasMaxLength(64).IsRequired();
        evidence.Property(item => item.Note).HasMaxLength(500);
        evidence.Property(item => item.Latitude).IsRequired();
        evidence.Property(item => item.Longitude).IsRequired();
        evidence.HasIndex(item => new { item.MilestoneId, item.Sha256 }).IsUnique();
    }
}
