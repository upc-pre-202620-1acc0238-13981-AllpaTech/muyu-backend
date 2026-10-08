using Muyu.Shared.Domain;

namespace Muyu.Modules.IAM.Domain;

public sealed class Role : BaseEntity
{
    public const string Agricultor = "AGRICULTOR";
    public const string Comerciante = "COMERCIANTE";
    public const string Administrator = "ADMIN";

    private Role()
    {
    }

    private Role(Guid id, string name)
        : base(id)
    {
        Name = NormalizeName(name);
    }

    public string Name { get; private set; } = string.Empty;

    public static Role Create(string name) => new(Guid.NewGuid(), name);

    internal static Role Create(Guid id, string name) => new(id, name);

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim().ToUpperInvariant();
    }
}
