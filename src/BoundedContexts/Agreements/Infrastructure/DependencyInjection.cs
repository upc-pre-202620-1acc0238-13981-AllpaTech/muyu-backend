using Microsoft.Extensions.DependencyInjection;
using Muyu.Modules.Agreements.Application;
using Muyu.Shared.Application;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Agreements.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAgreementsModule(this IServiceCollection services)
    {
        services.AddSingleton<IModuleEntityConfiguration, AgreementEntityConfiguration>();
        services.AddScoped<IAgreementRepository, AgreementRepository>();
        services.AddScoped<ICommandHandler<CreateAgreementCommand, AgreementResponse>, CreateAgreementCommandHandler>();
        services.AddScoped<IQueryHandler<GetAgreementByIdQuery, AgreementResponse?>, GetAgreementByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListAgreementsByProducerQuery, IReadOnlyList<AgreementResponse>>, ListAgreementsByProducerQueryHandler>();
        services.AddScoped<ICommandHandler<ActivateAgreementCommand, bool>, ActivateAgreementCommandHandler>();
        return services;
    }
}
