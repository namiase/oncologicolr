using System.Text.RegularExpressions;
using TrabajoSocial.Application.Common.Interfaces;

namespace TrabajoSocial.Api.Infrastructure;

/// <summary>Guarda archivos públicos en wwwroot/uploads/{carpeta}.</summary>
internal sealed partial class WebRootFileStorage(IWebHostEnvironment environment) : IFileStorage
{
    private const string UploadsFolder = "uploads";

    private string WebRoot => environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
    {
        if (!SafeSegment().IsMatch(folder) || !SafeExtension().IsMatch(extension))
        {
            throw new ArgumentException("Carpeta o extensión no válida.");
        }

        var directory = Path.Combine(WebRoot, UploadsFolder, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var file = File.Create(Path.Combine(directory, fileName)))
        {
            await content.CopyToAsync(file, ct);
        }

        return $"/{UploadsFolder}/{folder}/{fileName}";
    }

    public Task DeleteAsync(string publicPath, CancellationToken ct = default)
    {
        var uploadsRoot = Path.GetFullPath(Path.Combine(WebRoot, UploadsFolder)) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(WebRoot, publicPath.TrimStart('/')));

        // Evita borrar fuera de wwwroot/uploads (path traversal).
        if (fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex SafeSegment();

    [GeneratedRegex(@"^\.[a-z0-9]{2,5}$")]
    private static partial Regex SafeExtension();
}
