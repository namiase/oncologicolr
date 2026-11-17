using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TrabajoSocial.Application.Common.Security;
using TrabajoSocial.Application.Permissions;

namespace TrabajoSocial.Api.Authorization;

internal sealed class PermissionHandler(IPermissionService permissions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (Guid.TryParse(context.User.FindFirstValue(AppClaimTypes.RoleId), out var roleId)
            && await permissions.HasPermissionAsync(roleId, requirement.MenuCode, requirement.Action))
        {
            context.Succeed(requirement);
        }
    }
}
