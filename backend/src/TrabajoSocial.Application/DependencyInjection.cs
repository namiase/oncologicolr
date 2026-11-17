using Microsoft.Extensions.DependencyInjection;
using TrabajoSocial.Application.Auth;
using TrabajoSocial.Application.Catalogs;
using TrabajoSocial.Application.Dashboard;
using TrabajoSocial.Application.Permissions;
using TrabajoSocial.Application.Settings;
using TrabajoSocial.Application.SocialWorkers;
using TrabajoSocial.Application.Users;

namespace TrabajoSocial.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<ISessionRevocationStore, MemorySessionRevocationStore>();
        services.AddScoped<SessionRevocationLoader>();
        services.AddScoped<SessionTerminator>();
        services.AddScoped<MenuTreeBuilder>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ISocialWorkerService, SocialWorkerService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Tablas maestras: un servicio por catálogo, resuelto por su clave.
        services.AddScoped<ICatalogServiceFactory, CatalogServiceFactory>();
        foreach (var definition in CatalogRegistry.All)
        {
            var implementation = definition.ParentEntityType is { } parentType
                ? typeof(ChildCatalogService<,>).MakeGenericType(definition.EntityType, parentType)
                : typeof(CatalogService<>).MakeGenericType(definition.EntityType);

            var captured = definition;
            services.AddKeyedScoped(typeof(ICatalogService), captured.Key,
                (sp, _) => ActivatorUtilities.CreateInstance(sp, implementation, captured));
        }

        return services;
    }
}
