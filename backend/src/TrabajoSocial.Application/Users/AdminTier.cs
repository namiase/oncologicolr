using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Users;

/// <summary>
/// Jerarquía para administrar cuentas de administrador: una cuenta de administrador solo la
/// gestiona alguien de un nivel superior. Las demás cuentas las gestiona cualquiera con permiso
/// sobre Usuarios.
/// </summary>
public enum AdminTier
{
    /// <summary>No es administrador.</summary>
    None = 0,

    /// <summary>Administrador normal: gestiona los usuarios que no son administradores.</summary>
    Administrator = 1,

    /// <summary>Autorizado por el principal: además crea y gestiona administradores normales.</summary>
    Delegate = 2,

    /// <summary>El que creó el sistema. Nadie más puede modificarlo, eliminarlo ni cambiarle la contraseña.</summary>
    Principal = 3
}

public static class AdminTiers
{
    public static AdminTier Of(bool isSuperAdmin, bool canManageAdministrators, bool isAdministrator)
        => isSuperAdmin ? AdminTier.Principal
            : !isAdministrator ? AdminTier.None
            : canManageAdministrators ? AdminTier.Delegate
            : AdminTier.Administrator;

    /// <summary>Requiere el perfil (<see cref="User.Role"/>) cargado.</summary>
    public static AdminTier Of(User user)
        => Of(user.IsSuperAdmin, user.CanManageAdministrators, user.Role.Name == RoleNames.Administrator);

    /// <summary>Crear administradores (o asignar ese perfil) requiere ser el principal o un autorizado.</summary>
    public static bool CanCreateAdministrators(this AdminTier tier) => tier >= AdminTier.Delegate;
}
