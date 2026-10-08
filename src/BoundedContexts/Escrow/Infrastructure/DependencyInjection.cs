using Microsoft.Extensions.DependencyInjection;
using Muyu.Modules.Escrow.Application;
using Muyu.Shared.Application;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Escrow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEscrowModule(this IServiceCollection services)
    {
        services.AddSingleton<IModuleEntityConfiguration, EscrowEntityConfiguration>();
        services.AddScoped<IEscrowRepository, EscrowRepository>();
        services.AddScoped<IPaymentGateway, PaymentGateway>();
        services.AddScoped<ICommandHandler<OpenEscrowCommand, EscrowAccountResponse>, OpenEscrowCommandHandler>();
        services.AddScoped<IQueryHandler<GetEscrowByIdQuery, EscrowAccountResponse?>, GetEscrowByIdQueryHandler>();
        services.AddScoped<ICommandHandler<FundEscrowCommand, bool>, FundEscrowCommandHandler>();
        services.AddScoped<ICommandHandler<ReleasePayoutCommand, PayoutResponse?>, ReleasePayoutCommandHandler>();
        return services;
    }
}
