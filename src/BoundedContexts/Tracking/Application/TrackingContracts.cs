using System.ComponentModel.DataAnnotations;
using Muyu.Modules.Tracking.Domain;
using Muyu.Shared.Application;

namespace Muyu.Modules.Tracking.Application;

public sealed record ScheduleMilestoneCommand(
    Guid AgreementId,
    Guid ParcelId,
    [Required, MaxLength(160)] string Title,
    DateOnly DueDate) : ICommand<MilestoneResponse>;

public sealed record StoreEvidenceCommand(
    Guid MilestoneId,
    string FileName,
    string ContentType,
    Stream Content,
    long SizeBytes,
    DateTimeOffset CapturedAtUtc,
    string? Note) : ICommand<EvidenceResponse?>;

public sealed record SubmitMilestoneCommand(Guid MilestoneId) : ICommand<bool>;

public sealed record ReviewMilestoneCommand(
    Guid MilestoneId,
    bool Approved,
    string? Comment) : ICommand<bool>;

public sealed record GetMilestoneByIdQuery(Guid MilestoneId) : IQuery<MilestoneResponse?>;

public sealed record ListMilestonesByAgreementQuery(
    Guid AgreementId) : IQuery<IReadOnlyList<MilestoneResponse>>;

public sealed record ReviewMilestoneRequest(bool Approved, string? Comment);

public sealed record EvidenceResponse(
    Guid Id,
    string FileName,
    string ContentType,
    string StorageUri,
    string Sha256,
    long SizeBytes,
    DateTimeOffset CapturedAtUtc,
    string? Note);

public sealed record MilestoneResponse(
    Guid Id,
    Guid AgreementId,
    Guid ParcelId,
    string Title,
    DateOnly DueDate,
    MilestoneStatus Status,
    string? ReviewComment,
    IReadOnlyList<EvidenceResponse> Evidences);
