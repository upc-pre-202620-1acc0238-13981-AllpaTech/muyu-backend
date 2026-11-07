using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Application;

public sealed record ChangePasswordCommand(
    [Required, MinLength(8)] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword) : ICommand<bool>;

public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher<User> passwordHasher)
{
    public async Task<bool> HandleForUserAsync(
        Guid userId,
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetTrackedByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            command.CurrentPassword);
        if (verification is PasswordVerificationResult.Failed)
        {
            return false;
        }

        user.ChangePasswordHash(passwordHasher.HashPassword(user, command.NewPassword));
        await users.SaveChangesAsync(cancellationToken);
        return true;
    }
}
