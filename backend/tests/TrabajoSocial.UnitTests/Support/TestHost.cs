using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Infrastructure;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.Infrastructure.Persistence.Seed;

namespace TrabajoSocial.UnitTests.Support;

/// <summary>
/// Contenedor de DI real (Application + Infrastructure) sobre SQLite en memoria,
/// con usuario y reloj controlables. Cada test crea su propia base de datos.
/// </summary>
public sealed class TestHost : IAsyncDisposable
{
    public const string AdminPassword = "Admin123!";

    private readonly SqliteConnection connection;
    private readonly ServiceProvider provider;

    public FakeCurrentUser CurrentUser { get; } = new();
    public FixedTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero));

    private TestHost(bool loadReferenceData)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('k', 64),
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Seed:AdminPassword"] = AdminPassword,
                ["Seed:LoadReferenceData"] = loadReferenceData.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructureServices(configuration);
        services.AddSingleton<ICurrentUserService>(CurrentUser);
        services.AddSingleton<TimeProvider>(Time);
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlite(connection);
            TrabajoSocial.Infrastructure.DependencyInjection.ConfigureDbContext(options, sp);
        });

        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    /// <param name="loadReferenceData">Carga países/estados/municipios y listas (más lento; solo donde se prueba).</param>
    public static async Task<TestHost> CreateAsync(bool loadReferenceData = false)
    {
        var host = new TestHost(loadReferenceData);
        await using var scope = host.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        await host.SeedAsync();
        return host;
    }

    /// <summary>Ejecuta el seeder (equivale a un arranque del API).</summary>
    public Task SeedAsync() => RunAsync(sp => sp.GetRequiredService<DbSeeder>().SeedAsync());

    public AsyncServiceScope CreateScope() => provider.CreateAsyncScope();

    /// <summary>Ejecuta una acción en un scope nuevo (equivalente a una petición HTTP).</summary>
    public async Task<T> RunAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = CreateScope();
        return await action(scope.ServiceProvider);
    }

    public Task RunAsync(Func<IServiceProvider, Task> action)
        => RunAsync(async sp => { await action(sp); return true; });

    public async ValueTask DisposeAsync()
    {
        await provider.DisposeAsync();
        await connection.DisposeAsync();
    }
}

public sealed class FakeCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Username { get; set; } = "tester";
    public Guid? RoleId { get; set; }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan delta) => Now += delta;
}
