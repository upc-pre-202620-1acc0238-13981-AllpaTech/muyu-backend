using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Application;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileResponse?>;

public sealed class GetUserProfileQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse?>
{
    public async Task<UserProfileResponse?> HandleAsync(
        GetUserProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        return user is null ? null : UserProfileMapper.Map(user);
    }
}
