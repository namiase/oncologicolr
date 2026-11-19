using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Auth;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Domain.Entities.SocialWorkers;

namespace TrabajoSocial.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(UserQuery query, CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <param name="assignableOnly">Solo los perfiles que el usuario actual puede asignar.</param>
    Task<IReadOnlyList<LookupItemDto>> GetRolesAsync(bool assignableOnly = false, CancellationToken ct = default);
    Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordDto dto, CancellationToken ct = default);
    Task UnlockAsync(Guid id, CancellationToken ct = default);
    Task SetAdminDelegationAsync(Guid id, bool enabled, CancellationToken ct = default);
    Task ChangeOwnPasswordAsync(Guid userId, Guid? currentSessionId, ChangePasswordDto dto, CancellationToken ct = default);
}

/// <summary>
/// Administración de usuarios. Reglas de protección:
/// - Nadie puede eliminarse, desactivarse ni cambiar su propio perfil.
/// - El administrador principal (el que creó el sistema) es intocable para los demás: nadie
///   puede modificarlo, eliminarlo, desbloquearlo ni cambiarle la contraseña. Él mismo solo
///   puede editar su nombre y correo, y cambiar su contraseña con "Cambiar contraseña".
/// - Solo el principal, o un administrador que él autorice, crea administradores y gestiona
///   sus cuentas; a los autorizados solo los gestiona el principal (ver <see cref="AdminTier"/>).
///   Como el principal siempre es un administrador activo, el sistema nunca se queda sin uno.
/// - Cambiar perfil, estatus o autorización, restablecer la contraseña o eliminar un usuario
///   cierra sus sesiones de inmediato (sus tokens dejan de servir).
/// </summary>
public sealed partial class UserService(
    IRepository<User> users,
    IRepository<Role> roles,
    IRepository<SocialWorker> socialWorkers,
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasher,
    SessionTerminator sessions,
    ICurrentUserService currentUser,
    TimeProvider time) : IUserService
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(UserQuery query, CancellationToken ct = default)
    {
        var q = users.Query();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(u => u.Username.Contains(s) || u.FullName.Contains(s) || (u.Email != null && u.Email.Contains(s)));
        }

        if (query.StatusId is { } statusId)
        {
            q = q.Where(u => u.StatusId == statusId);
        }

        if (query.RoleId is { } roleId)
        {
            q = q.Where(u => u.RoleId == roleId);
        }

        var desc = query.SortDesc;
        q = query.SortBy?.ToLowerInvariant() switch
        {
            "username" => desc ? q.OrderByDescending(u => u.Username) : q.OrderBy(u => u.Username),
            "fullname" => desc ? q.OrderByDescending(u => u.FullName) : q.OrderBy(u => u.FullName),
            "role" => desc ? q.OrderByDescending(u => u.Role.Name) : q.OrderBy(u => u.Role.Name),
            "lastloginat" => desc ? q.OrderByDescending(u => u.LastLoginAt) : q.OrderBy(u => u.LastLoginAt),
            "status" => desc ? q.OrderByDescending(u => u.Status.Name) : q.OrderBy(u => u.Status.Name),
            "createdat" => desc ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt),
            _ => desc ? q.OrderByDescending(u => u.FullName) : q.OrderBy(u => u.FullName)
        };

        return await Project(q).ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    public Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Project(users.Query().Where(u => u.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<LookupItemDto>> GetRolesAsync(bool assignableOnly = false, CancellationToken ct = default)
    {
        var q = roles.Query().Where(r => r.StatusId == StatusIds.Active);
        if (assignableOnly && !(await GetActorTierAsync(ct)).CanCreateAdministrators())
        {
            q = q.Where(r => r.Name != RoleNames.Administrator);
        }

        return await q
            .OrderBy(r => r.Name)
            .Select(r => new LookupItemDto(r.Id, r.Name, null, null, null))
            .ToListAsync(ct);
    }

    public async Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim().ToLowerInvariant();
        var errors = new Dictionary<string, string[]>();
        if (!UsernameRegex().IsMatch(username))
        {
            errors["username"] = ["Use de 3 a 50 caracteres: letras, números, punto, guion o guion bajo (sin espacios)."];
        }

        if (PasswordPolicy.Validate(dto.Password, username) is { } passwordError)
        {
            errors["password"] = [passwordError];
        }

        AddStatusError(errors, dto.StatusId);
        ThrowIfAny(errors);

        var role = await GetActiveRoleAsync(dto.RoleId, ct);
        if (IsAdministratorRole(role))
        {
            EnsureCanCreateAdministrators(await GetActorTierAsync(ct));
        }

        if (await users.Query().AnyAsync(u => u.Username == username, ct))
        {
            throw new ConflictException($"Ya existe un usuario \"{username}\".", "username");
        }

        var email = NormalizeEmail(dto.Email);
        await EnsureEmailAvailableAsync(email, null, ct);

        var user = new User
        {
            Username = username,
            FullName = dto.FullName.Trim(),
            Email = email,
            RoleId = dto.RoleId,
            StatusId = dto.StatusId,
            // La contraseña inicial la conoce el administrador: debe cambiarse al primer ingreso.
            MustChangePassword = true
        };
        user.PasswordHash = passwordHasher.Hash(user, dto.Password);
        await users.AddAsync(user, ct);

        await LinkSocialWorkerAsync(user.Id, dto.SocialWorkerId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetByIdAsync(user.Id, ct))!;
    }

    public async Task<UserDto> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        var self = IsCurrentUser(id);
        var actor = await GetActorTierAsync(ct);
        if (!self)
        {
            EnsureCanManage(actor, user);
        }

        var errors = new Dictionary<string, string[]>();
        AddStatusError(errors, dto.StatusId);
        ThrowIfAny(errors);

        var newRole = await GetActiveRoleAsync(dto.RoleId, ct);
        var roleChanged = user.RoleId != dto.RoleId;
        var statusChanged = user.StatusId != dto.StatusId;

        if (self && (roleChanged || dto.StatusId != StatusIds.Active))
        {
            throw new BusinessRuleException("No puede cambiar su propio perfil ni desactivar su propia cuenta.");
        }

        if (roleChanged && IsAdministratorRole(newRole))
        {
            EnsureCanCreateAdministrators(actor);
        }

        var email = NormalizeEmail(dto.Email);
        await EnsureEmailAvailableAsync(email, id, ct);

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.RoleId = dto.RoleId;
        user.StatusId = dto.StatusId;
        if (!IsAdministratorRole(newRole))
        {
            // La autorización para crear administradores solo existe con ese perfil.
            user.CanManageAdministrators = false;
        }

        await LinkSocialWorkerAsync(id, dto.SocialWorkerId, ct);

        if (roleChanged || statusChanged)
        {
            // Sus permisos cambiaron: debe volver a iniciar sesión.
            await sessions.EndAllForUserAsync(id, SessionEndReasons.AccountChanged, null, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await GetByIdAsync(id, ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        if (IsCurrentUser(id))
        {
            throw new BusinessRuleException("No puede eliminar su propio usuario.");
        }

        EnsureCanManage(await GetActorTierAsync(ct), user);

        await LinkSocialWorkerAsync(id, null, ct);
        await sessions.EndAllForUserAsync(id, SessionEndReasons.UserDeleted, null, ct);
        users.Remove(user);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordDto dto, CancellationToken ct = default)
    {
        if (IsCurrentUser(id))
        {
            throw new BusinessRuleException("Para su propia cuenta use la opción \"Cambiar contraseña\".");
        }

        var user = await LoadAsync(id, ct);
        EnsureCanManage(await GetActorTierAsync(ct), user);
        if (PasswordPolicy.Validate(dto.NewPassword, user.Username) is { } error)
        {
            throw new ValidationException("newPassword", error);
        }

        user.PasswordHash = passwordHasher.Hash(user, dto.NewPassword);
        user.MustChangePassword = true;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await sessions.EndAllForUserAsync(id, SessionEndReasons.PasswordChanged, null, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task UnlockAsync(Guid id, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        if (!IsCurrentUser(id))
        {
            EnsureCanManage(await GetActorTierAsync(ct), user);
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// El principal autoriza (o deja de autorizar) a un administrador para crear administradores
    /// y gestionar sus cuentas. Cierra las sesiones del usuario para que entre con el cambio.
    /// </summary>
    public async Task SetAdminDelegationAsync(Guid id, bool enabled, CancellationToken ct = default)
    {
        if (await GetActorTierAsync(ct) != AdminTier.Principal)
        {
            throw new BusinessRuleException("Solo el administrador principal puede autorizar a otros administradores.");
        }

        var user = await LoadAsync(id, ct);
        if (user.IsSuperAdmin)
        {
            throw new BusinessRuleException("El administrador principal ya tiene todos los permisos.");
        }

        if (enabled && !IsAdministratorRole(user.Role))
        {
            throw new BusinessRuleException("Solo se puede autorizar a usuarios con el perfil Administrador.");
        }

        if (user.CanManageAdministrators == enabled)
        {
            return;
        }

        user.CanManageAdministrators = enabled;
        await sessions.EndAllForUserAsync(id, SessionEndReasons.AccountChanged, null, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ChangeOwnPasswordAsync(
        Guid userId, Guid? currentSessionId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await LoadAsync(userId, ct);

        if (passwordHasher.Verify(user, dto.CurrentPassword) == PasswordCheck.Failed)
        {
            throw new ValidationException("currentPassword", "La contraseña actual no es correcta.");
        }

        if (PasswordPolicy.Validate(dto.NewPassword, user.Username) is { } error)
        {
            throw new ValidationException("newPassword", error);
        }

        if (dto.NewPassword == dto.CurrentPassword)
        {
            throw new ValidationException("newPassword", "La nueva contraseña debe ser distinta de la actual.");
        }

        user.PasswordHash = passwordHasher.Hash(user, dto.NewPassword);
        user.MustChangePassword = false;

        // Se mantiene la sesión actual; se cierran las abiertas en otros equipos. El cliente
        // renueva su token para obtener uno sin la marca de cambio pendiente.
        await sessions.EndAllForUserAsync(userId, SessionEndReasons.PasswordChanged, currentSessionId, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    // ---------- Auxiliares ----------

    private IQueryable<UserDto> Project(IQueryable<User> query)
    {
        var now = time.GetUtcNow().UtcDateTime;
        return query.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            RoleName = u.Role.Name,
            StatusId = u.StatusId,
            StatusName = u.Status.Name,
            LastLoginAt = u.LastLoginAt,
            IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > now,
            MustChangePassword = u.MustChangePassword,
            IsAdministrator = u.Role.Name == RoleNames.Administrator,
            IsSuperAdmin = u.IsSuperAdmin,
            CanManageAdministrators = u.CanManageAdministrators,
            SocialWorkerId = u.SocialWorker != null ? u.SocialWorker.Id : null,
            SocialWorkerName = u.SocialWorker != null ? u.SocialWorker.FirstNames + " " + u.SocialWorker.LastNames : null,
            CreatedAt = u.CreatedAt,
            CreatedBy = u.CreatedBy,
            UpdatedAt = u.UpdatedAt,
            UpdatedBy = u.UpdatedBy
        });
    }

    private async Task<User> LoadAsync(Guid id, CancellationToken ct)
        => await users.QueryTracked().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct)
           ?? throw new NotFoundException("El usuario solicitado no existe.");

    private async Task<Role> GetActiveRoleAsync(Guid roleId, CancellationToken ct)
        => await roles.Query().FirstOrDefaultAsync(r => r.Id == roleId && r.StatusId == StatusIds.Active, ct)
           ?? throw new ValidationException("roleId", "Seleccione un perfil válido.");

    private bool IsCurrentUser(Guid id) => currentUser.UserId == id;

    private static bool IsAdministratorRole(Role role) => role.Name == RoleNames.Administrator;

    /// <summary>
    /// Nivel del usuario que hace la petición. Sin usuario, o con la cuenta inactiva, no tiene
    /// ningún privilegio sobre administradores.
    /// </summary>
    private async Task<AdminTier> GetActorTierAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not { } actorId)
        {
            return AdminTier.None;
        }

        var actor = await users.Query()
            .Where(u => u.Id == actorId && u.StatusId == StatusIds.Active)
            .Select(u => new
            {
                u.IsSuperAdmin,
                u.CanManageAdministrators,
                IsAdministrator = u.Role.Name == RoleNames.Administrator && u.Role.StatusId == StatusIds.Active
            })
            .FirstOrDefaultAsync(ct);

        return actor is null
            ? AdminTier.None
            : AdminTiers.Of(actor.IsSuperAdmin, actor.CanManageAdministrators, actor.IsAdministrator);
    }

    /// <summary>Una cuenta de administrador solo la gestiona alguien de nivel superior.</summary>
    private static void EnsureCanManage(AdminTier actor, User target)
    {
        var tier = AdminTiers.Of(target);
        if (tier == AdminTier.Principal)
        {
            throw new BusinessRuleException(
                "El administrador principal no se puede modificar, eliminar ni cambiarle la contraseña.");
        }

        if (tier != AdminTier.None && actor <= tier)
        {
            throw new BusinessRuleException(tier == AdminTier.Delegate
                ? "Solo el administrador principal puede modificar a los administradores que él autorizó."
                : "Solo el administrador principal o un administrador autorizado por él puede modificar cuentas de administrador.");
        }
    }

    private static void EnsureCanCreateAdministrators(AdminTier actor)
    {
        if (!actor.CanCreateAdministrators())
        {
            throw new ValidationException("roleId",
                "Solo el administrador principal o un administrador autorizado por él puede crear administradores.");
        }
    }

    private async Task EnsureEmailAvailableAsync(string? email, Guid? currentId, CancellationToken ct)
    {
        if (email is not null && await users.Query().AnyAsync(u => u.Email == email && u.Id != currentId, ct))
        {
            throw new ConflictException("Ya existe un usuario con ese correo electrónico.", "email");
        }
    }

    /// <summary>Vincula (o desvincula, con null) la trabajadora social de la cuenta.</summary>
    private async Task LinkSocialWorkerAsync(Guid userId, Guid? socialWorkerId, CancellationToken ct)
    {
        var current = await socialWorkers.QueryTracked().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (current?.Id == socialWorkerId)
        {
            return;
        }

        SocialWorker? target = null;
        if (socialWorkerId is { } id)
        {
            target = await socialWorkers.QueryTracked().FirstOrDefaultAsync(w => w.Id == id, ct)
                     ?? throw new ValidationException("socialWorkerId", "La trabajadora social seleccionada no existe.");
            if (target.UserId is not null && target.UserId != userId)
            {
                throw new ConflictException("Esa trabajadora social ya está vinculada a otro usuario.", "socialWorkerId");
            }
        }

        if (current is not null)
        {
            current.UserId = null;
        }

        if (target is not null)
        {
            target.UserId = userId;
        }
    }

    private static void AddStatusError(Dictionary<string, string[]> errors, Guid statusId)
    {
        if (!StatusIds.Assignable.Contains(statusId))
        {
            errors["statusId"] = ["El estatus seleccionado no es válido."];
        }
    }

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    [GeneratedRegex("^[a-z0-9._-]{3,50}$")]
    private static partial Regex UsernameRegex();
}
