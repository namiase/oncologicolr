using Microsoft.AspNetCore.Authorization;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Api.Authorization;

/// <summary>
/// Exige un permiso RoleMenu sobre una opción de menú, p. ej.
/// <c>[HasPermission(MenuCodes.MasterTables, PermissionAction.Create)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string menuCode, PermissionAction action)
    : AuthorizeAttribute(PermissionPolicy.Name(menuCode, action));

internal static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string Name(string menuCode, PermissionAction action) => $"{Prefix}{menuCode}:{action}";

    public static bool TryParse(string policyName, out PermissionRequirement requirement)
    {
        requirement = null!;
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = policyName[Prefix.Length..].Split(':');
        if (parts.Length != 2 || !Enum.TryParse<PermissionAction>(parts[1], out var action))
        {
            return false;
        }

        requirement = new PermissionRequirement(parts[0], action);
        return true;
    }
}

internal sealed record PermissionRequirement(string MenuCode, PermissionAction Action) : IAuthorizationRequirement;
