using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.IAM.Infrastructure;

public sealed class IamDataSeeder(
    MuyuDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration)
{
    private static readonly Guid AdministratorRoleId = new("53a9c0b4-3a45-4d15-86d1-d89fc4b07533");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Set<Role>().AnyAsync(cancellationToken))
        {
            dbContext.Add(Role.Create(AdministratorRoleId, "ADMIN"));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var seedSection = configuration.GetSection("Iam:SeedUser");
        var email = seedSection["Email"];
        var password = seedSection["Password"];
        var displayName = seedSection["DisplayName"] ?? "Administrador Muyu";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var normalizedEmail = User.NormalizeEmail(email);
        if (await dbContext.Set<User>().AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        var administrator = User.Create(normalizedEmail, displayName, AdministratorRoleId);
        administrator.ChangePasswordHash(passwordHasher.HashPassword(administrator, password));

        dbContext.Add(administrator);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
