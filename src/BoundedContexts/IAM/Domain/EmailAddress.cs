using System.Net.Mail;

namespace Muyu.Modules.IAM.Domain;

public sealed record EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var normalized = email.Trim().ToLowerInvariant();
        _ = new MailAddress(normalized);
        return new EmailAddress(normalized);
    }
}
