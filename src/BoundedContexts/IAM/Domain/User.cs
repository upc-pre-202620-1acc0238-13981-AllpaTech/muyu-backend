using System.Net.Mail;
using Muyu.Shared.Domain;

namespace Muyu.Modules.IAM.Domain;

public sealed class User : AggregateRoot
{
    private User()
    {
    }

    private User(string email, string displayName, Guid roleId)
    {
        Email = NormalizeEmail(email);
        DisplayName = NormalizeDisplayName(displayName);
        RoleId = roleId;
        IsActive = true;
    }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public Guid RoleId { get; private set; }

    public Role? Role { get; private set; }

    public static User Create(string email, string displayName, Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("El rol es obligatorio.", nameof(roleId));
        }

        var user = new User(email, displayName, roleId);
        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email, DateTimeOffset.UtcNow));
        return user;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
        RaiseDomainEvent(new UserDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow));
    }

    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalized = email.Trim().ToLowerInvariant();
        _ = new MailAddress(normalized);
        return normalized;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return displayName.Trim();
    }
}
