using Muyu.Shared.Domain;

namespace Muyu.Modules.IAM.Domain;

public sealed class User : AggregateRoot
{
    private User()
    {
    }

    private User(
        EmailAddress email,
        string displayName,
        IdentityDocument identityDocument,
        Guid roleId)
    {
        Email = email.Value;
        DisplayName = NormalizeDisplayName(displayName);
        IdentityDocument = identityDocument;
        RoleId = roleId;
        Status = UserStatus.Active;
    }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public IdentityDocument IdentityDocument { get; private set; } = null!;

    public UserStatus Status { get; private set; }

    public bool IsActive => Status is UserStatus.Active;

    public Guid RoleId { get; private set; }

    public Role? Role { get; private set; }

    public static User Create(
        string email,
        string displayName,
        IdentityDocument identityDocument,
        Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("El rol es obligatorio.", nameof(roleId));
        }

        ArgumentNullException.ThrowIfNull(identityDocument);

        var user = new User(EmailAddress.Create(email), displayName, identityDocument, roleId);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(
            user.Id,
            user.Email,
            identityDocument.Type,
            identityDocument.Number,
            DateTimeOffset.UtcNow));
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
        Status = UserStatus.Inactive;
        MarkAsUpdated();
        RaiseDomainEvent(new UserDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow));
    }

    public static string NormalizeEmail(string email) => EmailAddress.Create(email).Value;

    private static string NormalizeDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return displayName.Trim();
    }
}
