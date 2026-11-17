using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.Settings;

namespace TrabajoSocial.Infrastructure.Persistence.Seed;

/// <summary>
/// Carga los datos de referencia (países, estados/departamentos y municipios de Venezuela,
/// Colombia y Brasil, religiones, profesiones y oficios).
/// - Cada paquete se aplica UNA vez y queda registrado en SeedHistory.
/// - Dentro de un paquete solo se insertan los registros que faltan (por código o nombre,
///   incluso entre los eliminados), así nunca choca con datos cargados a mano.
/// - Para actualizar datos en el futuro se agrega un paquete nuevo (p. ej. "geo:BR:v2").
/// </summary>
public sealed class ReferenceDataSeeder(AppDbContext db, ILogger<ReferenceDataSeeder> logger)
{
    /// <summary>Orden de aparición de los países en las listas (el resto, alfabético).</summary>
    private static readonly Dictionary<string, int> CountryPriority = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VE"] = 1,
        ["CO"] = 2,
        ["BR"] = 3
    };

    private const int DefaultCountryOrder = 10;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var applied = (await db.SeedHistory.IgnoreQueryFilters().Select(h => h.Key).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await ApplyAsync(applied, "catalog:religions:v1", () => SeedListAsync(db.Religions, "religions.json", ct), ct);
        await ApplyAsync(applied, "catalog:professions:v1", () => SeedListAsync(db.Professions, "professions.json", ct), ct);
        await ApplyAsync(applied, "catalog:occupations:v1", () => SeedListAsync(db.Occupations, "occupations.json", ct), ct);
        await ApplyAsync(applied, "geo:countries:v1", () => SeedCountriesAsync(ct), ct);
        await ApplyAsync(applied, "geo:VE:v1", () => SeedGeographyAsync("geo-ve.json", ct), ct);
        await ApplyAsync(applied, "geo:CO:v1", () => SeedGeographyAsync("geo-co.json", ct), ct);
        await ApplyAsync(applied, "geo:BR:v1", () => SeedGeographyAsync("geo-br.json", ct), ct);
    }

    private async Task ApplyAsync(HashSet<string> applied, string key, Func<Task<int>> seed, CancellationToken ct)
    {
        if (applied.Contains(key))
        {
            return;
        }

        var watch = Stopwatch.StartNew();
        var added = await seed();

        // Los datos del paquete y su registro en el historial se guardan en la misma transacción.
        db.SeedHistory.Add(new SeedHistory { Key = key, RecordsAdded = added });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        applied.Add(key);
        logger.LogInformation("Datos de referencia '{Key}': {Count} registros en {Elapsed} ms.",
            key, added, watch.ElapsedMilliseconds);
    }

    private async Task<int> SeedListAsync<T>(DbSet<T> set, string file, CancellationToken ct)
        where T : CatalogEntity, new()
    {
        var names = SeedData.Load<string[]>(file);
        var existing = (await set.IgnoreQueryFilters().Select(e => e.Name).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = names
            .Select((name, index) => new T { Name = name.Trim(), SortOrder = index + 1 })
            .Where(e => existing.Add(e.Name))
            .ToList();

        set.AddRange(toAdd);
        return toAdd.Count;
    }

    private async Task<int> SeedCountriesAsync(CancellationToken ct)
    {
        var countries = SeedData.Load<CodeNameSeed[]>("countries.json");
        var existing = await db.Countries.IgnoreQueryFilters().Select(c => new { c.Code, c.Name }).ToListAsync(ct);
        var codes = existing.Where(c => c.Code != null).Select(c => c.Code!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var names = existing.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = countries
            .Where(c => !codes.Contains(c.Code!) && names.Add(c.Name))
            .Select(c => new Country
            {
                Code = c.Code,
                Name = c.Name,
                SortOrder = CountryPriority.GetValueOrDefault(c.Code!, DefaultCountryOrder)
            })
            .ToList();

        db.Countries.AddRange(toAdd);
        return toAdd.Count;
    }

    private async Task<int> SeedGeographyAsync(string file, CancellationToken ct)
    {
        var geography = SeedData.Load<GeographySeed>(file);
        var country = await db.Countries.FirstOrDefaultAsync(c => c.Code == geography.CountryCode, ct);
        if (country is null)
        {
            logger.LogWarning("No se cargó {File}: el país {Code} no existe o fue eliminado.", file, geography.CountryCode);
            return 0;
        }

        var states = await db.States.IgnoreQueryFilters().Where(s => s.ParentId == country.Id).ToListAsync(ct);
        var stateIds = states.Select(s => s.Id).ToList();
        var municipalities = (await db.Municipalities.IgnoreQueryFilters()
                .Where(m => stateIds.Contains(m.ParentId))
                .Select(m => new { m.ParentId, m.Code, m.Name })
                .ToListAsync(ct))
            .ToLookup(m => m.ParentId);

        var added = 0;
        for (var i = 0; i < geography.States.Count; i++)
        {
            var seed = geography.States[i];
            var state = states.FirstOrDefault(s => Same(s.Code, seed.Code) || Same(s.Name, seed.Name));

            if (state is null)
            {
                state = new State { Code = seed.Code, Name = seed.Name, SortOrder = i + 1, ParentId = country.Id };
                db.States.Add(state);
                states.Add(state);
                added++;
            }
            else if (state.IsDeleted)
            {
                continue; // el administrador lo eliminó: no se cargan sus municipios
            }
            else if (state.Code is null && seed.Code is not null && !states.Any(s => Same(s.Code, seed.Code)))
            {
                state.Code = seed.Code; // completa el código oficial de estados cargados antes sin código
            }

            var existing = municipalities[state.Id].ToList();
            var codes = existing.Where(m => m.Code != null).Select(m => m.Code!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var names = existing.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var municipality in seed.Municipalities)
            {
                if ((municipality.Code is not null && codes.Contains(municipality.Code)) || !names.Add(municipality.Name))
                {
                    continue;
                }

                db.Municipalities.Add(new Municipality
                {
                    Code = municipality.Code,
                    Name = municipality.Name,
                    Parent = state
                });
                added++;
            }
        }

        return added;
    }

    private static bool Same(string? a, string? b)
        => a is not null && b is not null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
