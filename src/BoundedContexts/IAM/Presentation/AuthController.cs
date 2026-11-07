using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Muyu.Modules.IAM.Application;
using Muyu.Shared.Application;

namespace Muyu.Modules.IAM.Presentation;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(
    ICommandHandler<LoginCommand, LoginResponse?> loginHandler,
    ICommandHandler<RegisterAgricultorCommand, UserProfileResponse> registerAgricultorHandler,
    ICommandHandler<RegisterComercianteCommand, UserProfileResponse> registerComercianteHandler,
    ICommandHandler<RegisterMobileAccountCommand, UserProfileResponse> registerMobileHandler,
    ChangePasswordCommandHandler changePasswordHandler) : ControllerBase
{
    /// <summary>Registra una cuenta con el rol Agricultor y valida su DNI.</summary>
    [AllowAnonymous]
    [HttpPost("register/agricultor")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<UserProfileResponse>> RegisterAgricultor(
        [FromBody] RegisterAgricultorCommand command,
        CancellationToken cancellationToken) =>
        RegisterAsync(
            () => registerAgricultorHandler.HandleAsync(command, cancellationToken));

    /// <summary>Registra una cuenta con el rol Comerciante y valida su RUC.</summary>
    [AllowAnonymous]
    [HttpPost("register/comerciante")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<UserProfileResponse>> RegisterComerciante(
        [FromBody] RegisterComercianteCommand command,
        CancellationToken cancellationToken) =>
        RegisterAsync(
            () => registerComercianteHandler.HandleAsync(command, cancellationToken));

    /// <summary>Registers one verified account for both mobile experiences.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<UserProfileResponse>> RegisterMobile(
        [FromBody] RegisterMobileAccountCommand command,
        CancellationToken cancellationToken) =>
        RegisterAsync(() => registerMobileHandler.HandleAsync(command, cancellationToken));

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

    /// <summary>Changes the password after verifying the authenticated user's current password.</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var changed = await changePasswordHandler.HandleForUserAsync(userId, command, cancellationToken);
        if (!changed)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No se pudo cambiar la contraseña",
                Detail = "La contraseña actual no es válida.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    private async Task<ActionResult<UserProfileResponse>> RegisterAsync(
        Func<Task<UserProfileResponse>> registration)
    {
        try
        {
            var response = await registration();
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (UserRegistrationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "No se pudo registrar la cuenta",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
