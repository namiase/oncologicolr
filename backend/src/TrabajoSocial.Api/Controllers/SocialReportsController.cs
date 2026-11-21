using Microsoft.AspNetCore.Mvc;
using TrabajoSocial.Api.Authorization;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Application.Reports;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Api.Controllers;

/// <summary>Informes sociales (entrevista preliminar) y su PDF de 2 hojas Carta.</summary>
[ApiController]
[Route("api/social-reports")]
[Produces("application/json")]
public sealed class SocialReportsController(ISocialReportService reports) : ControllerBase
{
    /// <summary>Trabajadora social vinculada al usuario y si puede elegir otra (administrador).</summary>
    [HttpGet("context")]
    [HasPermission(MenuCodes.CreateReport, PermissionAction.View)]
    public async Task<ActionResult<SocialReportContextDto>> GetContext(CancellationToken ct)
        => Ok(await reports.GetContextAsync(ct));

    /// <summary>Informes visibles para el usuario (un administrador ve todos), del más reciente al más antiguo.</summary>
    [HttpGet]
    [HasPermission(MenuCodes.CreateReport, PermissionAction.View)]
    public async Task<ActionResult<PagedResult<SocialReportListItemDto>>> GetPaged([FromQuery] PagedQuery query, CancellationToken ct)
        => Ok(await reports.GetPagedAsync(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(MenuCodes.CreateReport, PermissionAction.View)]
    public async Task<ActionResult<SocialReportDto>> GetById(Guid id, CancellationToken ct)
        => await reports.GetByIdAsync(id, ct) is { } item ? Ok(item) : NotFound();

    [HttpPost]
    [HasPermission(MenuCodes.CreateReport, PermissionAction.Create)]
    public async Task<ActionResult<SocialReportDto>> Create(SocialReportUpsertDto dto, CancellationToken ct)
    {
        var item = await reports.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MenuCodes.CreateReport, PermissionAction.Update)]
    public async Task<ActionResult<SocialReportDto>> Update(Guid id, SocialReportUpsertDto dto, CancellationToken ct)
        => Ok(await reports.UpdateAsync(id, dto, ct));

    /// <summary>
    /// PDF del informe para imprimir (2 hojas Carta). Un administrador puede indicar otra
    /// trabajadora social para la firma (?socialWorkerId=); a los demás usuarios se les ignora.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [HasPermission(MenuCodes.CreateReport, PermissionAction.View)]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, [FromQuery] Guid? socialWorkerId, CancellationToken ct)
    {
        var file = await reports.GetPdfAsync(id, socialWorkerId, ct);
        return File(file.Content, "application/pdf", file.FileName);
    }
}
