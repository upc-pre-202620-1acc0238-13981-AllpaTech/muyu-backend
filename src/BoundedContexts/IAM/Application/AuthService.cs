using Microsoft.AspNetCore.Identity;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Application;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IJwtProvider jwtProvider) : ICommandHandler<LoginCommand, LoginResponse?>
{
    public async Task<LoginResponse?> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(command.Email);
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            command.Password);

        if (verification is PasswordVerificationResult.Failed)
        {
            return null;
        }

        var token = jwtProvider.Create(user);

        return new LoginResponse(
            token.Value,
            token.ExpiresAtUtc,
            new UserSummary(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Role?.Name ?? "USER"));
    }
}
