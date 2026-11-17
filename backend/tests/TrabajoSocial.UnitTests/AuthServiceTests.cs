using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using TrabajoSocial.Application.Auth;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.UnitTests.Support;

namespace TrabajoSocial.UnitTests;

public sealed class AuthServiceTests
{
    private static readonly ClientInfo Client = new("127.0.0.1", "xunit");

    private static Task<AuthResult> Login(TestHost host, string password)
        => host.RunAsync(sp => sp.GetRequiredService<IAuthService>()
            .LoginAsync(new LoginRequest { Username = "admin", Password = password }, Client));

    private static Task<AuthResult> Refresh(TestHost host, string token)
        => host.RunAsync(sp => sp.GetRequiredService<IAuthService>().RefreshAsync(token));

    private static Task<UserSession> SingleSession(TestHost host)
        => host.RunAsync(sp => sp.GetRequiredService<AppDbContext>().UserSessions.SingleAsync());

    private static bool IsRevoked(TestHost host, Guid sessionId)
        => host.RunAsync(sp => Task.FromResult(sp.GetRequiredService<ISessionRevocationStore>().IsRevoked(sessionId))).Result;

    [Fact]
    public async Task Login_creates_a_session_and_returns_tokens_and_menu_tree()
    {
        await using var host = await TestHost.CreateAsync();

        var result = await Login(host, TestHost.AdminPassword);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(15 * 60, result.AccessTokenExpiresInSeconds);
        Assert.Equal(30, result.IdleTimeoutMinutes);
        Assert.Equal(host.Time.Now.UtcDateTime.AddMinutes(30), result.RefreshTokenExpiresAt);
        Assert.Equal(RoleNames.Administrator, result.User.RoleName);
        Assert.Equal([MenuCodes.Home, MenuCodes.CreateReport, MenuCodes.Configuration, MenuCodes.Reports],
            result.User.Menus.Select(m => m.Code));

        var session = await SingleSession(host);
        Assert.Equal("127.0.0.1", session.IpAddress);
        Assert.Equal(host.Time.Now.UtcDateTime.AddHours(12), session.ExpiresAt);

        // El access token lleva el Id de la sesión.
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(result.AccessToken);
        Assert.Equal(session.Id.ToString(), jwt.GetClaim("sid").Value);

        await host.RunAsync(async sp =>
        {
            var stored = await sp.GetRequiredService<AppDbContext>().RefreshTokens.SingleAsync();
            Assert.NotEqual(result.RefreshToken, stored.TokenHash); // solo se guarda el hash
            Assert.Equal(session.Id, stored.SessionId);
        });
    }

    [Fact]
    public async Task Wrong_password_five_times_locks_the_account()
    {
        await using var host = await TestHost.CreateAsync();

        for (var i = 0; i < AuthService.MaxFailedAccessAttempts; i++)
        {
            var failed = await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(host, "incorrecta"));
            Assert.Equal(AuthService.InvalidCredentials, failed.Message);
        }

        var locked = await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(host, TestHost.AdminPassword));
        Assert.Contains("bloqueado", locked.Message);

        host.Time.Advance(AuthService.LockoutDuration + TimeSpan.FromSeconds(1));
        Assert.NotNull((await Login(host, TestHost.AdminPassword)).AccessToken);
    }

    [Fact]
    public async Task Unknown_user_gets_the_same_generic_message()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IAuthService>()
                .LoginAsync(new LoginRequest { Username = "nadie", Password = "x" }, Client)));
        Assert.Equal(AuthService.InvalidCredentials, ex.Message);
    }

    [Fact]
    public async Task Inactive_user_cannot_log_in()
    {
        await using var host = await TestHost.CreateAsync();
        await host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            (await db.Users.SingleAsync()).StatusId = StatusIds.Inactive;
            await db.SaveChangesAsync();
        });

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(host, TestHost.AdminPassword));
        Assert.Contains("inactivo", ex.Message);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_after_grace_closes_the_session()
    {
        await using var host = await TestHost.CreateAsync();
        var login = await Login(host, TestHost.AdminPassword);
        var session = await SingleSession(host);

        var rotated = await Refresh(host, login.RefreshToken);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);

        // Fuera del margen de gracia, reutilizar el token viejo se trata como robo...
        host.Time.Advance(AuthService.RefreshReuseGracePeriod + TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Refresh(host, login.RefreshToken));

        // ...se cierra la sesión: el token nuevo deja de servir y el access token queda invalidado.
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Refresh(host, rotated.RefreshToken));
        Assert.True(IsRevoked(host, session.Id));
        Assert.Equal(SessionEndReasons.TokenReuse, (await SingleSession(host)).RevokedReason);
    }

    [Fact]
    public async Task Refresh_reuse_within_grace_period_is_allowed()
    {
        await using var host = await TestHost.CreateAsync();
        var login = await Login(host, TestHost.AdminPassword);

        await Refresh(host, login.RefreshToken);
        var second = await Refresh(host, login.RefreshToken); // p. ej. otra pestaña al mismo tiempo

        Assert.NotNull(second.AccessToken);
    }

    [Fact]
    public async Task Session_closes_after_30_minutes_without_activity()
    {
        await using var host = await TestHost.CreateAsync();
        var login = await Login(host, TestHost.AdminPassword);

        host.Time.Advance(TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(1));

        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() => Refresh(host, login.RefreshToken));
        Assert.Equal(AuthService.IdleSession, ex.Message);
        Assert.Equal(SessionEndReasons.Idle, (await SingleSession(host)).RevokedReason);
    }

    [Fact]
    public async Task Activity_keeps_the_session_alive_until_the_absolute_limit()
    {
        await using var host = await TestHost.CreateAsync();
        var token = (await Login(host, TestHost.AdminPassword)).RefreshToken;

        // Usuario activo: renueva cada 25 min (dentro de la ventana de 30) durante casi 12 horas.
        for (var elapsed = TimeSpan.Zero; elapsed < TimeSpan.FromHours(11.5); elapsed += TimeSpan.FromMinutes(25))
        {
            host.Time.Advance(TimeSpan.FromMinutes(25));
            token = (await Refresh(host, token)).RefreshToken;
        }

        // Al superar la duración máxima (12 h) se exige iniciar sesión de nuevo.
        host.Time.Advance(TimeSpan.FromMinutes(25));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Refresh(host, token));
        Assert.NotNull((await SingleSession(host)).RevokedAt);
    }

    [Fact]
    public async Task Logout_invalidates_the_session_and_its_access_tokens()
    {
        await using var host = await TestHost.CreateAsync();
        var login = await Login(host, TestHost.AdminPassword);
        var session = await SingleSession(host);
        Assert.False(IsRevoked(host, session.Id));

        await host.RunAsync(sp => sp.GetRequiredService<IAuthService>().LogoutAsync(login.RefreshToken, null));

        Assert.True(IsRevoked(host, session.Id)); // el access token se rechaza de inmediato
        Assert.Equal(SessionEndReasons.Logout, (await SingleSession(host)).RevokedReason);

        // Ni siquiera dentro del margen de gracia se puede reutilizar el refresh token.
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Refresh(host, login.RefreshToken));
    }

    [Fact]
    public async Task Logout_with_only_the_access_token_session_id_also_works()
    {
        await using var host = await TestHost.CreateAsync();
        var login = await Login(host, TestHost.AdminPassword);
        var session = await SingleSession(host);

        await host.RunAsync(sp => sp.GetRequiredService<IAuthService>().LogoutAsync(null, session.Id));

        Assert.True(IsRevoked(host, session.Id));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Refresh(host, login.RefreshToken));
    }

    [Fact]
    public async Task Revoked_sessions_are_reloaded_after_a_restart()
    {
        await using var host = await TestHost.CreateAsync();
        var login = await Login(host, TestHost.AdminPassword);
        await host.RunAsync(sp => sp.GetRequiredService<IAuthService>().LogoutAsync(login.RefreshToken, null));
        var session = await SingleSession(host);

        // Simula un reinicio: memoria vacía y recarga desde la base de datos.
        using var freshCache = new MemoryCache(new MemoryCacheOptions());
        var freshStore = new MemorySessionRevocationStore(freshCache, host.Time);
        Assert.False(freshStore.IsRevoked(session.Id));

        var loaded = await host.RunAsync(sp => new SessionRevocationLoader(
            sp.GetRequiredService<IRepository<UserSession>>(), freshStore,
            sp.GetRequiredService<IJwtTokenService>(), host.Time).LoadAsync());

        Assert.Equal(1, loaded);
        Assert.True(freshStore.IsRevoked(session.Id));
    }
}
