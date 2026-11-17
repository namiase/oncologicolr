using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Domain.Entities.Security;

/// <summary>
/// Sesión de un usuario (un inicio de sesión en un navegador). Agrupa los refresh tokens
/// que se van rotando y su Id viaja en el access token (claim "sid"), lo que permite
/// invalidar el token al cerrar sesión.
/// </summary>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Vencimiento absoluto: pasado este momento hay que iniciar sesión de nuevo.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Última renovación de la sesión (marca de actividad del usuario).</summary>
    public DateTime LastActivityAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    /// <summary>Motivo del cierre (ver <see cref="SessionEndReasons"/>).</summary>
    public string? RevokedReason { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

/// <summary>Motivos de cierre de una sesión.</summary>
public static class SessionEndReasons
{
    public const string Logout = "Logout";
    public const string Idle = "Idle";
    public const string Expired = "Expired";
    public const string TokenReuse = "TokenReuse";
    public const string UserDisabled = "UserDisabled";
    public const string PasswordChanged = "PasswordChanged";
    public const string AccountChanged = "AccountChanged";
    public const string UserDeleted = "UserDeleted";
}
