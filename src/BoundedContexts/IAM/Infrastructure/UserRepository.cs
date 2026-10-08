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

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<User>()
            .AsNoTracking()
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> ExistsByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<User>().AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<bool> ExistsByDocumentAsync(
        string documentNumber,
        CancellationToken cancellationToken = default) =>
        dbContext.Set<User>().AnyAsync(
            user => user.IdentityDocument.Number == documentNumber,
            cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await dbContext.Set<User>().AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

public sealed class RoleRepository(MuyuDbContext dbContext) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Set<Role>()
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Name == name.ToUpperInvariant(), cancellationToken);
}
