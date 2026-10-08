using Microsoft.EntityFrameworkCore;
using Muyu.Modules.ParcelManagement.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.ParcelManagement.Infrastructure;

public sealed class ParcelEntityConfiguration : IModuleEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Parcel>();

        builder.ToTable("parcel_management_parcels");
        builder.HasKey(parcel => parcel.Id);
        builder.Property(parcel => parcel.Name).HasMaxLength(120).IsRequired();
        builder.Property(parcel => parcel.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(parcel => new { parcel.ProducerId, parcel.Name }).IsUnique();

        builder.OwnsOne(parcel => parcel.Boundary, owned =>
        {
            owned.Property(boundary => boundary.GeoJson)
                .HasColumnName("BoundaryGeoJson")
                .HasColumnType("TEXT")
                .IsRequired();
            owned.Property(boundary => boundary.AreaHectares)
                .HasColumnName("AreaHectares")
                .HasPrecision(18, 4);
        });

        builder.Navigation(parcel => parcel.Boundary).IsRequired();
    }
}
