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

        await EnsureConfiguredUserAsync(
            configuration.GetSection("Iam:SeedUser"),
            AdministratorRoleId,
            IdentityDocumentType.Dni,
            "99999999",
            cancellationToken);

        foreach (var demoSection in configuration.GetSection("Iam:DemoUsers").GetChildren())
        {
            var roleName = demoSection["Role"]?.Trim().ToUpperInvariant();
            var (roleId, defaultDocumentType) = roleName switch
            {
                Role.Agricultor => (AgricultorRoleId, IdentityDocumentType.Dni),
                Role.Comerciante => (ComercianteRoleId, IdentityDocumentType.Ruc),
                _ => throw new InvalidOperationException("El usuario demo tiene un rol no válido.")
            };

            var documentType = Enum.TryParse<IdentityDocumentType>(
                demoSection["DocumentType"],
                true,
                out var parsedType)
                    ? parsedType
                    : defaultDocumentType;

            await EnsureConfiguredUserAsync(
                demoSection,
                roleId,
                documentType,
                demoSection["DocumentNumber"] ?? string.Empty,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureConfiguredUserAsync(
        IConfigurationSection section,
        Guid roleId,
        IdentityDocumentType documentType,
        string documentNumber,
        CancellationToken cancellationToken)
    {
        var email = section["Email"];
        var password = section["Password"];
        var displayName = section["DisplayName"] ?? "Usuario Muyu";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var normalizedEmail = User.NormalizeEmail(email);
        if (await dbContext.Set<User>().AnyAsync(
                user => user.Email == normalizedEmail,
                cancellationToken))
        {
            return;
        }

        var user = User.Create(
            normalizedEmail,
            displayName,
            IdentityDocument.Create(documentType, documentNumber),
            roleId);
        user.ChangePasswordHash(passwordHasher.HashPassword(user, password));
        dbContext.Add(user);
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
