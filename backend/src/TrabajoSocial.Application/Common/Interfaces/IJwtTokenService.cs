using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Common.Interfaces;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    /// <summary>
    /// Access token de vida corta con el Id de la sesión (claim "sid"). Nunca vence después
    /// de <paramref name="notAfter"/> (el fin absoluto de la sesión).
    /// </summary>
    AccessToken CreateAccessToken(User user, Guid sessionId, DateTime notAfter);

    TimeSpan AccessTokenLifetime { get; }

    /// <summary>Tolerancia de reloj con la que el API sigue aceptando un token ya vencido.</summary>
    TimeSpan ClockSkew { get; }

    /// <summary>
    /// Cuánto debe seguir revocada una sesión cerrada "ahora": lo que un access token emitido
    /// en este instante puede seguir siendo aceptado (vida + tolerancia de reloj), más un margen
    /// por un refresh que estuviera en curso al cerrar la sesión.
    /// </summary>
    TimeSpan RevocationWindow { get; }

    /// <summary>Genera un refresh token aleatorio (valor en claro para la cookie).</summary>
    string GenerateRefreshToken();

    /// <summary>Hash SHA-256 del refresh token (lo único que se persiste).</summary>
    string HashRefreshToken(string refreshToken);
}
