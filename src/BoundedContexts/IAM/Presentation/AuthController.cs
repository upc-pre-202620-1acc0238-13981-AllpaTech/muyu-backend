using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Muyu.Modules.IAM.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Presentation;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    ICommandHandler<LoginCommand, LoginResponse?> loginHandler) : ControllerBase
{
    /// <summary>Autentica un usuario y emite un access token JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var response = await loginHandler.HandleAsync(command, cancellationToken);

        return response is not null
            ? Ok(response)
            : Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas",
                Detail = "El correo o la contraseña no son válidos.",
                Status = StatusCodes.Status401Unauthorized
            });
    }
}
