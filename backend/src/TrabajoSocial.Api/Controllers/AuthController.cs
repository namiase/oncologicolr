using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrabajoSocial.Api.Infrastructure;
using TrabajoSocial.Application.Auth;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Security;
using TrabajoSocial.Application.Users;

namespace TrabajoSocial.Api.Controllers;

/// <summary>
/// Autenticación con JWT. El access token (15 min) se devuelve en el cuerpo y el cliente lo guarda
/// en memoria; el refresh token viaja en una cookie HttpOnly + Secure + SameSite=Strict, rota en cada
/// uso y vence tras 30 min sin renovarse (cierre por inactividad).
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Inicia sesión con usuario y contraseña.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var client = new ClientInfo(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        var result = await authService.LoginAsync(request, client, ct);
        RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result.ToResponse());
    }

    /// <summary>Renueva el access token usando la cookie de refresh (y la rota).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        try
        {
            var result = await authService.RefreshAsync(RefreshTokenCookie.Read(Request) ?? string.Empty, ct);
            RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt);
            return Ok(result.ToResponse());
        }
        catch (AuthenticationFailedException ex)
        {
            // Se responde aquí (no en el handler global) para poder borrar la cookie inválida.
            RefreshTokenCookie.Delete(Response);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "No autorizado", detail: ex.Message);
        }
    }

    /// <summary>
    /// Cierra la sesión: revoca el refresh token, invalida de inmediato el access token
    /// (claim "sid") y borra la cookie. Funciona aunque el access token ya haya vencido.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Guid? sessionId = Guid.TryParse(User.FindFirstValue(AppClaimTypes.SessionId), out var sid) ? sid : null;
        await authService.LogoutAsync(RefreshTokenCookie.Read(Request), sessionId, ct);
        RefreshTokenCookie.Delete(Response);
        return NoContent();
    }

    /// <summary>
    /// Cambia la contraseña del usuario autenticado. Mantiene la sesión actual y cierra
    /// las abiertas en otros equipos.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordDto dto, [FromServices] IUserService users, [FromServices] ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException("Sesión inválida.");
        Guid? sessionId = Guid.TryParse(User.FindFirstValue(AppClaimTypes.SessionId), out var sid) ? sid : null;
        await users.ChangeOwnPasswordAsync(userId, sessionId, dto, ct);
        return NoContent();
    }

    /// <summary>Datos del usuario autenticado y su menú.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me([FromServices] ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException("Sesión inválida.");
        return Ok(await authService.GetCurrentUserAsync(userId, ct));
    }
}
