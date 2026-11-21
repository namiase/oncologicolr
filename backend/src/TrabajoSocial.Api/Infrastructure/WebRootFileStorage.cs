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
        if (ResolveInsideUploads(publicPath) is { } fullPath && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public async Task<byte[]?> ReadAsync(string publicPath, CancellationToken ct = default)
        => ResolveInsideUploads(publicPath) is { } fullPath && File.Exists(fullPath)
            ? await File.ReadAllBytesAsync(fullPath, ct)
            : null;

    /// <summary>Ruta física del archivo, o null si queda fuera de wwwroot/uploads (path traversal).</summary>
    private string? ResolveInsideUploads(string publicPath)
    {
        var uploadsRoot = Path.GetFullPath(Path.Combine(WebRoot, UploadsFolder)) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(WebRoot, publicPath.TrimStart('/')));
        return fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex SafeSegment();

    [GeneratedRegex(@"^\.[a-z0-9]{2,5}$")]
    private static partial Regex SafeExtension();
}
