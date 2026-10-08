using Microsoft.Extensions.DependencyInjection;
using Muyu.Modules.Tracking.Application;
using Muyu.Shared.Application;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Tracking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTrackingModule(this IServiceCollection services)
    {
        services.AddSingleton<IModuleEntityConfiguration, TrackingEntityConfiguration>();
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        services.AddScoped<ICloudStorage, CloudStorage>();
        services.AddScoped<TrackingSchemaUpgrade>();
        services.AddScoped<ICommandHandler<ScheduleMilestoneCommand, MilestoneResponse>, ScheduleMilestoneCommandHandler>();
        services.AddScoped<IQueryHandler<GetMilestoneByIdQuery, MilestoneResponse?>, GetMilestoneByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListMilestonesByAgreementQuery, IReadOnlyList<MilestoneResponse>>, ListMilestonesByAgreementQueryHandler>();
        services.AddScoped<ICommandHandler<StoreEvidenceCommand, EvidenceResponse?>, StoreEvidenceCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitMilestoneCommand, bool>, SubmitMilestoneCommandHandler>();
        services.AddScoped<ICommandHandler<ReviewMilestoneCommand, bool>, ReviewMilestoneCommandHandler>();
        return services;
    }
}
