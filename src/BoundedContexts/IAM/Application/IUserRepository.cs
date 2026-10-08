using Muyu.Modules.IAM.Domain;

namespace Muyu.Modules.IAM.Application;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
