using Microsoft.AspNetCore.Mvc;
using TrabajoSocial.Api.Authorization;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Application.Users;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Api.Controllers;

/// <summary>Administración de usuarios del sistema.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    [HasPermission(MenuCodes.Users, PermissionAction.View)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged([FromQuery] UserQuery query, CancellationToken ct)
        => Ok(await users.GetPagedAsync(query, ct));

    /// <summary>
    /// Perfiles activos. Con assignable=true, solo los que el usuario actual puede asignar
    /// (Administrador solo para el principal o un autorizado por él).
    /// </summary>
    [HttpGet("roles")]
    [HasPermission(MenuCodes.Users, PermissionAction.View)]
    public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetRoles([FromQuery] bool assignable, CancellationToken ct)
        => Ok(await users.GetRolesAsync(assignable, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(MenuCodes.Users, PermissionAction.View)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
        => await users.GetByIdAsync(id, ct) is { } user ? Ok(user) : NotFound();

    [HttpPost]
    [HasPermission(MenuCodes.Users, PermissionAction.Create)]
    public async Task<ActionResult<UserDto>> Create(UserCreateDto dto, CancellationToken ct)
    {
        var user = await users.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MenuCodes.Users, PermissionAction.Update)]
    public async Task<ActionResult<UserDto>> Update(Guid id, UserUpdateDto dto, CancellationToken ct)
        => Ok(await users.UpdateAsync(id, dto, ct));

    /// <summary>Eliminación lógica. Cierra de inmediato las sesiones del usuario.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(MenuCodes.Users, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await users.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Asigna una contraseña nueva, desbloquea la cuenta y cierra sus sesiones.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(MenuCodes.Users, PermissionAction.Update)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordDto dto, CancellationToken ct)
    {
        await users.ResetPasswordAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>Quita el bloqueo por intentos fallidos.</summary>
    [HttpPost("{id:guid}/unlock")]
    [HasPermission(MenuCodes.Users, PermissionAction.Update)]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
    {
        await users.UnlockAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Solo el administrador principal: autoriza (o no) a un administrador para crear
    /// administradores y gestionar sus cuentas. Cierra las sesiones del usuario.
    /// </summary>
    [HttpPut("{id:guid}/admin-delegation")]
    [HasPermission(MenuCodes.Users, PermissionAction.Update)]
    public async Task<IActionResult> SetAdminDelegation(Guid id, AdminDelegationDto dto, CancellationToken ct)
    {
        await users.SetAdminDelegationAsync(id, dto.Enabled, ct);
        return NoContent();
    }
}
