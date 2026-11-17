using Microsoft.AspNetCore.Mvc;
using TrabajoSocial.Api.Authorization;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Application.SocialWorkers;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Api.Controllers;

[ApiController]
[Route("api/social-workers")]
[Produces("application/json")]
public sealed class SocialWorkersController(ISocialWorkerService socialWorkers) : ControllerBase
{
    [HttpGet]
    [HasPermission(MenuCodes.SocialWorkers, PermissionAction.View)]
    public async Task<ActionResult<PagedResult<SocialWorkerDto>>> GetPaged([FromQuery] PagedQuery query, CancellationToken ct)
        => Ok(await socialWorkers.GetPagedAsync(query, ct));

    /// <summary>Trabajadoras sociales activas para listas desplegables (?withoutUser=true: sin usuario vinculado).</summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetLookup([FromQuery] bool withoutUser, CancellationToken ct)
        => Ok(await socialWorkers.GetLookupAsync(withoutUser, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(MenuCodes.SocialWorkers, PermissionAction.View)]
    public async Task<ActionResult<SocialWorkerDto>> GetById(Guid id, CancellationToken ct)
        => await socialWorkers.GetByIdAsync(id, ct) is { } item ? Ok(item) : NotFound();

    [HttpPost]
    [HasPermission(MenuCodes.SocialWorkers, PermissionAction.Create)]
    public async Task<ActionResult<SocialWorkerDto>> Create(SocialWorkerUpsertDto dto, CancellationToken ct)
    {
        var item = await socialWorkers.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MenuCodes.SocialWorkers, PermissionAction.Update)]
    public async Task<ActionResult<SocialWorkerDto>> Update(Guid id, SocialWorkerUpsertDto dto, CancellationToken ct)
        => Ok(await socialWorkers.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(MenuCodes.SocialWorkers, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await socialWorkers.DeleteAsync(id, ct);
        return NoContent();
    }
}
