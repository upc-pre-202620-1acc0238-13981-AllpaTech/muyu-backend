using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muyu.Shared.Infrastructure;

namespace Muyu.API;

/// <summary>
/// Remote source of truth for the Android plot model while preserving field-level ownership.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/mobile")]
public sealed class MobileDataController(MuyuDbContext dbContext) : ControllerBase
{
    private static readonly HashSet<string> FarmerFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "crop", "area", "harvestDate", "place", "amount", "cycle", "milestones",
        "imageUri", "latitude", "longitude", "accuracyMeters", "publishedAt", "milestoneStates",
        "weatherMeasures", "visitOutcome", "visitDate", "agreedPrice", "harvestIncidents"
    };

    /// <summary>
    /// The owner reports cultivation progress, but funding belongs to the merchant:
    /// a client with stale data must not be able to undo it.
    /// </summary>
    private static readonly HashSet<string> OwnerProgressFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "stage", "progress"
    };

    private static readonly HashSet<string> MerchantFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "stage", "progress", "fundedByEmail", "milestoneStates", "visitOutcome",
        "visitDate", "agreedPrice", "harvestReceivedAt", "harvestReceivedBy", "harvestIncidents"
    };

    [HttpGet("plots")]
    public async Task<IActionResult> GetPlots(CancellationToken cancellationToken)
    {
        var payloads = await dbContext.MobilePlots.AsNoTracking()
            .OrderByDescending(item => item.Id)
            .Select(item => item.PayloadJson)
            .ToListAsync(cancellationToken);
        return Content($"[{string.Join(',', payloads)}]", "application/json");
    }

    [HttpPut("plots/{id:int}")]
    public async Task<IActionResult> PutPlot(int id, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var incoming = JsonNode.Parse(payload.GetRawText())?.AsObject()
            ?? throw new InvalidOperationException("Invalid plot payload.");
        var ownerEmail = ReadRequired(incoming, "ownerEmail");
        var callerEmail = GetEmail();
        var callerId = GetUserId();
        var existing = await dbContext.MobilePlots.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (existing is null)
        {
            if (!SameEmail(ownerEmail, callerEmail)) return Forbid();
            incoming["id"] = id;
            incoming["ownerEmail"] = callerEmail;
            incoming["fundedByEmail"] = null;
            existing = new MobilePlotData
            {
                Id = id,
                OwnerId = callerId,
                OwnerEmail = callerEmail,
                PayloadJson = incoming.ToJsonString(),
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.MobilePlots.Add(existing);
        }
        else
        {
            if (!SameEmail(ownerEmail, existing.OwnerEmail)) return BadRequest("The plot owner cannot change.");
            var stored = JsonNode.Parse(existing.PayloadJson)!.AsObject();
            if (User.IsInRole("ADMIN"))
            {
                MergeAllowed(stored, incoming, FarmerFields);
                MergeAllowed(stored, incoming, MerchantFields);
                existing.FundedByEmail = incoming["fundedByEmail"]?.GetValue<string>();
            }
            else if (existing.OwnerId == callerId)
            {
                MergeAllowed(stored, incoming, FarmerFields);
                MergeAllowed(stored, incoming, OwnerProgressFields);
            }
            else
            {
                var requestedFunder = incoming["fundedByEmail"]?.GetValue<string>();
                var canClaimAvailablePlot = string.IsNullOrWhiteSpace(existing.FundedByEmail)
                                            && SameEmail(requestedFunder, callerEmail);
                var isAssignedMerchant = SameEmail(existing.FundedByEmail, callerEmail);
                if (!canClaimAvailablePlot && !isAssignedMerchant) return Forbid();
                MergeAllowed(stored, incoming, MerchantFields);
                existing.FundedByEmail = callerEmail;
                stored["fundedByEmail"] = callerEmail;
            }
            existing.PayloadJson = stored.ToJsonString();
            existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("plots/{id:int}")]
    public async Task<IActionResult> DeletePlot(int id, CancellationToken cancellationToken)
    {
        var plot = await dbContext.MobilePlots.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plot is null) return NoContent();
        if (plot.OwnerId != GetUserId() && !User.IsInRole("ADMIN")) return Forbid();
        dbContext.MobilePlots.Remove(plot);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits(CancellationToken cancellationToken)
    {
        var email = GetEmail();
        var isAdmin = User.IsInRole("ADMIN");
        var payloads = await dbContext.MobileVisits.AsNoTracking()
            .Where(item => isAdmin || item.FarmerEmail == email || item.MerchantEmail == email)
            .OrderByDescending(item => item.Id)
            .Select(item => item.PayloadJson)
            .ToListAsync(cancellationToken);
        return Content($"[{string.Join(',', payloads)}]", "application/json");
    }

    [HttpPut("visits/{id:int}")]
    public async Task<IActionResult> PutVisit(int id, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var incoming = JsonNode.Parse(payload.GetRawText())!.AsObject();
        var farmerEmail = ReadRequired(incoming, "farmerEmail");
        var merchantEmail = ReadRequired(incoming, "merchantEmail");
        var callerEmail = GetEmail();
        var visit = await dbContext.MobileVisits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (visit is null)
        {
            if (!SameEmail(merchantEmail, callerEmail)) return Forbid();
            visit = new MobileVisitData { Id = id, FarmerEmail = farmerEmail, MerchantEmail = merchantEmail };
            dbContext.MobileVisits.Add(visit);
        }
        else if ((!SameEmail(visit.FarmerEmail, callerEmail) && !SameEmail(visit.MerchantEmail, callerEmail))
                 || !SameEmail(visit.FarmerEmail, farmerEmail) || !SameEmail(visit.MerchantEmail, merchantEmail))
        {
            return Forbid();
        }
        visit.PayloadJson = incoming.ToJsonString();
        visit.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
    private string GetEmail() => (User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "").Trim().ToLowerInvariant();
    private static string ReadRequired(JsonObject value, string name) => value[name]?.GetValue<string>()?.Trim()
        ?? throw new InvalidOperationException($"{name} is required.");
    private static bool SameEmail(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static void MergeAllowed(JsonObject target, JsonObject source, HashSet<string> allowed)
    {
        foreach (var field in allowed)
        {
            if (source.TryGetPropertyValue(field, out var value)) target[field] = value?.DeepClone();
        }
    }
}
