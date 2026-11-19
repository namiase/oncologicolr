using System.ComponentModel.DataAnnotations;
using TrabajoSocial.Application.Common.Models;

namespace TrabajoSocial.Application.Users;

public sealed record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public Guid StatusId { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime? LastLoginAt { get; init; }
    public bool IsLockedOut { get; init; }

    /// <summary>Tiene una contraseña asignada por un administrador que aún no cambió.</summary>
    public bool MustChangePassword { get; init; }

    /// <summary>Tiene el perfil Administrador.</summary>
    public bool IsAdministrator { get; init; }

    /// <summary>Es el administrador principal (el que creó el sistema).</summary>
    public bool IsSuperAdmin { get; init; }

    /// <summary>El principal lo autorizó para crear administradores y gestionar sus cuentas.</summary>
    public bool CanManageAdministrators { get; init; }

    public Guid? SocialWorkerId { get; init; }
    public string? SocialWorkerName { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class UserCreateDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre de usuario admite máximo {1} caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre completo admite máximo {1} caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [MaxLength(150, ErrorMessage = "El correo admite máximo {1} caracteres.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "El perfil es obligatorio.")]
    public Guid RoleId { get; set; }

    [Required(ErrorMessage = "El estatus es obligatorio.")]
    public Guid StatusId { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(PasswordPolicy.MaxLength, ErrorMessage = PasswordTooLong)]
    public string Password { get; set; } = string.Empty;

    internal const string PasswordTooLong = "La contraseña admite máximo {1} caracteres.";

    /// <summary>Trabajadora social vinculada a la cuenta (opcional).</summary>
    public Guid? SocialWorkerId { get; set; }
}

public sealed class UserUpdateDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre completo admite máximo {1} caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [MaxLength(150, ErrorMessage = "El correo admite máximo {1} caracteres.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "El perfil es obligatorio.")]
    public Guid RoleId { get; set; }

    [Required(ErrorMessage = "El estatus es obligatorio.")]
    public Guid StatusId { get; set; }

    public Guid? SocialWorkerId { get; set; }
}

public sealed class ResetPasswordDto
{
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MaxLength(PasswordPolicy.MaxLength, ErrorMessage = UserCreateDto.PasswordTooLong)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class AdminDelegationDto
{
    /// <summary>true: puede crear administradores y gestionar sus cuentas.</summary>
    public bool Enabled { get; set; }
}

public sealed class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [MaxLength(PasswordPolicy.MaxLength, ErrorMessage = UserCreateDto.PasswordTooLong)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MaxLength(PasswordPolicy.MaxLength, ErrorMessage = UserCreateDto.PasswordTooLong)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class UserQuery : PagedQuery
{
    public Guid? RoleId { get; set; }
}
