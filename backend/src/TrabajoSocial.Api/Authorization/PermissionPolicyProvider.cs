using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TrabajoSocial.Api.Authorization;

/// <summary>Crea al vuelo las políticas "perm:{menu}:{acción}" usadas por <see cref="HasPermissionAttribute"/>.</summary>
internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (PermissionPolicy.TryParse(policyName, out var requirement))
        {
            return new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .AddRequirements(requirement)
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}
