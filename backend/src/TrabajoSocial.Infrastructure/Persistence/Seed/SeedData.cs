using System.Reflection;
using System.Text.Json;

namespace TrabajoSocial.Infrastructure.Persistence.Seed;

/// <summary>Lee los JSON de datos de referencia incrustados en el ensamblado (Seed/Data/*.json).</summary>
internal static class SeedData
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static T Load<T>(string fileName)
    {
        var resource = $"seed.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"No se encontró el recurso de datos '{resource}'.");
        return JsonSerializer.Deserialize<T>(stream, Options)
            ?? throw new InvalidOperationException($"El recurso '{resource}' está vacío.");
    }
}

internal sealed record CodeNameSeed(string? Code, string Name);

internal sealed record StateSeed(string? Code, string Name, List<CodeNameSeed> Municipalities);

internal sealed record GeographySeed(string CountryCode, List<StateSeed> States);
