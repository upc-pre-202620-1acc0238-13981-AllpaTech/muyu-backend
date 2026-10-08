using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.IAM.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Presentation;

[ApiController]
[Authorize]
[Route("api/users")]
[Route("api/v1/users")]
[Produces("application/json")]
public sealed class UsersController(
    IQueryHandler<GetUserProfileQuery, UserProfileResponse?> profileHandler) : ControllerBase
{
    /// <summary>Obtiene el perfil de la cuenta autenticada.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var identifier = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(identifier, out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Token inválido",
                Detail = "El token no contiene un identificador de usuario válido.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var profile = await profileHandler.HandleAsync(
            new GetUserProfileQuery(userId),
            cancellationToken);

        return profile is null
            ? NotFound(new ProblemDetails
            {
                Title = "Usuario no encontrado",
                Status = StatusCodes.Status404NotFound
            })
            : Ok(profile);
    }
}
