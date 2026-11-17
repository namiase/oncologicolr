using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Auth;

/// <summary>
/// Lista de sesiones cerradas cuyos access tokens aún no vencen. Se consulta en CADA
/// petición autenticada, por eso vive en memoria (sin ir a la base de datos).
/// Con varias instancias del API, reemplazar por una implementación distribuida (Redis).
/// </summary>
public interface ISessionRevocationStore
{
    void Revoke(Guid sessionId, DateTime untilUtc);
    bool IsRevoked(Guid sessionId);
}

public sealed class MemorySessionRevocationStore(IMemoryCache cache, TimeProvider time) : ISessionRevocationStore
{
    public void Revoke(Guid sessionId, DateTime untilUtc)
    {
        var ttl = untilUtc - time.GetUtcNow().UtcDateTime;
        if (ttl > TimeSpan.Zero)
        {
            cache.Set(Key(sessionId), true, ttl);
        }
    }

    public bool IsRevoked(Guid sessionId) => cache.TryGetValue(Key(sessionId), out _);

    private static string Key(Guid sessionId) => $"revoked-session:{sessionId}";
}

/// <summary>
/// Al iniciar el API recarga en memoria las sesiones cerradas recientemente, para que
/// un reinicio no "reviva" tokens de sesiones ya cerradas.
/// </summary>
public sealed class SessionRevocationLoader(
    IRepository<UserSession> sessions,
    ISessionRevocationStore store,
    IJwtTokenService jwt,
    TimeProvider time)
{
    public async Task<int> LoadAsync(CancellationToken ct = default)
    {
        var since = time.GetUtcNow().UtcDateTime - jwt.RevocationWindow;
        var revoked = await sessions.Query()
            .Where(s => s.RevokedAt != null && s.RevokedAt > since)
            .Select(s => new { s.Id, RevokedAt = s.RevokedAt!.Value })
            .ToListAsync(ct);

        foreach (var session in revoked)
        {
            store.Revoke(session.Id, session.RevokedAt + jwt.RevocationWindow);
        }

        return revoked.Count;
    }
}
