using Microsoft.EntityFrameworkCore;
using Muyu.Modules.Agreements.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Agreements.Infrastructure;

public sealed class AgreementEntityConfiguration : IModuleEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var agreement = modelBuilder.Entity<Agreement>();
        agreement.ToTable("agreements_agreements");
        agreement.HasKey(item => item.Id);
        agreement.Property(item => item.PrincipalAmount).HasPrecision(18, 2);
        agreement.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        agreement.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        agreement.HasIndex(item => item.ParcelId);

        agreement
            .HasMany(item => item.DisbursementPlans)
            .WithOne()
            .HasForeignKey(item => item.AgreementId)
            .OnDelete(DeleteBehavior.Cascade);

        agreement.Navigation(item => item.DisbursementPlans)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        var plan = modelBuilder.Entity<DisbursementPlan>();
        plan.ToTable("agreements_disbursement_plans");
        plan.HasKey(item => item.Id);
        plan.Property(item => item.Amount).HasPrecision(18, 2);
        plan.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        plan.HasIndex(item => new { item.AgreementId, item.Sequence }).IsUnique();
    }
}
