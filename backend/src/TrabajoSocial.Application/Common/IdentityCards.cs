using System.Text.RegularExpressions;

namespace TrabajoSocial.Application.Common;

/// <summary>Cédulas de identidad venezolanas (V = venezolano, E = extranjero).</summary>
public static partial class IdentityCards
{
    public const string FormatError = "Formato de cédula inválido. Ejemplo: V-12345678.";

    /// <summary>Normaliza la cédula a "V-12345678" / "E-12345678". Devuelve null si el formato no es válido.</summary>
    public static string? Normalize(string value)
    {
        var compact = value.Trim().ToUpperInvariant().Replace(".", "").Replace("-", "").Replace(" ", "");
        var match = IdentityCardRegex().Match(compact);
        return match.Success ? $"{match.Groups[1].Value}-{match.Groups[2].Value}" : null;
    }

    [GeneratedRegex(@"^([VE])(\d{5,9})$")]
    private static partial Regex IdentityCardRegex();
}
