using Microsoft.Extensions.DependencyInjection;
using Muyu.Modules.ClimateAlerts.Application;
using Muyu.Shared.Application;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.ClimateAlerts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddClimateAlertsModule(this IServiceCollection services)
    {
        services.AddSingleton<IModuleEntityConfiguration, AlertEntityConfiguration>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<ICommandHandler<MonitorParcelCommand, MonitoringResult>, MonitorParcelCommandHandler>();
        services.AddScoped<IQueryHandler<ListOpenClimateAlertsQuery, IReadOnlyList<ClimateAlertResponse>>, ListOpenClimateAlertsQueryHandler>();
        services.AddScoped<ICommandHandler<AcknowledgeClimateAlertCommand, bool>, AcknowledgeClimateAlertCommandHandler>();
        services.AddScoped<ICommandHandler<ResolveClimateAlertCommand, bool>, ResolveClimateAlertCommandHandler>();
        services.AddHttpClient<IWeatherProvider, OpenWeatherAdapter>(client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        return services;
    }
}
