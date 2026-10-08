using Microsoft.AspNetCore.Identity;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Application;

public sealed class RegisterAgricultorCommandHandler(
    UserRegistrationService registrationService)
    : ICommandHandler<RegisterAgricultorCommand, UserProfileResponse>
{
    public Task<UserProfileResponse> HandleAsync(
        RegisterAgricultorCommand command,
        CancellationToken cancellationToken = default) =>
        registrationService.RegisterAsync(
            command.Email,
            command.Password,
            command.DisplayName,
            IdentityDocument.Create(IdentityDocumentType.Dni, command.Dni),
            Role.Agricultor,
            cancellationToken);
}

public sealed class RegisterComercianteCommandHandler(
    UserRegistrationService registrationService)
    : ICommandHandler<RegisterComercianteCommand, UserProfileResponse>
{
    public Task<UserProfileResponse> HandleAsync(
        RegisterComercianteCommand command,
        CancellationToken cancellationToken = default) =>
        registrationService.RegisterAsync(
            command.Email,
            command.Password,
            command.DisplayName,
            IdentityDocument.Create(IdentityDocumentType.Ruc, command.Ruc),
            Role.Comerciante,
            cancellationToken);
}

public sealed class UserRegistrationService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IIdentityVerificationProvider identityVerificationProvider,
    IPasswordHasher<User> passwordHasher)
{
    public async Task<UserProfileResponse> RegisterAsync(
        string email,
        string password,
        string displayName,
        IdentityDocument document,
        string roleName,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new UserRegistrationException("El correo ya se encuentra registrado.");
        }

        if (await userRepository.ExistsByDocumentAsync(document.Number, cancellationToken))
        {
            throw new UserRegistrationException("El documento de identidad ya se encuentra registrado.");
        }

        if (!await identityVerificationProvider.IsActiveAsync(document, cancellationToken))
        {
            throw new UserRegistrationException("El documento no es válido o no se encuentra activo.");
        }

        var role = await roleRepository.GetByNameAsync(roleName, cancellationToken)
            ?? throw new InvalidOperationException($"El rol {roleName} no fue configurado.");

        var user = User.Create(normalizedEmail, displayName, document, role.Id);
        user.ChangePasswordHash(passwordHasher.HashPassword(user, password));

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return UserProfileMapper.Map(user, role.Name);
    }
}

public sealed class UserRegistrationException(string message) : Exception(message);

internal static class UserProfileMapper
{
    public static UserProfileResponse Map(User user, string? role = null) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        role ?? user.Role?.Name ?? "USER",
        user.IdentityDocument.Type.ToString().ToUpperInvariant(),
        user.IdentityDocument.Number,
        user.Status.ToString().ToUpperInvariant(),
        user.CreatedAtUtc);
}
