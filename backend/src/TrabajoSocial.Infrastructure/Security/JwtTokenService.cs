using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Security;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time) : IJwtTokenService
{
    private readonly JwtOptions jwt = options.Value;
    private readonly JsonWebTokenHandler handler = new();

    /// <summary>Margen por un refresh que estuviera en curso justo cuando se cerró la sesión.</summary>
    private static readonly TimeSpan RefreshRaceMargin = TimeSpan.FromMinutes(1);

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(jwt.AccessTokenMinutes);

    public TimeSpan ClockSkew => JwtOptions.ClockSkew;

    public TimeSpan RevocationWindow => AccessTokenLifetime + ClockSkew + RefreshRaceMargin;

    public AccessToken CreateAccessToken(User user, Guid sessionId, DateTime notAfter)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(AccessTokenLifetime);
        if (notAfter < expiresAt)
        {
            expiresAt = notAfter;
        }

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id.ToString()),
            new(AppClaimTypes.Username, user.Username),
            new(AppClaimTypes.FullName, user.FullName),
            new(AppClaimTypes.Role, user.Role.Name),
            new(AppClaimTypes.RoleId, user.RoleId.ToString()),
            new(AppClaimTypes.SessionId, sessionId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (user.MustChangePassword)
        {
            claims.Add(new Claim(AppClaimTypes.MustChangePassword, "true"));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(jwt.GetSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(handler.CreateToken(descriptor), expiresAt);
    }

    public string GenerateRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
