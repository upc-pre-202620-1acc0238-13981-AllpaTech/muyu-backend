using Microsoft.EntityFrameworkCore;
using Muyu.Modules.ClimateAlerts.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.ClimateAlerts.Infrastructure;

public sealed class AlertEntityConfiguration : IModuleEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<ClimateAlert>();
        builder.ToTable("climate_alerts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Hazard).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.Message).HasMaxLength(300).IsRequired();
        builder.Property(item => item.MeasuredValue).HasPrecision(18, 4);
        builder.Property(item => item.Unit).HasMaxLength(20).IsRequired();
        builder.HasIndex(item => new { item.ParcelId, item.Status });
    }
}
