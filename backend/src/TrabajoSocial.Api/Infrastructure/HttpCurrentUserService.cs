using System.Security.Claims;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Security;

namespace TrabajoSocial.Api.Infrastructure;

internal sealed class HttpCurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal =>
        accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user ? user : null;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(AppClaimTypes.UserId), out var id) ? id : null;

    public string? Username => Principal?.FindFirstValue(AppClaimTypes.Username);

    public Guid? RoleId => Guid.TryParse(Principal?.FindFirstValue(AppClaimTypes.RoleId), out var id) ? id : null;
}
