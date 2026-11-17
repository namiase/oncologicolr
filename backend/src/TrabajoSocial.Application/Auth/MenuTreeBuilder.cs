using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Auth;

/// <summary>Construye el árbol de menús visibles para un perfil a partir de RoleMenu.</summary>
public sealed class MenuTreeBuilder(IRepository<RoleMenu> roleMenus)
{
    public async Task<IReadOnlyList<MenuItemDto>> BuildAsync(Guid roleId, CancellationToken ct = default)
    {
        var allowed = await roleMenus.Query()
            .Where(rm => rm.RoleId == roleId && rm.CanView && rm.Menu.StatusId == StatusIds.Active)
            .Select(rm => new
            {
                rm.Menu.Id,
                rm.Menu.Code,
                rm.Menu.Title,
                rm.Menu.Path,
                rm.Menu.Icon,
                rm.Menu.SortOrder,
                rm.Menu.ParentId,
                rm.CanCreate,
                rm.CanUpdate,
                rm.CanDelete
            })
            .ToListAsync(ct);

        var byParent = allowed.ToLookup(m => m.ParentId);

        List<MenuItemDto> Build(Guid? parentId) => byParent[parentId]
            .OrderBy(m => m.SortOrder)
            .Select(m => new MenuItemDto(
                m.Id, m.Code, m.Title, m.Path, m.Icon, m.SortOrder,
                new MenuPermissionsDto(m.CanCreate, m.CanUpdate, m.CanDelete),
                Build(m.Id)))
            // Un menú agrupador (sin ruta) sin hijos visibles no se muestra.
            .Where(m => m.Path is not null || m.Children.Count > 0)
            .ToList();

        return Build(null);
    }
}
