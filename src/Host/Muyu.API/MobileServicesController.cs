using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muyu.Modules.IAM.Domain;
using Muyu.Shared.Infrastructure;

namespace Muyu.API;

public sealed record CreateMobileIncidentRequest(string? Type, string? Description);

public sealed record MobileIncidentResponse(
    int Id,
    string Type,
    string Description,
    DateTimeOffset CreatedAtUtc);

public sealed record FinishedContractResponse(
    int PlotId,
    string PlotName,
    string BuyerEmail,
    string BuyerName,
    string EndedOn);

/// <summary>
/// Incident reports and finished contracts for the mobile clients.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/mobile")]
public sealed class MobileServicesController(MuyuDbContext dbContext) : ControllerBase
{
    private const int MaxDescriptionLength = 2000;

    private static readonly HashSet<string> IncidentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "technical", "payment", "plot", "other"
    };

    [HttpPost("incidents")]
    [ProducesResponseType<MobileIncidentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIncident(
        [FromBody] CreateMobileIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var type = request.Type?.Trim().ToLowerInvariant();
        var description = request.Description?.Trim();
        if (type is null || !IncidentTypes.Contains(type))
        {
            return BadRequest("The incident type is not valid.");
        }

        if (string.IsNullOrEmpty(description) || description.Length > MaxDescriptionLength)
        {
            return BadRequest($"The description must have between 1 and {MaxDescriptionLength} characters.");
        }

        var incident = new MobileIncidentData
        {
            UserId = GetUserId(),
            Type = type,
            Description = description,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.MobileIncidents.Add(incident);
        await dbContext.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ToResponse(incident));
    }

    [HttpGet("incidents")]
    [ProducesResponseType<IReadOnlyList<MobileIncidentResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncidents(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var incidents = await dbContext.MobileIncidents.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        return Ok(incidents.Select(ToResponse));
    }

    /// <summary>
    /// The caller's funded plots whose harvest the merchant has already received.
    /// </summary>
    [HttpGet("contracts/finished")]
    [ProducesResponseType<IReadOnlyList<FinishedContractResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinishedContracts(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var plots = await dbContext.MobilePlots.AsNoTracking()
            .Where(item => item.OwnerId == userId && item.FundedByEmail != null)
            .OrderByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var finished = plots
            .Select(plot => (Plot: plot, Payload: JsonDocument.Parse(plot.PayloadJson).RootElement))
            .Where(item => !string.IsNullOrWhiteSpace(ReadText(item.Payload, "harvestReceivedAt")))
            .ToList();

        var buyerEmails = finished.Select(item => item.Plot.FundedByEmail!.ToLowerInvariant()).Distinct().ToList();
        var buyerNames = await dbContext.Set<User>().AsNoTracking()
            .Where(user => buyerEmails.Contains(user.Email))
            .ToDictionaryAsync(user => user.Email, user => user.DisplayName, cancellationToken);

        return Ok(finished.Select(item => new FinishedContractResponse(
            item.Plot.Id,
            ReadText(item.Payload, "name"),
            item.Plot.FundedByEmail!,
            buyerNames.GetValueOrDefault(item.Plot.FundedByEmail!.ToLowerInvariant(), string.Empty),
            ReadText(item.Payload, "harvestReceivedAt"))));
    }

    private static MobileIncidentResponse ToResponse(MobileIncidentData incident) => new(
        incident.Id,
        incident.Type,
        incident.Description,
        incident.CreatedAtUtc);

    private static string ReadText(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private Guid GetUserId() => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("The JWT does not contain a user identifier."));
}
