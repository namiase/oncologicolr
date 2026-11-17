namespace TrabajoSocial.Domain.Common;

/// <summary>
/// Clase base de todas las entidades persistidas. Los campos de auditoría y el
/// soft delete los gestiona automáticamente el interceptor de EF Core.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    /// <summary>Fecha de creación (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Usuario (username) que creó el registro.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Fecha de la última edición (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Usuario (username) que editó el registro por última vez.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Borrado lógico. Los registros eliminados se excluyen con un filtro global.</summary>
    public bool IsDeleted { get; set; }
}
