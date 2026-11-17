using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.Security;
using TrabajoSocial.Domain.Entities.Settings;
using TrabajoSocial.Domain.Entities.SocialWorkers;

namespace TrabajoSocial.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    // Seguridad
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Tablas maestras
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Gender> Genders => Set<Gender>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<MaritalStatus> MaritalStatuses => Set<MaritalStatus>();
    public DbSet<Religion> Religions => Set<Religion>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<AreaCode> AreaCodes => Set<AreaCode>();
    public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();
    public DbSet<Profession> Professions => Set<Profession>();
    public DbSet<Occupation> Occupations => Set<Occupation>();
    public DbSet<Kinship> Kinships => Set<Kinship>();

    // Negocio
    public DbSet<SocialWorker> SocialWorkers => Set<SocialWorker>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();
    public DbSet<SeedHistory> SeedHistory => Set<SeedHistory>();

    private readonly List<Action> afterSave = [];

    public void AfterSaveChanges(Action action) => afterSave.Add(action);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            RunAfterSave();
            return result;
        }
        finally
        {
            afterSave.Clear();
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            RunAfterSave();
            return result;
        }
        finally
        {
            afterSave.Clear();
        }
    }

    private void RunAfterSave()
    {
        foreach (var action in afterSave.ToArray())
        {
            action();
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Soft delete: filtro global !IsDeleted en toda entidad que herede de BaseEntity.
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.BaseType is null)
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
            }

            // Con borrado lógico no se usan cascadas.
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
