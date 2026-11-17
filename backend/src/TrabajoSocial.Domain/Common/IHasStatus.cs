using TrabajoSocial.Domain.Entities.Catalogs;

namespace TrabajoSocial.Domain.Common;

/// <summary>
/// Entidades con estatus de negocio (Activo / Inactivo / Eliminado).
/// Al eliminar lógicamente, el interceptor asigna el estatus Eliminado.
/// </summary>
public interface IHasStatus
{
    Guid StatusId { get; set; }
    Status Status { get; set; }
}
