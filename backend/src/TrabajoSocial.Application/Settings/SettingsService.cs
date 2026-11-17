using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Entities.Settings;

namespace TrabajoSocial.Application.Settings;

public sealed record BrandingDto(string CompanyName, string? LogoUrl);

public sealed record UpdateBrandingCommand(
    string CompanyName,
    Stream? Logo,
    string? LogoFileName,
    long LogoLength,
    bool RemoveLogo);

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

    public async Task<BrandingDto> GetBrandingAsync(CancellationToken ct = default)
    {
        var setting = await settings.Query().OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        return setting is null
            ? new BrandingDto(DefaultCompanyName, null)
            : new BrandingDto(setting.CompanyName, setting.LogoPath);
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

        if (command.Logo is not null)
        {
            var extension = Path.GetExtension(command.LogoFileName ?? string.Empty).ToLowerInvariant();
            await using var buffer = await ValidateLogoAsync(command.Logo, extension, command.LogoLength, ct);
            setting.LogoPath = await fileStorage.SaveAsync(buffer, "branding", extension, ct);
        }
        else if (command.RemoveLogo)
        {
            setting.LogoPath = null;
        }

        await unitOfWork.SaveChangesAsync(ct);

        if (previousLogo is not null && previousLogo != setting.LogoPath)
        {
            await fileStorage.DeleteAsync(previousLogo, ct);
        }

        return new BrandingDto(setting.CompanyName, setting.LogoPath);
    }

    private static async Task<MemoryStream> ValidateLogoAsync(Stream logo, string extension, long length, CancellationToken ct)
    {
        if (!AllowedSignatures.TryGetValue(extension, out var signatures))
        {
            throw new ValidationException("logo", "Formato no permitido. Use PNG, JPG o WEBP.");
        }

        if (length is <= 0 or > MaxLogoBytes)
        {
            throw new ValidationException("logo", "El logo debe pesar como máximo 2 MB.");
        }

        var buffer = new MemoryStream();
        await logo.CopyToAsync(buffer, ct);
        if (buffer.Length > MaxLogoBytes)
        {
            throw new ValidationException("logo", "El logo debe pesar como máximo 2 MB.");
        }

        var bytes = buffer.GetBuffer();
        var validSignature = signatures.Any(sig => buffer.Length >= sig.Length && bytes.AsSpan(0, sig.Length).SequenceEqual(sig));
        if (validSignature && extension == ".webp")
        {
            validSignature = buffer.Length >= 12 && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        }

        if (!validSignature)
        {
            throw new ValidationException("logo", "El archivo no es una imagen válida.");
        }

        buffer.Position = 0;
        return buffer;
    }
}
