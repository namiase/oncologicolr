using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Auth;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.Infrastructure.Persistence.Interceptors;
using TrabajoSocial.Infrastructure.Persistence.Repositories;
using TrabajoSocial.Infrastructure.Persistence.Seed;
using TrabajoSocial.Infrastructure.Security;

namespace TrabajoSocial.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra EF Core (SQL Server) y los servicios de infraestructura.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:Default'.");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            ConfigureDbContext(options, sp);
        });

        return services.AddInfrastructureServices(configuration);
    }

    /// <summary>Servicios de infraestructura sin el proveedor de base de datos (reutilizable en tests).</summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= 32,
                "Jwt:Key debe tener al menos 32 bytes. Configúrela con: dotnet user-secrets set \"Jwt:Key\" \"<clave>\"")
            .ValidateOnStart();

        services.AddOptions<SessionOptions>()
            .Bind(configuration.GetSection(SessionOptions.SectionName))
            .Validate(o => o.IdleTimeoutMinutes > 0 && o.AbsoluteTimeoutHours > 0, "Session: valores inválidos.")
            .ValidateOnStart();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<ReferenceDataSeeder>();
        services.AddScoped<DbSeeder>();

        return services;
    }

    /// <summary>Opciones comunes del DbContext: interceptor de auditoría y advertencias.</summary>
    public static void ConfigureDbContext(DbContextOptionsBuilder options, IServiceProvider sp)
    {
        options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());

        // Los padres nunca se eliminan mientras tengan hijos (lo valida la aplicación),
        // por lo que la interacción filtro global / navegación requerida es segura.
        options.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }
}
