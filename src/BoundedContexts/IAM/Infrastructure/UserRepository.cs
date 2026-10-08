using Microsoft.EntityFrameworkCore;
using Muyu.Modules.IAM.Application;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.IAM.Infrastructure;

public sealed class UserRepository(MuyuDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<User>()
            .AsNoTracking()
            .Include(user => user.Role)
            .SingleOrDefaultAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);
}
