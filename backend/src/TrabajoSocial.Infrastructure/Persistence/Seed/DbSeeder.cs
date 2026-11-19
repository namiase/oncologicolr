using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Domain.Entities.Settings;

namespace TrabajoSocial.Infrastructure.Persistence.Seed;

/// <summary>Datos iniciales idempotentes: solo inserta lo que falta.</summary>
public sealed class DbSeeder(
    AppDbContext db,
    IPasswordHasherService passwordHasher,
    ReferenceDataSeeder referenceData,
    IOptions<SeedOptions> options,
    ILogger<DbSeeder> logger)
{
    private readonly SeedOptions seed = options.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedStatusesAsync(ct);
        var roles = await SeedRolesAsync(ct);
        var menus = await SeedMenusAsync(ct);
        await SeedRoleMenusAsync(roles, menus, ct);
        await SeedAdminAsync(roles[RoleNames.Administrator], ct);
        await SeedCompanyAsync(ct);
        await SeedCatalogsAsync(ct);

        if (seed.LoadReferenceData)
        {
            await referenceData.SeedAsync(ct);
        }
    }

    private async Task SeedStatusesAsync(CancellationToken ct)
    {
        (Guid Id, string Code, string Name)[] statuses =
        [
            (StatusIds.Active, "ACT", "Activo"),
            (StatusIds.Inactive, "INA", "Inactivo"),
            (StatusIds.Deleted, "ELI", "Eliminado")
        ];

        var existing = await db.Statuses.IgnoreQueryFilters().Select(s => s.Id).ToListAsync(ct);
        foreach (var (id, code, name) in statuses.Where(s => !existing.Contains(s.Id)))
        {
            db.Statuses.Add(new Status { Id = id, Code = code, Name = name });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken ct)
    {
        (string Name, string Description)[] roles =
        [
            (RoleNames.Administrator, "Acceso total al sistema."),
            (RoleNames.SocialWorker, "Elabora informes y consulta reportes.")
        ];

        foreach (var (name, description) in roles)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == name, ct))
            {
                db.Roles.Add(new Role { Name = name, Description = description });
            }
        }

        await db.SaveChangesAsync(ct);
        return await db.Roles.ToDictionaryAsync(r => r.Name, ct);
    }

    private async Task<Dictionary<string, Menu>> SeedMenusAsync(CancellationToken ct)
    {
        // Títulos en mayúscula solo al inicio, igual que los títulos de cada página.
        var definitions = new (string Code, string Title, string? Path, string Icon, int Order, string? Parent)[]
        {
            (MenuCodes.Home, "Inicio", "/inicio", "house", 1, null),
            (MenuCodes.CreateReport, "Crear informe", "/informes/nuevo", "file-plus", 2, null),
            (MenuCodes.Configuration, "Configuración", null, "settings", 3, null),
            (MenuCodes.MasterTables, "Tablas maestras", "/configuracion/tablas-maestras", "database", 1, MenuCodes.Configuration),
            (MenuCodes.SocialWorkers, "Trabajadoras sociales", "/configuracion/trabajadoras-sociales", "users", 2, MenuCodes.Configuration),
            (MenuCodes.Users, "Usuarios", "/configuracion/usuarios", "user-cog", 3, MenuCodes.Configuration),
            (MenuCodes.CompanySettings, "Empresa y logo", "/configuracion/empresa", "building-2", 4, MenuCodes.Configuration),
            (MenuCodes.Reports, "Reportes", "/reportes", "chart-column", 4, null)
        };

        // Los menús son del sistema (no se editan desde la aplicación): se crean los que faltan
        // y los existentes se alinean con esta definición (título, ruta, icono y orden).
        var menus = await db.Menus.ToDictionaryAsync(m => m.Code, ct);
        foreach (var d in definitions)
        {
            if (menus.TryGetValue(d.Code, out var existing))
            {
                existing.Title = d.Title;
                existing.Path = d.Path;
                existing.Icon = d.Icon;
                existing.SortOrder = d.Order;
                continue;
            }

            var menu = new Menu
            {
                Code = d.Code,
                Title = d.Title,
                Path = d.Path,
                Icon = d.Icon,
                SortOrder = d.Order,
                Parent = d.Parent is null ? null : menus[d.Parent]
            };
            db.Menus.Add(menu);
            menus[d.Code] = menu;
        }

        await db.SaveChangesAsync(ct);
        return menus;
    }

    private async Task SeedRoleMenusAsync(
        Dictionary<string, Role> roles, Dictionary<string, Menu> menus, CancellationToken ct)
    {
        var grants = new List<(string Role, string Menu, bool Create, bool Update, bool Delete)>();

        // Administrador: todo.
        grants.AddRange(menus.Keys.Select(code => (RoleNames.Administrator, code, true, true, true)));

        // Trabajadora Social: inicio, crear informe y reportes.
        grants.Add((RoleNames.SocialWorker, MenuCodes.Home, false, false, false));
        grants.Add((RoleNames.SocialWorker, MenuCodes.CreateReport, true, true, false));
        grants.Add((RoleNames.SocialWorker, MenuCodes.Reports, false, false, false));

        var existing = await db.RoleMenus.Select(rm => new { rm.RoleId, rm.MenuId }).ToListAsync(ct);
        foreach (var g in grants)
        {
            var role = roles[g.Role];
            var menu = menus[g.Menu];
            if (existing.Any(e => e.RoleId == role.Id && e.MenuId == menu.Id))
            {
                continue;
            }

            db.RoleMenus.Add(new RoleMenu
            {
                RoleId = role.Id,
                MenuId = menu.Id,
                CanView = true,
                CanCreate = g.Create,
                CanUpdate = g.Update,
                CanDelete = g.Delete
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedAdminAsync(Role adminRole, CancellationToken ct)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == seed.AdminUsername, ct))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning(
                "No se creó el usuario administrador: falta Seed:AdminPassword (configúrelo con dotnet user-secrets).");
            return;
        }

        var admin = new User { Username = seed.AdminUsername, FullName = seed.AdminFullName, RoleId = adminRole.Id };
        admin.PasswordHash = passwordHasher.Hash(admin, seed.AdminPassword);
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Usuario administrador '{Username}' creado.", seed.AdminUsername);
    }

    private async Task SeedCompanyAsync(CancellationToken ct)
    {
        if (!await db.CompanySettings.AnyAsync(ct))
        {
            db.CompanySettings.Add(new CompanySetting { CompanyName = seed.CompanyName });
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task SeedCatalogsAsync(CancellationToken ct)
    {
        await SeedCatalogAsync(db.Genders, [("F", "Femenino"), ("M", "Masculino")], ct);

        await SeedCatalogAsync(db.MaritalStatuses,
        [
            (null, "Soltero(a)"), (null, "Casado(a)"), (null, "Divorciado(a)"),
            (null, "Viudo(a)"), (null, "Unión estable de hecho")
        ], ct);

        await SeedCatalogAsync(db.Kinships,
        [
            (null, "Madre"), (null, "Padre"), (null, "Hijo(a)"), (null, "Hermano(a)"),
            (null, "Abuelo(a)"), (null, "Nieto(a)"), (null, "Tío(a)"), (null, "Sobrino(a)"),
            (null, "Primo(a)"), (null, "Cónyuge"), (null, "Concubino(a)"), (null, "Padrastro"),
            (null, "Madrastra"), (null, "Otro")
        ], ct);

        await SeedCatalogAsync(db.EducationLevels,
        [
            (null, "Sin instrucción"), (null, "Educación inicial"), (null, "Primaria"),
            (null, "Media general (Bachillerato)"), (null, "Técnico medio"),
            (null, "Técnico superior universitario (TSU)"), (null, "Universitario"), (null, "Postgrado")
        ], ct);

        await SeedCatalogAsync(db.AreaCodes,
        [
            ("0212", "Caracas - Distrito Capital (0212)"), ("0412", "Digitel (0412)"), ("0414", "Movistar (0414)"),
            ("0424", "Movistar (0424)"), ("0416", "Movilnet (0416)"), ("0426", "Movilnet (0426)")
        ], ct);

        await SeedCatalogAsync(db.Countries, [("VE", "Venezuela")], ct);

        // Estados de Venezuela (misma fuente que los datos de referencia, con su código ISO 3166-2).
        var venezuela = await db.Countries.FirstAsync(c => c.Code == "VE", ct);
        if (!await db.States.AnyAsync(ct))
        {
            var states = SeedData.Load<GeographySeed>("geo-ve.json").States;
            db.States.AddRange(states.Select((state, i) => new State
            {
                Code = state.Code,
                Name = state.Name,
                SortOrder = i + 1,
                ParentId = venezuela.Id
            }));
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task SeedCatalogAsync<T>(DbSet<T> set, (string? Code, string Name)[] items, CancellationToken ct)
        where T : CatalogEntity, new()
    {
        if (await set.AnyAsync(ct))
        {
            return;
        }

        set.AddRange(items.Select((item, i) => new T { Code = item.Code, Name = item.Name, SortOrder = i + 1 }));
        await db.SaveChangesAsync(ct);
    }
}
