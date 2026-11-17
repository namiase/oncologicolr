using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Auth;

/// <summary>
/// Cierra sesiones: revoca sus refresh tokens e invalida al instante sus access tokens
/// (lista de revocación en memoria). No guarda cambios: lo hace quien lo invoca, dentro
/// de su misma unidad de trabajo. La revocación en memoria se aplica solo cuando ese
/// guardado se confirma.
/// </summary>
public sealed class SessionTerminator(
    IRepository<UserSession> sessions,
    IRepository<RefreshToken> refreshTokens,
    ISessionRevocationStore revocations,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwt,
    TimeProvider time)
{
    public async Task EndAsync(UserSession session, string reason, DateTime now, CancellationToken ct = default)
    {
        var activeTokens = await refreshTokens.QueryTracked()
            .Where(t => t.SessionId == session.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        Close(session, reason, now);
    }

    /// <summary>Cierra todas las sesiones vigentes de un usuario, salvo <paramref name="exceptSessionId"/>.</summary>
    public async Task<int> EndAllForUserAsync(
        Guid userId, string reason, Guid? exceptSessionId = null, CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime;

        // Una sesión que venció hace más de lo que dura un access token ya no tiene ningún token
        // válido: no se toca (así no se reescribe el motivo de cierre de sesiones viejas).
        var mayHaveValidTokens = now - jwt.RevocationWindow;
        var active = await sessions.QueryTracked()
            .Where(s => s.UserId == userId
                        && s.RevokedAt == null
                        && s.ExpiresAt > mayHaveValidTokens
                        && s.Id != exceptSessionId)
            .ToListAsync(ct);
        if (active.Count == 0)
        {
            return 0;
        }

        var sessionIds = active.Select(s => s.Id).ToList();
        var activeTokens = await refreshTokens.QueryTracked()
            .Where(t => sessionIds.Contains(t.SessionId) && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        foreach (var session in active)
        {
            Close(session, reason, now);
        }

        return active.Count;
    }

    private void Close(UserSession session, string reason, DateTime now)
    {
        session.RevokedAt = now;
        session.RevokedReason = reason;

        var sessionId = session.Id;
        var until = now.Add(jwt.RevocationWindow);
        unitOfWork.AfterSaveChanges(() => revocations.Revoke(sessionId, until));
    }
}
