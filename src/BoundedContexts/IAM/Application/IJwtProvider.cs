using Muyu.Modules.IAM.Domain;

namespace Muyu.Modules.IAM.Application;

public interface IJwtProvider
{
    AccessToken Create(User user);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
