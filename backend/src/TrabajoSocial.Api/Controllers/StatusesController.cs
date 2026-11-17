using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;

namespace TrabajoSocial.Api.Controllers;

/// <summary>Estatus asignables por el usuario (Activo / Inactivo).</summary>
[ApiController]
[Route("api/statuses")]
[Produces("application/json")]
public sealed class StatusesController(IRepository<Status> statuses) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> Get(CancellationToken ct)
        => Ok(await statuses.Query()
            .Where(s => StatusIds.Assignable.Contains(s.Id))
            .OrderBy(s => s.Id)
            .Select(s => new LookupItemDto(s.Id, s.Name, s.Code, null))
            .ToListAsync(ct));
}
