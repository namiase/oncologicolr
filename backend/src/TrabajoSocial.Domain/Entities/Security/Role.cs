using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;

namespace TrabajoSocial.Domain.Entities.Security;

/// <summary>Perfil de usuario.</summary>
public class Role : BaseEntity, IHasStatus
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid StatusId { get; set; } = StatusIds.Active;
    public Status Status { get; set; } = null!;

    public ICollection<User> Users { get; set; } = [];
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
