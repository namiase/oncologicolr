using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TrabajoSocial.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TrabajoSocial.Api";
    public string Audience { get; set; } = "TrabajoSocial.Web";

    /// <summary>Clave HMAC (mínimo 32 bytes). Se configura con user-secrets o variables de entorno.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Vida del access token. Corta: al cerrar sesión se invalida, y si lo roban sirve poco tiempo.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Tolerancia de reloj al validar el vencimiento (la usa también la lista de revocación).</summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
