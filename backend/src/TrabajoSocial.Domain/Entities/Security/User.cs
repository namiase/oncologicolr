using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.SocialWorkers;

namespace TrabajoSocial.Domain.Entities.Security;

/// <summary>Usuario del sistema.</summary>
public class User : BaseEntity, IHasStatus
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid StatusId { get; set; } = StatusIds.Active;
    public Status Status { get; set; } = null!;

    /// <summary>
    /// La contraseña la asignó un administrador (alta o restablecimiento): el usuario debe
    /// cambiarla al iniciar sesión antes de usar el sistema.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// Administrador principal: el que creó el sistema (lo marca el seeder; hay uno solo).
    /// Nadie más puede modificarlo, eliminarlo ni cambiarle la contraseña.
    /// </summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>
    /// Autorizado por el administrador principal para crear administradores y gestionar sus
    /// cuentas. Solo tiene efecto con el perfil Administrador.
    /// </summary>
    public bool CanManageAdministrators { get; set; }

    // Los actualiza el login: no cuentan como edición del usuario en la auditoría.
    [NotAudited] public DateTime? LastLoginAt { get; set; }
    [NotAudited] public int AccessFailedCount { get; set; }
    [NotAudited] public DateTime? LockoutEnd { get; set; }

    /// <summary>Trabajadora social vinculada (opcional).</summary>
    public SocialWorker? SocialWorker { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserSession> Sessions { get; set; } = [];

    public bool IsLockedOut(DateTime utcNow) => LockoutEnd.HasValue && LockoutEnd.Value > utcNow;
}
