using Muyu.Modules.Agreements.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.Agreements.Application;

public sealed class CreateAgreementCommandHandler(IAgreementRepository repository)
    : ICommandHandler<CreateAgreementCommand, AgreementResponse>
{
    public async Task<AgreementResponse> HandleAsync(
        CreateAgreementCommand command,
        CancellationToken cancellationToken = default)
    {
        var agreement = Agreement.Create(
            command.ParcelId,
            command.ProducerId,
            command.PrincipalAmount,
            command.Currency,
            command.StartsOn,
            command.EndsOn,
            command.Disbursements.Select(item => (item.Amount, item.ScheduledDate)));

        await repository.AddAsync(agreement, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return AgreementMapping.Map(agreement);
    }
}

public sealed class GetAgreementByIdQueryHandler(IAgreementRepository repository)
    : IQueryHandler<GetAgreementByIdQuery, AgreementResponse?>
{
    public async Task<AgreementResponse?> HandleAsync(
        GetAgreementByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var agreement = await repository.GetByIdReadOnlyAsync(query.AgreementId, cancellationToken);
        return agreement is null ? null : AgreementMapping.Map(agreement);
    }
}

public sealed class ListAgreementsByProducerQueryHandler(IAgreementRepository repository)
    : IQueryHandler<ListAgreementsByProducerQuery, IReadOnlyList<AgreementResponse>>
{
    public async Task<IReadOnlyList<AgreementResponse>> HandleAsync(
        ListAgreementsByProducerQuery query,
        CancellationToken cancellationToken = default) =>
        (await repository.ListByProducerAsync(query.ProducerId, cancellationToken))
            .Select(AgreementMapping.Map)
            .ToArray();
}

public sealed class ActivateAgreementCommandHandler(IAgreementRepository repository)
    : ICommandHandler<ActivateAgreementCommand, bool>
{
    public async Task<bool> HandleAsync(
        ActivateAgreementCommand command,
        CancellationToken cancellationToken = default)
    {
        var agreement = await repository.GetByIdAsync(command.AgreementId, cancellationToken);
        if (agreement is null)
        {
            return false;
        }

        agreement.Activate();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class AgreementMapping
{
    internal static AgreementResponse Map(Agreement agreement) =>
        new(
            agreement.Id,
            agreement.ParcelId,
            agreement.ProducerId,
            agreement.PrincipalAmount,
            agreement.Currency,
            agreement.StartsOn,
            agreement.EndsOn,
            agreement.Status,
            agreement.DisbursementPlans
                .OrderBy(item => item.Sequence)
                .Select(item => new DisbursementPlanResponse(
                    item.Sequence,
                    item.Amount,
                    item.ScheduledDate,
                    item.Status,
                    item.ReleasedAtUtc))
                .ToArray());
}
