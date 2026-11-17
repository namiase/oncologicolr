using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Domain.Entities.Catalogs;

/// <summary>Tabla de estatus con valores fijos: Activo, Inactivo, Eliminado.</summary>
public class Status : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
