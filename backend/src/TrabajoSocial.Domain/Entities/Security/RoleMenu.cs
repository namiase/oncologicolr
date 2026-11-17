using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Domain.Entities.Security;

/// <summary>Permisos de un perfil sobre una opción de menú.</summary>
public class RoleMenu : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid MenuId { get; set; }
    public Menu Menu { get; set; } = null!;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }

    public bool Allows(PermissionAction action) => action switch
    {
        PermissionAction.View => CanView,
        PermissionAction.Create => CanView && CanCreate,
        PermissionAction.Update => CanView && CanUpdate,
        PermissionAction.Delete => CanView && CanDelete,
        _ => false
    };
}
