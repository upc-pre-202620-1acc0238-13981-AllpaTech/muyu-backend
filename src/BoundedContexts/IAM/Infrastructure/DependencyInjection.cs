using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Muyu.Modules.IAM.Application;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Application;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.IAM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIamModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer es obligatorio.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience es obligatorio.")
            .Validate(options => options.SigningKey.Length >= 32, "Jwt:SigningKey debe tener al menos 32 caracteres.")
            .Validate(options => options.ExpirationMinutes > 0, "Jwt:ExpirationMinutes debe ser mayor que cero.")
            .ValidateOnStart();

        services.AddSingleton<IModuleEntityConfiguration, IamEntityConfiguration>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IIdentityVerificationProvider, LocalIdentityVerificationProvider>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse?>, LoginCommandHandler>();
        services.AddScoped<UserRegistrationService>();
        services.AddScoped<ICommandHandler<RegisterAgricultorCommand, UserProfileResponse>, RegisterAgricultorCommandHandler>();
        services.AddScoped<ICommandHandler<RegisterComercianteCommand, UserProfileResponse>, RegisterComercianteCommandHandler>();
        services.AddScoped<IQueryHandler<GetUserProfileQuery, UserProfileResponse?>, GetUserProfileQueryHandler>();
        services.AddScoped<IamDataSeeder>();
        services.AddScoped<IamSchemaUpgrade>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }
}
