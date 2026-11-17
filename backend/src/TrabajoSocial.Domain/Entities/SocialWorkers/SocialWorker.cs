using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Domain.Entities.SocialWorkers;

/// <summary>Trabajadora social.</summary>
public class SocialWorker : BaseEntity, IHasStatus
{
    /// <summary>Nombres.</summary>
    public string FirstNames { get; set; } = string.Empty;

    /// <summary>Apellidos.</summary>
    public string LastNames { get; set; } = string.Empty;

    /// <summary>Cédula de identidad, formato V-12345678 / E-12345678.</summary>
    public string IdentityCard { get; set; } = string.Empty;

    /// <summary>N° de registro ANTS.</summary>
    public string AntsRegistrationNumber { get; set; } = string.Empty;

    /// <summary>Colegio de Trabajadores Sociales (texto libre).</summary>
    public string SocialWorkersAssociation { get; set; } = string.Empty;

    /// <summary>Año de grado.</summary>
    public short GraduationYear { get; set; }

    /// <summary>Usuario del sistema vinculado (opcional).</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid StatusId { get; set; } = StatusIds.Active;
    public Status Status { get; set; } = null!;

    public string FullName => $"{FirstNames} {LastNames}".Trim();
}
