using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.IAM.Infrastructure;

public sealed class IamEntityConfiguration : IModuleEntityConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureRole(modelBuilder.Entity<Role>());
        ConfigureUser(modelBuilder.Entity<User>());
    }

    private static void ConfigureRole(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("iam_roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(role => role.Name).IsUnique();
    }

    private static void ConfigureUser(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("iam_users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(254).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Ignore(user => user.IsActive);
        builder.HasIndex(user => user.Email).IsUnique();

        builder.OwnsOne(user => user.IdentityDocument, document =>
        {
            document.Property(value => value.Type)
                .HasColumnName("IdentityDocumentType")
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();
            document.Property(value => value.Number)
                .HasColumnName("IdentityDocumentNumber")
                .HasMaxLength(11)
                .IsRequired();
            document.HasIndex(value => value.Number).IsUnique();
        });

        builder
            .HasOne(user => user.Role)
            .WithMany()
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
