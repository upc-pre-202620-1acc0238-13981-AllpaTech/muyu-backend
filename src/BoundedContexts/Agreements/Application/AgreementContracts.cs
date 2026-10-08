using System.ComponentModel.DataAnnotations;
using Muyu.Modules.Agreements.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.Agreements.Application;

public sealed record DisbursementPlanInput(
    [Range(0.01, double.MaxValue)] decimal Amount,
    DateOnly ScheduledDate);

public sealed record CreateAgreementCommand(
    Guid ParcelId,
    Guid ProducerId,
    [Range(0.01, double.MaxValue)] decimal PrincipalAmount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    DateOnly StartsOn,
    DateOnly EndsOn,
    [Required, MinLength(1)] IReadOnlyList<DisbursementPlanInput> Disbursements)
    : ICommand<AgreementResponse>;

public sealed record GetAgreementByIdQuery(Guid AgreementId) : IQuery<AgreementResponse?>;

public sealed record ListAgreementsByProducerQuery(Guid ProducerId)
    : IQuery<IReadOnlyList<AgreementResponse>>;

public sealed record ActivateAgreementCommand(Guid AgreementId) : ICommand<bool>;

public sealed record DisbursementPlanResponse(
    int Sequence,
    decimal Amount,
    DateOnly ScheduledDate,
    DisbursementPlanStatus Status,
    DateTimeOffset? ReleasedAtUtc);

public sealed record AgreementResponse(
    Guid Id,
    Guid ParcelId,
    Guid ProducerId,
    decimal PrincipalAmount,
    string Currency,
    DateOnly StartsOn,
    DateOnly EndsOn,
    AgreementStatus Status,
    IReadOnlyList<DisbursementPlanResponse> Disbursements);
