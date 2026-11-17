namespace TrabajoSocial.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, ClientInfo client, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Cierra la sesión indicada por el refresh token (cookie) y/o por el claim "sid" del
    /// access token, e invalida de inmediato los access tokens de esa sesión.
    /// </summary>
    Task LogoutAsync(string? refreshToken, Guid? sessionId, CancellationToken ct = default);

    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
