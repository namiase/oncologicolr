using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Domain.Entities.Security;

/// <summary>
/// Refresh token emitido a un usuario. Solo se guarda el hash SHA-256 del token;
/// el valor en claro únicamente viaja en la cookie HttpOnly. Vence a los 30 minutos
/// (ventana de inactividad) y se rota en cada uso.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid SessionId { get; set; }
    public UserSession Session { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
