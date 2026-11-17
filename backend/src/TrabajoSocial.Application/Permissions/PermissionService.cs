using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Permissions;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid roleId, string menuCode, PermissionAction action, CancellationToken ct = default);

    /// <summary>Invalida la caché de permisos de un perfil (llamar al modificar RoleMenu).</summary>
    void Invalidate(Guid roleId);
}

/// <summary>Evalúa permisos RoleMenu con caché en memoria por perfil (5 minutos).</summary>
public sealed class PermissionService(IRepository<RoleMenu> roleMenus, IMemoryCache cache) : IPermissionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<bool> HasPermissionAsync(
        Guid roleId, string menuCode, PermissionAction action, CancellationToken ct = default)
    {
        var permissions = await cache.GetOrCreateAsync(CacheKey(roleId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var rows = await roleMenus.Query()
                .Where(rm => rm.RoleId == roleId
                             && rm.Role.StatusId == StatusIds.Active
                             && rm.Menu.StatusId == StatusIds.Active)
                .Select(rm => new { rm.Menu.Code, Entry = rm })
                .ToListAsync(ct);

            return rows.ToDictionary(r => r.Code, r => r.Entry, StringComparer.OrdinalIgnoreCase);
        });

        return permissions is not null
               && permissions.TryGetValue(menuCode, out var roleMenu)
               && roleMenu.Allows(action);
    }

    public void Invalidate(Guid roleId) => cache.Remove(CacheKey(roleId));

    private static string CacheKey(Guid roleId) => $"permissions:{roleId}";
}
