using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Catalogs;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Domain.Entities.SocialWorkers;

namespace TrabajoSocial.Application.Dashboard;

public sealed record DashboardSummaryDto(
    int ActiveSocialWorkers,
    int ActiveUsers,
    int MasterTables,
    int MasterTableRecords,
    int Reports);

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}

public sealed class DashboardService(
    IRepository<SocialWorker> socialWorkers,
    IRepository<User> users,
    ICatalogServiceFactory catalogs) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var activeSocialWorkers = await socialWorkers.Query().CountAsync(w => w.StatusId == StatusIds.Active, ct);
        var activeUsers = await users.Query().CountAsync(u => u.StatusId == StatusIds.Active, ct);

        var records = 0;
        foreach (var definition in CatalogRegistry.All)
        {
            records += await catalogs.Get(definition.Key).CountAsync(ct);
        }

        // Reports = 0 hasta que exista el módulo de informes.
        return new DashboardSummaryDto(activeSocialWorkers, activeUsers, CatalogRegistry.All.Count, records, 0);
    }
}
