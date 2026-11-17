namespace TrabajoSocial.Application.Users;

/// <summary>Reglas mínimas de contraseña (las mismas se validan en el frontend).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>Devuelve el motivo por el que la contraseña no es válida, o null si cumple.</summary>
    public static string? Validate(string? password, string? username = null)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return $"La contraseña debe tener al menos {MinLength} caracteres.";
        }

        if (password.Length > MaxLength)
        {
            return $"La contraseña admite máximo {MaxLength} caracteres.";
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            return "La contraseña debe incluir al menos una letra y un número.";
        }

        if (!string.IsNullOrWhiteSpace(username) && password.Contains(username, StringComparison.OrdinalIgnoreCase))
        {
            return "La contraseña no puede contener el nombre de usuario.";
        }

        return null;
    }
}
