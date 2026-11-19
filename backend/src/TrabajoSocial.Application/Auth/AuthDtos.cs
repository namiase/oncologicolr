using System.ComponentModel.DataAnnotations;

namespace TrabajoSocial.Application.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta pública de login/refresh. El refresh token viaja aparte, en cookie HttpOnly.
/// ExpiresInSeconds es relativo (inmune a diferencias de reloj entre cliente y servidor) e
/// IdleTimeoutMinutes indica al cliente cuándo cerrar la sesión por inactividad.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    int ExpiresInSeconds,
    int IdleTimeoutMinutes,
    CurrentUserDto User);

/// <summary>Resultado interno de autenticación (incluye el refresh token para la cookie).</summary>
public sealed record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    int AccessTokenExpiresInSeconds,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    int IdleTimeoutMinutes,
    CurrentUserDto User)
{
    public AuthResponse ToResponse()
        => new(AccessToken, AccessTokenExpiresAt, AccessTokenExpiresInSeconds, IdleTimeoutMinutes, User);
}

public sealed record CurrentUserDto(
    Guid Id,
    string Username,
    string FullName,
    string? Email,
    Guid RoleId,
    string RoleName,
    bool MustChangePassword,
    // Es el administrador principal (el que creó el sistema).
    bool IsSuperAdmin,
    // Puede crear administradores y gestionar sus cuentas (el principal o un autorizado por él).
    bool CanManageAdministrators,
    IReadOnlyList<MenuItemDto> Menus);

public sealed record MenuPermissionsDto(bool CanCreate, bool CanUpdate, bool CanDelete);

public sealed record MenuItemDto(
    Guid Id,
    string Code,
    string Title,
    string? Path,
    string? Icon,
    int SortOrder,
    MenuPermissionsDto Permissions,
    IReadOnlyList<MenuItemDto> Children);
