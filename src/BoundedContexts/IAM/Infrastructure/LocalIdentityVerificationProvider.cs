using Muyu.Modules.IAM.Application;
using Muyu.Modules.IAM.Domain;

namespace Muyu.Modules.IAM.Infrastructure;

/// <summary>
/// Adaptador de demostración. Puede sustituirse por RENIEC/SUNAT sin cambiar Application ni Domain.
/// </summary>
public sealed class LocalIdentityVerificationProvider : IIdentityVerificationProvider
{
    public Task<bool> IsActiveAsync(
        IdentityDocument document,
        CancellationToken cancellationToken = default)
    {
        var expectedLength = document.Type is IdentityDocumentType.Dni ? 8 : 11;
        var isValid = document.Number.Length == expectedLength
            && document.Number.Any(character => character != '0');

        return Task.FromResult(isValid);
    }
}
