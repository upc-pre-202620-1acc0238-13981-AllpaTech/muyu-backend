using Muyu.Modules.IAM.Domain;

namespace Muyu.Modules.IAM.Application;

public interface IIdentityVerificationProvider
{
    Task<bool> IsActiveAsync(
        IdentityDocument document,
        CancellationToken cancellationToken = default);
}
