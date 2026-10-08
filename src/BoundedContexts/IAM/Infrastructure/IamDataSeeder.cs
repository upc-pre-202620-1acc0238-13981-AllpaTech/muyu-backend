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
    private static readonly Guid AgricultorRoleId = new("deec5c58-b719-4c83-96fc-a88ea9eadf74");
    private static readonly Guid ComercianteRoleId = new("663ea31d-42ab-43f1-a727-b4eb93f9a022");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(AdministratorRoleId, Role.Administrator, cancellationToken);
        await EnsureRoleAsync(AgricultorRoleId, Role.Agricultor, cancellationToken);
        await EnsureRoleAsync(ComercianteRoleId, Role.Comerciante, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

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

        var administrator = User.Create(
            normalizedEmail,
            displayName,
            IdentityDocument.Create(IdentityDocumentType.Dni, "99999999"),
            AdministratorRoleId);
        administrator.ChangePasswordHash(passwordHasher.HashPassword(administrator, password));

        dbContext.Add(administrator);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRoleAsync(
        Guid id,
        string name,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Set<Role>().AnyAsync(role => role.Name == name, cancellationToken))
        {
            dbContext.Add(Role.Create(id, name));
        }
    }
}
