using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muyu.Shared.Infrastructure;

namespace Muyu.API;

/// <summary>
/// Persists the authenticated mobile user's application state on the server.
/// The Android client uses this endpoint instead of files or SharedPreferences.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/mobile/state")]
public sealed class MobileAppStateController(MuyuDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var payload = await dbContext.MobileAppStates
            .AsNoTracking()
            .Where(state => state.UserId == userId)
            .Select(state => state.PayloadJson)
            .SingleOrDefaultAsync(cancellationToken);

        return payload is null
            ? NoContent()
            : Content(payload, "application/json");
    }

    [HttpPut]
    public async Task<IActionResult> Put(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var state = await dbContext.MobileAppStates
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (state is null)
        {
            state = new MobileAppState { UserId = userId };
            dbContext.MobileAppStates.Add(state);
        }

        state.PayloadJson = payload.GetRawText();
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var identifier = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(identifier, out var userId)
            ? userId
            : throw new InvalidOperationException("The JWT does not contain a valid user identifier.");
    }
}
