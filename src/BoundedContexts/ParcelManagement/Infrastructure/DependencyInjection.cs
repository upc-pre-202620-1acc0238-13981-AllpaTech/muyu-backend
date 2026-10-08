using Microsoft.Extensions.DependencyInjection;
using Muyu.Modules.ParcelManagement.Application;
using Muyu.Shared.Application;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.ParcelManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddParcelManagementModule(this IServiceCollection services)
    {
        services.AddSingleton<IModuleEntityConfiguration, ParcelEntityConfiguration>();
        services.AddScoped<IParcelRepository, ParcelRepository>();
        services.AddScoped<ICommandHandler<RegisterParcelCommand, ParcelResponse>, RegisterParcelCommandHandler>();
        services.AddScoped<IQueryHandler<GetParcelByIdQuery, ParcelResponse?>, GetParcelByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListParcelsByProducerQuery, IReadOnlyList<ParcelResponse>>, ListParcelsByProducerQueryHandler>();
        services.AddScoped<ICommandHandler<ActivateParcelCommand, bool>, ActivateParcelCommandHandler>();
        return services;
    }
}
