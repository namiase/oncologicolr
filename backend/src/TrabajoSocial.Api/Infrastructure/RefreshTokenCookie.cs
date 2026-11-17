namespace TrabajoSocial.Api.Infrastructure;

/// <summary>Cookie HttpOnly que transporta el refresh token (inaccesible desde JavaScript).</summary>
internal static class RefreshTokenCookie
{
    public const string Name = "ts_refresh";
    private const string CookiePath = "/api/auth";

    public static void Append(HttpResponse response, string token, DateTime expiresAt)
        => response.Cookies.Append(Name, token, Options(expiresAt));

    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, Options(null));

    public static string? Read(HttpRequest request)
        => request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static CookieOptions Options(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expiresAt,
        IsEssential = true
    };
}
