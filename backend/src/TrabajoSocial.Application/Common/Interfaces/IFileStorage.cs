namespace TrabajoSocial.Application.Common.Interfaces;

/// <summary>Almacenamiento de archivos públicos (p. ej. el logo).</summary>
public interface IFileStorage
{
    /// <summary>Guarda el contenido y devuelve la ruta pública relativa (p. ej. /uploads/branding/x.png).</summary>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default);

    Task DeleteAsync(string publicPath, CancellationToken ct = default);
}
