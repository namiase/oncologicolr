using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Users;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;

namespace TrabajoSocial.Application.Auth;

/// <summary>
/// Inicio, renovación y cierre de sesión.
/// - Access token de vida corta (15 min) con el Id de sesión ("sid").
/// - Refresh token rotativo que vence a los 30 min sin renovarse (cierre por inactividad
///   aplicado también en el servidor) y una duración máxima absoluta por sesión.
/// - Cerrar sesión invalida de inmediato los access tokens emitidos para esa sesión.
/// </summary>
public sealed class AuthService(
    IRepository<User> users,
    IRepository<UserSession> sessions,
    IRepository<RefreshToken> refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasher,
    IJwtTokenService jwt,
    SessionTerminator terminator,
    IOptions<SessionOptions> sessionOptions,
    MenuTreeBuilder menuTreeBuilder,
    TimeProvider time) : IAuthService
{
    public const int MaxFailedAccessAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Margen en el que reutilizar un refresh token recién rotado no se considera robo
    /// (p. ej. dos pestañas refrescando a la vez).
    /// </summary>
    public static readonly TimeSpan RefreshReuseGracePeriod = TimeSpan.FromSeconds(30);

    public const string InvalidCredentials = "Usuario o contraseña incorrectos.";
    public const string InvalidSession = "La sesión expiró. Inicie sesión nuevamente.";
    public const string IdleSession = "La sesión se cerró por inactividad. Inicie sesión nuevamente.";

    /// <summary>Hash señuelo para que un usuario inexistente tarde lo mismo que uno real.</summary>
    private static string? decoyPasswordHash;

    private readonly SessionOptions policy = sessionOptions.Value;

    public async Task<AuthResult> LoginAsync(LoginRequest request, ClientInfo client, CancellationToken ct = default)
    {
        var username = request.Username.Trim();
        var now = UtcNow();

        var user = await users.QueryTracked()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null)
        {
            // Mismo costo que verificar una contraseña real: el tiempo de respuesta no revela
            // si el usuario existe (evita la enumeración de usuarios por tiempo).
            decoyPasswordHash ??= passwordHasher.Hash(new User(), Guid.NewGuid().ToString("N"));
            passwordHasher.Verify(new User { PasswordHash = decoyPasswordHash }, request.Password);
            throw new AuthenticationFailedException(InvalidCredentials);
        }

        if (user.IsLockedOut(now))
        {
            // Mismo mensaje y mismo costo que una contraseña incorrecta: la respuesta no revela
            // que la cuenta existe (ni que está bloqueada). Durante el bloqueo no se aceptan
            // contraseñas, ni siquiera la correcta.
            passwordHasher.Verify(user, request.Password);
            throw new AuthenticationFailedException(InvalidCredentials);
        }

        var check = passwordHasher.Verify(user, request.Password);
        if (check == PasswordCheck.Failed)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAccessAttempts)
            {
                user.LockoutEnd = now.Add(LockoutDuration);
                user.AccessFailedCount = 0;
            }

            await unitOfWork.SaveChangesAsync(ct);
            throw new AuthenticationFailedException(InvalidCredentials);
        }

        if (user.StatusId != StatusIds.Active || user.Role.StatusId != StatusIds.Active)
        {
            throw new AuthenticationFailedException("El usuario está inactivo. Contacte al administrador.");
        }

        if (check == PasswordCheck.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.Hash(user, request.Password);
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;

        var session = new UserSession
        {
            UserId = user.Id,
            ExpiresAt = now.Add(policy.AbsoluteTimeout),
            LastActivityAt = now,
            IpAddress = Truncate(client.IpAddress, 64),
            UserAgent = Truncate(client.UserAgent, 256)
        };
        await sessions.AddAsync(session, ct);

        var result = await IssueTokensAsync(user, session, now, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return result;
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new AuthenticationFailedException(InvalidSession);
        }

        var now = UtcNow();
        var hash = jwt.HashRefreshToken(refreshToken);
        var stored = await refreshTokens.QueryTracked()
            .Include(t => t.Session)
            .Include(t => t.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || stored.Session.RevokedAt is not null)
        {
            throw new AuthenticationFailedException(InvalidSession);
        }

        var session = stored.Session;

        if (session.ExpiresAt <= now)
        {
            await EndSessionAsync(session, SessionEndReasons.Expired, now, ct);
            throw new AuthenticationFailedException(InvalidSession);
        }

        // El refresh token vence a los 30 min de su emisión: si no se renovó, hubo inactividad.
        if (stored.ExpiresAt <= now)
        {
            await EndSessionAsync(session, SessionEndReasons.Idle, now, ct);
            throw new AuthenticationFailedException(IdleSession);
        }

        if (stored.RevokedAt is { } revokedAt)
        {
            var withinGrace = stored.ReplacedByTokenHash is not null && now - revokedAt <= RefreshReuseGracePeriod;
            if (!withinGrace)
            {
                // Reutilización de un token ya rotado: posible robo. Se cierra toda la sesión.
                await EndSessionAsync(session, SessionEndReasons.TokenReuse, now, ct);
                throw new AuthenticationFailedException(InvalidSession);
            }
        }

        // El bloqueo por intentos fallidos solo impide nuevos inicios de sesión: no cierra una
        // sesión ya abierta (si no, cualquiera podría expulsar a un usuario con 5 claves malas).
        var user = stored.User;
        if (user.StatusId != StatusIds.Active || user.Role.StatusId != StatusIds.Active)
        {
            await EndSessionAsync(session, SessionEndReasons.UserDisabled, now, ct);
            throw new AuthenticationFailedException(InvalidSession);
        }

        session.LastActivityAt = now;
        var result = await IssueTokensAsync(user, session, now, ct);
        if (stored.RevokedAt is null)
        {
            stored.RevokedAt = now;
            stored.ReplacedByTokenHash = jwt.HashRefreshToken(result.RefreshToken);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return result;
    }

    public async Task LogoutAsync(string? refreshToken, Guid? sessionId, CancellationToken ct = default)
    {
        var sessionIds = new HashSet<Guid>();
        if (sessionId is { } fromAccessToken)
        {
            sessionIds.Add(fromAccessToken);
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = jwt.HashRefreshToken(refreshToken);
            var fromCookie = await refreshTokens.Query()
                .Where(t => t.TokenHash == hash)
                .Select(t => (Guid?)t.SessionId)
                .FirstOrDefaultAsync(ct);
            if (fromCookie is { } id)
            {
                sessionIds.Add(id);
            }
        }

        if (sessionIds.Count == 0)
        {
            return;
        }

        var now = UtcNow();
        var active = await sessions.QueryTracked()
            .Where(s => sessionIds.Contains(s.Id) && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in active)
        {
            await EndSessionAsync(session, SessionEndReasons.Logout, now, ct, save: false);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("El usuario no existe.");

        return await ToDtoAsync(user, ct);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, UserSession session, DateTime now, CancellationToken ct)
    {
        var access = jwt.CreateAccessToken(user, session.Id, session.ExpiresAt);
        var refresh = jwt.GenerateRefreshToken();

        // Ventana deslizante de inactividad, sin superar la duración máxima de la sesión.
        var idleDeadline = now.Add(policy.IdleTimeout);
        var refreshExpiresAt = idleDeadline < session.ExpiresAt ? idleDeadline : session.ExpiresAt;

        await refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            SessionId = session.Id,
            TokenHash = jwt.HashRefreshToken(refresh),
            ExpiresAt = refreshExpiresAt
        }, ct);

        var dto = await ToDtoAsync(user, ct);
        var expiresIn = (int)Math.Max(0, (access.ExpiresAt - now).TotalSeconds);
        return new AuthResult(
            access.Token, access.ExpiresAt, expiresIn, refresh, refreshExpiresAt, policy.IdleTimeoutMinutes, dto);
    }

    /// <summary>Cierra la sesión: revoca sus refresh tokens e invalida sus access tokens vigentes.</summary>
    private async Task EndSessionAsync(
        UserSession session, string reason, DateTime now, CancellationToken ct, bool save = true)
    {
        await terminator.EndAsync(session, reason, now, ct);
        if (save)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    private async Task<CurrentUserDto> ToDtoAsync(User user, CancellationToken ct)
    {
        var menus = await menuTreeBuilder.BuildAsync(user.RoleId, ct);
        var tier = AdminTiers.Of(user);
        return new CurrentUserDto(
            user.Id, user.Username, user.FullName, user.Email, user.RoleId, user.Role.Name, user.MustChangePassword,
            tier == AdminTier.Principal, tier.CanCreateAdministrators(), menus);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

    private DateTime UtcNow() => time.GetUtcNow().UtcDateTime;
}
