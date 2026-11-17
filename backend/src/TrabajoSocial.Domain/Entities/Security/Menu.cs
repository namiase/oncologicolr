using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;

namespace TrabajoSocial.Domain.Entities.Security;

/// <summary>Opción del menú de la aplicación (admite submenús).</summary>
public class Menu : BaseEntity, IHasStatus
{
    /// <summary>Código estable usado por los permisos (ver <see cref="MenuCodes"/>).</summary>
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Ruta del frontend. Nula para los menús que solo agrupan.</summary>
    public string? Path { get; set; }

    /// <summary>Nombre del icono (lucide) que muestra el frontend.</summary>
    public string? Icon { get; set; }
    public int SortOrder { get; set; }

    public Guid? ParentId { get; set; }
    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = [];

    public Guid StatusId { get; set; } = StatusIds.Active;
    public Status Status { get; set; } = null!;

    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
