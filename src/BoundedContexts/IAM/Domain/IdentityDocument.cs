namespace Muyu.Modules.IAM.Domain;

public sealed record IdentityDocument
{
    private IdentityDocument()
    {
    }

    private IdentityDocument(IdentityDocumentType type, string number)
    {
        Type = type;
        Number = number;
    }

    public IdentityDocumentType Type { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public static IdentityDocument Create(IdentityDocumentType type, string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        var normalized = new string(number.Where(char.IsDigit).ToArray());
        var expectedLength = type is IdentityDocumentType.Dni ? 8 : 11;

        if (normalized.Length != expectedLength)
        {
            throw new ArgumentException(
                $"El {type.ToString().ToUpperInvariant()} debe contener {expectedLength} dígitos.",
                nameof(number));
        }

        return new IdentityDocument(type, normalized);
    }
}

public enum IdentityDocumentType
{
    Dni = 1,
    Ruc = 2
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}
