using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Domain.Entities.Settings;

/// <summary>Configuración global de la empresa (una sola fila): nombre y logo.</summary>
public class CompanySetting : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Ruta pública relativa del logo, p. ej. /uploads/branding/logo-20261116.png</summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Membrete de los informes impresos (imagen del encabezado oficial, PNG/JPG), p. ej.
    /// /uploads/branding/x.png. Si no hay, el PDF usa el logo y el nombre de la empresa.
    /// </summary>
    public string? ReportHeaderPath { get; set; }
}
