using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Auth;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.SocialWorkers;
using TrabajoSocial.Application.Users;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.UnitTests.Support;

namespace TrabajoSocial.UnitTests;

public sealed class UserServiceTests
{
    private const string Password = "Clave2026";
    private static readonly ClientInfo Client = new("127.0.0.1", "xunit");

    private static Task<Guid> RoleId(TestHost host, string name)
        => host.RunAsync(async sp => (await sp.GetRequiredService<AppDbContext>().Roles.SingleAsync(r => r.Name == name)).Id);

    private static Task<Guid> AdminId(TestHost host)
        => host.RunAsync(async sp => (await sp.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Username == "admin")).Id);

    private static async Task<UserDto> CreateUser(TestHost host, string username, string? role = null, Guid? socialWorkerId = null)
    {
        var roleId = await RoleId(host, role ?? RoleNames.SocialWorker);
        return await host.RunAsync(sp => sp.GetRequiredService<IUserService>().CreateAsync(new UserCreateDto
        {
            Username = username,
            FullName = "María López",
            Email = $"{username.ToLowerInvariant()}@example.org",
            RoleId = roleId,
            StatusId = StatusIds.Active,
            Password = Password,
            SocialWorkerId = socialWorkerId
        }));
    }

    private static Task<AuthResult> Login(TestHost host, string username, string password)
        => host.RunAsync(sp => sp.GetRequiredService<IAuthService>()
            .LoginAsync(new LoginRequest { Username = username, Password = password }, Client));

    private static Task<Guid[]> SessionIds(TestHost host, Guid userId)
        => host.RunAsync(async sp => await sp.GetRequiredService<AppDbContext>().UserSessions
            .Where(s => s.UserId == userId).OrderBy(s => s.CreatedAt).Select(s => s.Id).ToArrayAsync());

    private static bool IsRevoked(TestHost host, Guid sessionId)
        => host.RunAsync(sp => Task.FromResult(sp.GetRequiredService<ISessionRevocationStore>().IsRevoked(sessionId))).Result;

    private static UserUpdateDto ToUpdate(UserDto user) => new()
    {
        FullName = user.FullName,
        Email = user.Email,
        RoleId = user.RoleId,
        StatusId = user.StatusId,
        SocialWorkerId = user.SocialWorkerId
    };

    [Fact]
    public async Task Created_user_is_normalized_and_can_log_in()
    {
        await using var host = await TestHost.CreateAsync();

        var user = await CreateUser(host, "  MLopez ");

        Assert.Equal("mlopez", user.Username);
        Assert.Equal(RoleNames.SocialWorker, user.RoleName);
        var login = await Login(host, "mlopez", Password);
        Assert.Equal([MenuCodes.Home, MenuCodes.CreateReport, MenuCodes.Reports], login.User.Menus.Select(m => m.Code));

        await host.RunAsync(async sp =>
        {
            var stored = await sp.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Username == "mlopez");
            Assert.NotEqual(Password, stored.PasswordHash);
        });
    }

    [Fact]
    public async Task Duplicate_username_and_weak_password_are_rejected()
    {
        await using var host = await TestHost.CreateAsync();
        await CreateUser(host, "mlopez");

        var duplicate = await Assert.ThrowsAsync<ConflictException>(() => CreateUser(host, "MLOPEZ"));
        Assert.Equal("username", duplicate.Field);

        var roleId = await RoleId(host, RoleNames.SocialWorker);
        var weak = await Assert.ThrowsAsync<ValidationException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<IUserService>().CreateAsync(new UserCreateDto
            {
                Username = "otra",
                FullName = "Otra",
                RoleId = roleId,
                StatusId = StatusIds.Active,
                Password = "solotexto"
            })));
        Assert.Contains("password", weak.Errors.Keys);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_on_create_and_update()
    {
        await using var host = await TestHost.CreateAsync();
        await CreateUser(host, "mlopez");                       // mlopez@example.org
        var other = await CreateUser(host, "jperez");           // jperez@example.org
        var roleId = await RoleId(host, RoleNames.SocialWorker);

        // Crear con el mismo correo, con otras mayúsculas y espacios.
        var onCreate = await Assert.ThrowsAsync<ConflictException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<IUserService>().CreateAsync(new UserCreateDto
            {
                Username = "otra",
                FullName = "Otra",
                Email = "  MLopez@Example.ORG ",
                RoleId = roleId,
                StatusId = StatusIds.Active,
                Password = Password
            })));
        Assert.Equal("email", onCreate.Field);

        // Editar otra cuenta poniéndole ese correo.
        var taken = ToUpdate(other);
        taken.Email = "MLOPEZ@example.org";
        var onUpdate = await Assert.ThrowsAsync<ConflictException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<IUserService>().UpdateAsync(other.Id, taken)));
        Assert.Equal("email", onUpdate.Field);

        // Guardar la cuenta con su propio correo no es un duplicado.
        var own = ToUpdate(other);
        own.FullName = "Juan Pérez";
        var saved = await host.RunAsync(sp => sp.GetRequiredService<IUserService>().UpdateAsync(other.Id, own));
        Assert.Equal("jperez@example.org", saved.Email);

        // Varias cuentas sin correo no chocan entre sí.
        foreach (var name in new[] { "sincorreo1", "sincorreo2" })
        {
            await host.RunAsync(sp => sp.GetRequiredService<IUserService>().CreateAsync(new UserCreateDto
            {
                Username = name,
                FullName = name,
                Email = null,
                RoleId = roleId,
                StatusId = StatusIds.Active,
                Password = Password
            }));
        }
    }

    [Fact]
    public async Task Users_cannot_delete_deactivate_or_demote_themselves()
    {
        await using var host = await TestHost.CreateAsync();
        var adminId = await AdminId(host);
        host.CurrentUser.UserId = adminId;
        var admin = (await host.RunAsync(sp => sp.GetRequiredService<IUserService>().GetByIdAsync(adminId)))!;

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IUserService>().DeleteAsync(adminId)));

        var demote = ToUpdate(admin);
        demote.RoleId = await RoleId(host, RoleNames.SocialWorker);
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IUserService>().UpdateAsync(adminId, demote)));
    }

    [Fact]
    public async Task The_seeded_admin_is_the_only_principal_administrator()
    {
        await using var host = await TestHost.CreateAsync();
        var adminId = await AdminId(host);

        var login = await Login(host, "admin", TestHost.AdminPassword);
        Assert.True(login.User.IsSuperAdmin);
        Assert.True(login.User.CanManageAdministrators);

        // Volver a ejecutar el seeder no crea otro principal.
        await host.SeedAsync();
        var principals = await host.RunAsync(sp =>
            sp.GetRequiredService<AppDbContext>().Users.Where(u => u.IsSuperAdmin).Select(u => u.Id).ToListAsync());
        Assert.Equal([adminId], principals);
    }

    [Fact]
    public async Task Nobody_else_can_modify_delete_unlock_or_reset_the_principal_administrator()
    {
        await using var host = await TestHost.CreateAsync();
        var adminId = await AdminId(host);
        var admin = (await host.RunAsync(sp => sp.GetRequiredService<IUserService>().GetByIdAsync(adminId)))!;

        // Ni siquiera un administrador autorizado por el principal.
        host.CurrentUser.UserId = adminId;
        var boss = await CreateUser(host, "jefa", RoleNames.Administrator);
        await host.RunAsync(sp => sp.GetRequiredService<IUserService>().SetAdminDelegationAsync(boss.Id, true));
        host.CurrentUser.UserId = boss.Id;

        var rename = ToUpdate(admin);
        rename.FullName = "Otro nombre";
        Func<IUserService, Task>[] attempts =
        [
            s => s.UpdateAsync(adminId, rename),
            s => s.DeleteAsync(adminId),
            s => s.ResetPasswordAsync(adminId, new ResetPasswordDto { NewPassword = "NuevaClave9" }),
            s => s.UnlockAsync(adminId)
        ];
        foreach (var attempt in attempts)
        {
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                host.RunAsync(sp => attempt(sp.GetRequiredService<IUserService>())));
            Assert.Contains("administrador principal", ex.Message);
        }

        // Él mismo sí puede corregir su nombre y correo (no su perfil ni su estatus).
        host.CurrentUser.UserId = adminId;
        var saved = await host.RunAsync(sp => sp.GetRequiredService<IUserService>().UpdateAsync(adminId, rename));
        Assert.Equal("Otro nombre", saved.FullName);
        Assert.True(saved.IsSuperAdmin);
    }

    [Fact]
    public async Task Only_the_principal_or_an_authorized_admin_can_create_and_manage_administrators()
    {
        await using var host = await TestHost.CreateAsync();
        var adminId = await AdminId(host);
        var adminRole = await RoleId(host, RoleNames.Administrator);

        host.CurrentUser.UserId = adminId;
        var normal = await CreateUser(host, "admin2", RoleNames.Administrator);
        var other = await CreateUser(host, "admin3", RoleNames.Administrator);
        var worker = await CreateUser(host, "mlopez");

        // Un administrador normal no crea administradores ni gestiona sus cuentas...
        host.CurrentUser.UserId = normal.Id;
        var create = await Assert.ThrowsAsync<ValidationException>(() => CreateUser(host, "admin4", RoleNames.Administrator));
        Assert.Contains("roleId", create.Errors.Keys);

        var promote = ToUpdate(worker);
        promote.RoleId = adminRole;
        await Assert.ThrowsAsync<ValidationException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IUserService>().UpdateAsync(worker.Id, promote)));
        await Assert.ThrowsAsync<BusinessRuleException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<IUserService>().ResetPasswordAsync(other.Id, new ResetPasswordDto { NewPassword = "NuevaClave9" })));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IUserService>().DeleteAsync(other.Id)));

        // ...ni ve el perfil Administrador entre los asignables; sí gestiona a los demás usuarios.
        var assignable = await host.RunAsync(sp => sp.GetRequiredService<IUserService>().GetRolesAsync(assignableOnly: true));
        Assert.DoesNotContain(assignable, r => r.Name == RoleNames.Administrator);
        await CreateUser(host, "jperez");
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IUserService>().SetAdminDelegationAsync(other.Id, true)));

        // El principal lo autoriza: sus sesiones se cierran para que entre con el permiso nuevo.
        await Login(host, "admin2", Password);
        var session = (await SessionIds(host, normal.Id)).Single();
        host.CurrentUser.UserId = adminId;
        await host.RunAsync(sp => sp.GetRequiredService<IUserService>().SetAdminDelegationAsync(normal.Id, true));
        Assert.True(IsRevoked(host, session));

        // Autorizado: crea administradores y gestiona a los normales...
        host.CurrentUser.UserId = normal.Id;
        var created = await CreateUser(host, "admin4", RoleNames.Administrator);
        Assert.True(created.IsAdministrator);
        await host.RunAsync(sp => sp.GetRequiredService<IUserService>()
            .ResetPasswordAsync(other.Id, new ResetPasswordDto { NewPassword = "NuevaClave9" }));

        // ...pero no autoriza a otros ni toca a otro autorizado: eso es solo del principal.
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IUserService>().SetAdminDelegationAsync(other.Id, true)));
        host.CurrentUser.UserId = adminId;
        await host.RunAsync(sp => sp.GetRequiredService<IUserService>().SetAdminDelegationAsync(other.Id, true));
        host.CurrentUser.UserId = normal.Id;
        var peer = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IUserService>().DeleteAsync(other.Id)));
        Assert.Contains("solo el administrador principal", peer.Message, StringComparison.OrdinalIgnoreCase);

        // Si el principal le quita el perfil Administrador, pierde también la autorización.
        host.CurrentUser.UserId = adminId;
        var demote = ToUpdate((await host.RunAsync(sp => sp.GetRequiredService<IUserService>().GetByIdAsync(normal.Id)))!);
        demote.RoleId = await RoleId(host, RoleNames.SocialWorker);
        var demoted = await host.RunAsync(sp => sp.GetRequiredService<IUserService>().UpdateAsync(normal.Id, demote));
        Assert.False(demoted.IsAdministrator);
        Assert.False(demoted.CanManageAdministrators);
    }

    [Fact]
    public async Task Changing_role_closes_the_users_sessions_immediately()
    {
        await using var host = await TestHost.CreateAsync();
        host.CurrentUser.UserId = await AdminId(host);
        var user = await CreateUser(host, "mlopez");
        var login = await Login(host, "mlopez", Password);
        var session = (await SessionIds(host, user.Id)).Single();

        var update = ToUpdate(user);
        update.RoleId = await RoleId(host, RoleNames.Administrator);
        await host.RunAsync(sp => sp.GetRequiredService<IUserService>().UpdateAsync(user.Id, update));

        Assert.True(IsRevoked(host, session));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            host.RunAsync(sp => sp.GetRequiredService<IAuthService>().RefreshAsync(login.RefreshToken)));
    }

    [Fact]
    public async Task Reset_password_unlocks_the_account_and_closes_sessions()
    {
        await using var host = await TestHost.CreateAsync();
        var user = await CreateUser(host, "mlopez");
        await Login(host, "mlopez", Password);
        var session = (await SessionIds(host, user.Id)).Single();

        for (var i = 0; i < AuthService.MaxFailedAccessAttempts; i++)
        {
            await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(host, "mlopez", "incorrecta1"));
        }

        var locked = await host.RunAsync(sp => sp.GetRequiredService<IUserService>().GetByIdAsync(user.Id));
        Assert.True(locked!.IsLockedOut);

        await host.RunAsync(sp => sp.GetRequiredService<IUserService>()
            .ResetPasswordAsync(user.Id, new ResetPasswordDto { NewPassword = "NuevaClave9" }));

        Assert.True(IsRevoked(host, session));
        Assert.NotNull((await Login(host, "mlopez", "NuevaClave9")).AccessToken);
    }

    [Fact]
    public async Task Changing_own_password_keeps_the_current_session_and_closes_the_others()
    {
        await using var host = await TestHost.CreateAsync();
        var user = await CreateUser(host, "mlopez");
        await Login(host, "mlopez", Password);
        host.Time.Advance(TimeSpan.FromSeconds(1));
        await Login(host, "mlopez", Password);
        var sessions = await SessionIds(host, user.Id);
        var (current, other) = (sessions[0], sessions[1]);

        var wrong = await Assert.ThrowsAsync<ValidationException>(() => host.RunAsync(sp =>
            sp.GetRequiredService<IUserService>().ChangeOwnPasswordAsync(user.Id, current,
                new ChangePasswordDto { CurrentPassword = "otra-cosa1", NewPassword = "NuevaClave9" })));
        Assert.Contains("currentPassword", wrong.Errors.Keys);

        await host.RunAsync(sp => sp.GetRequiredService<IUserService>().ChangeOwnPasswordAsync(user.Id, current,
            new ChangePasswordDto { CurrentPassword = Password, NewPassword = "NuevaClave9" }));

        Assert.False(IsRevoked(host, current));
        Assert.True(IsRevoked(host, other));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(host, "mlopez", Password));
        Assert.NotNull((await Login(host, "mlopez", "NuevaClave9")).AccessToken);
    }

    [Fact]
    public async Task A_social_worker_can_be_linked_to_only_one_user()
    {
        await using var host = await TestHost.CreateAsync();
        var first = await CreateWorker(host, "V-1234567", "A-1");
        var second = await CreateWorker(host, "V-7654321", "A-2");

        var user = await CreateUser(host, "mlopez", socialWorkerId: first);
        Assert.Equal(first, user.SocialWorkerId);

        await Assert.ThrowsAsync<ConflictException>(() => CreateUser(host, "otra", socialWorkerId: first));

        // Cambiar el vínculo libera a la trabajadora anterior.
        var relink = ToUpdate(user);
        relink.SocialWorkerId = second;
        var updated = await host.RunAsync(sp => sp.GetRequiredService<IUserService>().UpdateAsync(user.Id, relink));
        Assert.Equal(second, updated.SocialWorkerId);

        var other = await CreateUser(host, "otra", socialWorkerId: first);
        Assert.Equal(first, other.SocialWorkerId);
    }

    [Fact]
    public async Task Deleted_user_loses_access_and_the_username_can_be_reused()
    {
        await using var host = await TestHost.CreateAsync();
        var user = await CreateUser(host, "mlopez");
        await Login(host, "mlopez", Password);
        var session = (await SessionIds(host, user.Id)).Single();

        await host.RunAsync(sp => sp.GetRequiredService<IUserService>().DeleteAsync(user.Id));

        Assert.True(IsRevoked(host, session));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => Login(host, "mlopez", Password));
        Assert.NotEqual(user.Id, (await CreateUser(host, "mlopez")).Id);
    }

    private static Task<Guid> CreateWorker(TestHost host, string identityCard, string ants)
        => host.RunAsync(async sp => (await sp.GetRequiredService<ISocialWorkerService>().CreateAsync(new SocialWorkerUpsertDto
        {
            FirstNames = "Ana",
            LastNames = "Pérez",
            IdentityCard = identityCard,
            AntsRegistrationNumber = ants,
            SocialWorkersAssociation = "Colegio de TS",
            GraduationYear = 2010,
            StatusId = StatusIds.Active
        })).Id);
}
