using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrabajoSocial.Api.Authorization;
using TrabajoSocial.Application.Settings;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Produces("application/json")]
public sealed class SettingsController(ISettingsService settings) : ControllerBase
{
    /// <summary>Nombre y logo de la empresa (público: se muestra también en el login).</summary>
    [HttpGet("branding")]
    [AllowAnonymous]
    public async Task<ActionResult<BrandingDto>> GetBranding(CancellationToken ct)
        => Ok(await settings.GetBrandingAsync(ct));

    /// <summary>Actualiza el nombre y, opcionalmente, el logo (PNG/JPG/WEBP, máx. 2 MB).</summary>
    [HttpPut("branding")]
    [HasPermission(MenuCodes.CompanySettings, PermissionAction.Update)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<BrandingDto>> UpdateBranding([FromForm] UpdateBrandingForm form, CancellationToken ct)
    {
        await using var logo = form.Logo?.OpenReadStream();
        var command = new UpdateBrandingCommand(
            form.CompanyName, logo, form.Logo?.FileName, form.Logo?.Length ?? 0, form.RemoveLogo);
        return Ok(await settings.UpdateBrandingAsync(command, ct));
    }

    public sealed class UpdateBrandingForm
    {
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        public IFormFile? Logo { get; set; }

        public bool RemoveLogo { get; set; }
    }
}
