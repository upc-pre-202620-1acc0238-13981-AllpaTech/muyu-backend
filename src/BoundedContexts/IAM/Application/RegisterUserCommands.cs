using System.ComponentModel.DataAnnotations;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Application;

public sealed record RegisterAgricultorCommand(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(120)] string DisplayName,
    [Required, RegularExpression(@"^\d{8}$")] string Dni) : ICommand<UserProfileResponse>;

public sealed record RegisterComercianteCommand(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(120)] string DisplayName,
    [Required, RegularExpression(@"^\d{11}$")] string Ruc) : ICommand<UserProfileResponse>;

/// <summary>Creates one mobile account that may use both MUYU experiences.</summary>
public sealed record RegisterMobileAccountCommand(
    [Required, MaxLength(120)] string Name,
    [Required, EmailAddress] string Email,
    [Required, MaxLength(20)] string Phone,
    [Required, MaxLength(40)] string Birthdate,
    [Required, MinLength(8)] string Password,
    [Required, RegularExpression(@"^\d{8}$")] string Dni) : ICommand<UserProfileResponse>;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    string DocumentType,
    string DocumentNumber,
    string Status,
    DateTimeOffset CreatedAtUtc);
