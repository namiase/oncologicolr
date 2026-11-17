using Microsoft.AspNetCore.Mvc;
using TrabajoSocial.Api.Authorization;
using TrabajoSocial.Application.Catalogs;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Api.Controllers;

/// <summary>CRUD genérico de tablas maestras: /api/catalogs/{key} (genders, states, ...).</summary>
[ApiController]
[Route("api/catalogs")]
[Produces("application/json")]
public sealed class CatalogsController(ICatalogServiceFactory catalogs) : ControllerBase
{
    /// <summary>Lista las tablas maestras disponibles.</summary>
    [HttpGet]
    [HasPermission(MenuCodes.MasterTables, PermissionAction.View)]
    public ActionResult<IEnumerable<CatalogDefinitionDto>> GetDefinitions()
        => Ok(CatalogRegistry.All.Select(d => new CatalogDefinitionDto(
            d.Key, d.Name, d.SingularName, d.Group, d.ParentKey,
            d.ParentKey is not null && CatalogRegistry.TryGet(d.ParentKey, out var parent) ? parent.SingularName : null,
            d.Feminine)));

    [HttpGet("{key}")]
    [HasPermission(MenuCodes.MasterTables, PermissionAction.View)]
    public async Task<ActionResult<PagedResult<CatalogItemDto>>> GetPaged(
        string key, [FromQuery] CatalogQuery query, CancellationToken ct)
        => Ok(await catalogs.Get(key).GetPagedAsync(query, ct));

    /// <summary>Registros activos para listas desplegables (disponible para cualquier usuario autenticado).</summary>
    [HttpGet("{key}/lookup")]
    public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetLookup(
        string key, [FromQuery] Guid? parentId, CancellationToken ct)
        => Ok(await catalogs.Get(key).GetLookupAsync(parentId, ct));

    [HttpGet("{key}/{id:guid}")]
    [HasPermission(MenuCodes.MasterTables, PermissionAction.View)]
    public async Task<ActionResult<CatalogItemDto>> GetById(string key, Guid id, CancellationToken ct)
        => await catalogs.Get(key).GetByIdAsync(id, ct) is { } item ? Ok(item) : NotFound();

    [HttpPost("{key}")]
    [HasPermission(MenuCodes.MasterTables, PermissionAction.Create)]
    public async Task<ActionResult<CatalogItemDto>> Create(string key, CatalogUpsertDto dto, CancellationToken ct)
    {
        var item = await catalogs.Get(key).CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { key, id = item.Id }, item);
    }

    [HttpPut("{key}/{id:guid}")]
    [HasPermission(MenuCodes.MasterTables, PermissionAction.Update)]
    public async Task<ActionResult<CatalogItemDto>> Update(string key, Guid id, CatalogUpsertDto dto, CancellationToken ct)
        => Ok(await catalogs.Get(key).UpdateAsync(id, dto, ct));

    /// <summary>Eliminación lógica (IsDeleted = 1, estatus Eliminado).</summary>
    [HttpDelete("{key}/{id:guid}")]
    [HasPermission(MenuCodes.MasterTables, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(string key, Guid id, CancellationToken ct)
    {
        await catalogs.Get(key).DeleteAsync(id, ct);
        return NoContent();
    }
}
