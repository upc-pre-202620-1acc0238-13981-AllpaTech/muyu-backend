using System.ComponentModel.DataAnnotations;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Application;

public sealed record LoginCommand(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password) : ICommand<LoginResponse?>;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserSummary User);

public sealed record UserSummary(Guid Id, string Email, string DisplayName, string Role);
