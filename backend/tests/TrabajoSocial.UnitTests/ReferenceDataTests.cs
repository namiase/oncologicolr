using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Catalogs;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.UnitTests.Support;

namespace TrabajoSocial.UnitTests;

/// <summary>Carga completa de datos de referencia (países, estados, municipios y listas).</summary>
public sealed class ReferenceDataTests
{
    private static Task<int> MunicipalitiesOf(AppDbContext db, string countryCode)
        => db.Municipalities.CountAsync(m => m.Parent.Parent.Code == countryCode);

    private static Task<int> StatesOf(AppDbContext db, string countryCode)
        => db.States.CountAsync(s => s.Parent.Code == countryCode);

    [Fact]
    public async Task Fresh_install_loads_all_reference_data_once()
    {
        await using var host = await TestHost.CreateAsync(loadReferenceData: true);

        await host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            Assert.Equal(249, await db.Countries.CountAsync());
            Assert.Equal(25, await StatesOf(db, "VE"));
            Assert.Equal(335, await MunicipalitiesOf(db, "VE"));
            Assert.Equal(33, await StatesOf(db, "CO"));
            Assert.Equal(1122, await MunicipalitiesOf(db, "CO"));
            Assert.Equal(27, await StatesOf(db, "BR"));
            Assert.Equal(5571, await MunicipalitiesOf(db, "BR"));
            Assert.Equal(23, await db.Religions.CountAsync());
            Assert.Equal(60, await db.Professions.CountAsync());
            Assert.Equal(47, await db.Occupations.CountAsync());
            Assert.Equal(7, await db.SeedHistory.CountAsync());

            // Códigos oficiales y nombres correctos.
            Assert.Equal("Colombia", (await db.Countries.SingleAsync(c => c.Code == "CO")).Name);
            Assert.Equal("VE-X", (await db.States.SingleAsync(s => s.Name == "La Guaira")).Code);
            Assert.Equal("Medellín", (await db.Municipalities.SingleAsync(m => m.Code == "05001")).Name);
            Assert.Equal("São Paulo", (await db.Municipalities.SingleAsync(m => m.Code == "3550308")).Name);
            Assert.Equal("Bogotá, D.C.", (await db.States.SingleAsync(s => s.Code == "11")).Name);
            Assert.True(await db.Municipalities.AnyAsync(m => m.Name == "Piar" && m.Parent.Name == "Bolívar"));

            // Venezuela, Colombia y Brasil aparecen primero en la lista de países.
            var first = await sp.GetRequiredService<ICatalogServiceFactory>().Get("countries").GetLookupAsync(null);
            Assert.Equal(["Venezuela", "Colombia", "Brasil"], first.Take(3).Select(c => c.Name));
        });

        // Un segundo arranque no duplica nada.
        await host.SeedAsync();
        await host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            Assert.Equal(249, await db.Countries.CountAsync());
            Assert.Equal(7028, await db.Municipalities.CountAsync());
        });
    }

    [Fact]
    public async Task Reference_data_does_not_override_admin_changes()
    {
        await using var host = await TestHost.CreateAsync(loadReferenceData: true);

        // El administrador elimina una religión y renombra otra.
        await host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Religions.Remove(await db.Religions.SingleAsync(r => r.Name == "Santería"));
            (await db.Religions.SingleAsync(r => r.Name == "Católica")).Name = "Católica romana";
            await db.SaveChangesAsync();
        });

        await host.SeedAsync(); // reinicio del API

        await host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            Assert.False(await db.Religions.AnyAsync(r => r.Name == "Santería"));
            Assert.False(await db.Religions.AnyAsync(r => r.Name == "Católica"));
            Assert.True(await db.Religions.AnyAsync(r => r.Name == "Católica romana"));
        });
    }

    [Fact]
    public async Task State_lookup_includes_the_country_to_tell_repeated_names_apart()
    {
        await using var host = await TestHost.CreateAsync(loadReferenceData: true);

        var states = await host.RunAsync(sp => sp.GetRequiredService<ICatalogServiceFactory>().Get("states").GetLookupAsync(null));

        var amazonas = states.Where(s => s.Name == "Amazonas").Select(s => s.ParentName).ToList();
        Assert.Equal(["Venezuela", "Colombia", "Brasil"], amazonas); // agrupado por país, Venezuela primero
        Assert.Equal("Venezuela", states[0].ParentName);
        Assert.Equal(85, states.Count); // 25 + 33 + 27
    }
}
