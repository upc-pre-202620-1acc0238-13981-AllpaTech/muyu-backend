using Microsoft.EntityFrameworkCore;
using Muyu.Modules.Escrow.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Escrow.Infrastructure;

public sealed class EscrowEntityConfiguration : IModuleEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<EscrowAccount>();
        account.ToTable("escrow_accounts");
        account.HasKey(item => item.Id);
        account.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        account.Property(item => item.FundedAmount).HasPrecision(18, 2);
        account.Property(item => item.ReleasedAmount).HasPrecision(18, 2);
        account.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        account.Ignore(item => item.ReservedAmount);
        account.Ignore(item => item.AvailableBalance);
        account.HasIndex(item => item.AgreementId).IsUnique();
        account.HasMany(item => item.Payouts)
            .WithOne()
            .HasForeignKey(item => item.EscrowAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        account.Navigation(item => item.Payouts).UsePropertyAccessMode(PropertyAccessMode.Field);

        var payout = modelBuilder.Entity<PayoutRecord>();
        payout.ToTable("escrow_payout_records");
        payout.HasKey(item => item.Id);
        payout.Property(item => item.Amount).HasPrecision(18, 2);
        payout.Property(item => item.Purpose).HasMaxLength(250).IsRequired();
        payout.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        payout.Property(item => item.GatewayReference).HasMaxLength(150);
    }
}
