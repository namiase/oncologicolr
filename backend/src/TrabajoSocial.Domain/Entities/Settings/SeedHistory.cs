using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Domain.Entities.Settings;

/// <summary>
/// Paquete de datos de referencia ya aplicado (p. ej. "geo:BR:v1"). Cada paquete se aplica
/// una sola vez, así los cambios que haga el administrador después no se sobrescriben.
/// </summary>
public class SeedHistory : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public int RecordsAdded { get; set; }
}
