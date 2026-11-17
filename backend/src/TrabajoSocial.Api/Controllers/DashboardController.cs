using Microsoft.AspNetCore.Mvc;
using TrabajoSocial.Api.Authorization;
using TrabajoSocial.Application.Dashboard;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet("summary")]
    [HasPermission(MenuCodes.Home, PermissionAction.View)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
        => Ok(await dashboard.GetSummaryAsync(ct));
}
