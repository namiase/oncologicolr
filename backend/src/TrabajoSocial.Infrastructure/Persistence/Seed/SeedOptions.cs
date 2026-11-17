namespace TrabajoSocial.Infrastructure.Persistence.Seed;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminUsername { get; set; } = "admin";
    public string AdminFullName { get; set; } = "Administrador del sistema";

    /// <summary>Contraseña inicial del administrador. Se configura con user-secrets; si falta, no se crea el usuario.</summary>
    public string? AdminPassword { get; set; }

    public string CompanyName { get; set; } = "Trabajo Social";

    /// <summary>
    /// Carga los datos de referencia (países, estados y municipios de Venezuela, Colombia y Brasil,
    /// religiones, profesiones y oficios). Se desactiva en pruebas para que sean rápidas.
    /// </summary>
    public bool LoadReferenceData { get; set; } = true;
}
