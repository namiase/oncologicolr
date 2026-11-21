using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Entities.Settings;

namespace TrabajoSocial.Application.Settings;

/// <param name="ReportHeaderUrl">Membrete de los informes impresos (null: se usa el logo y el nombre).</param>
public sealed record BrandingDto(string CompanyName, string? LogoUrl, string? ReportHeaderUrl = null);

/// <summary>Archivo de imagen recibido (logo o membrete).</summary>
public sealed record ImageUpload(Stream Content, string? FileName, long Length);

public sealed record UpdateBrandingCommand(
    string CompanyName,
    ImageUpload? Logo,
    bool RemoveLogo,
    ImageUpload? ReportHeader = null,
    bool RemoveReportHeader = false);

public interface ISettingsService
{
    Task<BrandingDto> GetBrandingAsync(CancellationToken ct = default);
    Task<BrandingDto> UpdateBrandingAsync(UpdateBrandingCommand command, CancellationToken ct = default);
}

public sealed class SettingsService(
    IRepository<CompanySetting> settings,
    IUnitOfWork unitOfWork,
    IFileStorage fileStorage) : ISettingsService
{
    public const long MaxLogoBytes = 2 * 1024 * 1024;
    public const string DefaultCompanyName = "Trabajo Social";

    private static readonly Dictionary<string, byte[][]> AllowedSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]] // "RIFF" + "WEBP" en el byte 8 (se valida aparte)
    };

    /// <summary>El membrete se incrusta en el PDF, y el generador de PDF no admite WEBP.</summary>
    private static readonly string[] ReportHeaderExtensions = [".png", ".jpg", ".jpeg"];

    public async Task<BrandingDto> GetBrandingAsync(CancellationToken ct = default)
    {
        var setting = await settings.Query().OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        return setting is null
            ? new BrandingDto(DefaultCompanyName, null)
            : new BrandingDto(setting.CompanyName, setting.LogoPath, setting.ReportHeaderPath);
    }

    public async Task<BrandingDto> UpdateBrandingAsync(UpdateBrandingCommand command, CancellationToken ct = default)
    {
        var companyName = command.CompanyName.Trim();
        if (companyName.Length is 0 or > 150)
        {
            throw new ValidationException("companyName", "El nombre de la empresa es obligatorio (máximo 150 caracteres).");
        }

        var setting = await settings.QueryTracked().OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        if (setting is null)
        {
            setting = new CompanySetting();
            await settings.AddAsync(setting, ct);
        }

        setting.CompanyName = companyName;
        var previousLogo = setting.LogoPath;
        var previousHeader = setting.ReportHeaderPath;

        // Se validan ambas imágenes antes de guardar cualquiera de las dos.
        await using var logo = command.Logo is null
            ? null
            : await ValidateImageAsync(command.Logo, "logo", "El logo", AllowedSignatures.Keys, ct);
        await using var header = command.ReportHeader is null
            ? null
            : await ValidateImageAsync(command.ReportHeader, "reportHeader", "El membrete", ReportHeaderExtensions, ct);

        if (logo is not null)
        {
            setting.LogoPath = await fileStorage.SaveAsync(logo.Content, "branding", logo.Extension, ct);
        }
        else if (command.RemoveLogo)
        {
            setting.LogoPath = null;
        }

        if (header is not null)
        {
            setting.ReportHeaderPath = await fileStorage.SaveAsync(header.Content, "branding", header.Extension, ct);
        }
        else if (command.RemoveReportHeader)
        {
            setting.ReportHeaderPath = null;
        }

        await unitOfWork.SaveChangesAsync(ct);

        if (previousLogo is not null && previousLogo != setting.LogoPath)
        {
            await fileStorage.DeleteAsync(previousLogo, ct);
        }

        if (previousHeader is not null && previousHeader != setting.ReportHeaderPath)
        {
            await fileStorage.DeleteAsync(previousHeader, ct);
        }

        return new BrandingDto(setting.CompanyName, setting.LogoPath, setting.ReportHeaderPath);
    }

    private sealed record ValidatedImage(MemoryStream Content, string Extension) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Content.DisposeAsync();
    }

    /// <param name="field">Campo del formulario al que se asocia el error.</param>
    /// <param name="subject">"El logo", "El membrete": encabeza los mensajes de error.</param>
    private static async Task<ValidatedImage> ValidateImageAsync(
        ImageUpload upload, string field, string subject, IEnumerable<string> allowedExtensions, CancellationToken ct)
    {
        var extension = Path.GetExtension(upload.FileName ?? string.Empty).ToLowerInvariant();
        var allowed = allowedExtensions.ToArray();
        if (!allowed.Contains(extension) || !AllowedSignatures.TryGetValue(extension, out var signatures))
        {
            var formats = allowed.Contains(".webp") ? "PNG, JPG o WEBP" : "PNG o JPG";
            throw new ValidationException(field, $"Formato no permitido. Use {formats}.");
        }

        var sizeError = $"{subject} debe pesar como máximo 2 MB.";
        if (upload.Length is <= 0 or > MaxLogoBytes)
        {
            throw new ValidationException(field, sizeError);
        }

        var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, ct);
        if (buffer.Length > MaxLogoBytes)
        {
            throw new ValidationException(field, sizeError);
        }

        var bytes = buffer.GetBuffer();
        var validSignature = signatures.Any(sig => buffer.Length >= sig.Length && bytes.AsSpan(0, sig.Length).SequenceEqual(sig));
        if (validSignature && extension == ".webp")
        {
            validSignature = buffer.Length >= 12 && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        }

        if (!validSignature)
        {
            throw new ValidationException(field, "El archivo no es una imagen válida.");
        }

        buffer.Position = 0;
        return new ValidatedImage(buffer, extension);
    }
}
