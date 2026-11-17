using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Entities.Catalogs;
using static TrabajoSocial.Infrastructure.Persistence.Configurations.EntityTypeBuilderExtensions;

namespace TrabajoSocial.Infrastructure.Persistence.Configurations;

/// <summary>Configuración común de todas las tablas maestras (esquema "catalog").</summary>
internal abstract class CatalogEntityConfiguration<T>(string tableName) : IEntityTypeConfiguration<T>
    where T : CatalogEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.ToTable(tableName, Schemas.Catalog);
        builder.ConfigureBase();

        builder.Property(e => e.Code).HasMaxLength(CatalogEntity.CodeMaxLength);
        builder.Property(e => e.Name).HasMaxLength(CatalogEntity.NameMaxLength).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(CatalogEntity.DescriptionMaxLength);

        builder.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusId);

        ConfigureUniqueness(builder);
    }

    protected virtual void ConfigureUniqueness(EntityTypeBuilder<T> builder)
    {
        builder.HasIndex(e => e.Name).IsUnique().HasFilter(NotDeleted);
        builder.HasIndex(e => e.Code).IsUnique().HasFilter(NotNullAndNotDeleted(nameof(CatalogEntity.Code)));
    }
}

/// <summary>Catálogo con padre: ParentId se guarda en una columna con nombre propio (CountryId, StateId).</summary>
internal abstract class HierarchicalCatalogConfiguration<T, TParent>(
    string tableName,
    string parentColumn,
    Expression<Func<TParent, IEnumerable<T>?>>? inverseNavigation = null) : CatalogEntityConfiguration<T>(tableName)
    where T : HierarchicalCatalogEntity<TParent>
    where TParent : CatalogEntity
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.ParentId).HasColumnName(parentColumn);
        builder.HasOne(e => e.Parent).WithMany(inverseNavigation).HasForeignKey(e => e.ParentId);
    }

    protected override void ConfigureUniqueness(EntityTypeBuilder<T> builder)
    {
        builder.HasIndex(e => new { e.ParentId, e.Name }).IsUnique().HasFilter(NotDeleted);
        builder.HasIndex(e => new { e.ParentId, e.Code }).IsUnique()
            .HasFilter(NotNullAndNotDeleted(nameof(CatalogEntity.Code)));
    }
}

internal sealed class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("Statuses", Schemas.Catalog);
        builder.ConfigureBase();
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
    }
}

internal sealed class GenderConfiguration() : CatalogEntityConfiguration<Gender>("Genders");
internal sealed class RaceConfiguration() : CatalogEntityConfiguration<Race>("Races");
internal sealed class MaritalStatusConfiguration() : CatalogEntityConfiguration<MaritalStatus>("MaritalStatuses");
internal sealed class ReligionConfiguration() : CatalogEntityConfiguration<Religion>("Religions");
internal sealed class CountryConfiguration() : CatalogEntityConfiguration<Country>("Countries");
internal sealed class AreaCodeConfiguration() : CatalogEntityConfiguration<AreaCode>("AreaCodes");
internal sealed class EducationLevelConfiguration() : CatalogEntityConfiguration<EducationLevel>("EducationLevels");
internal sealed class ProfessionConfiguration() : CatalogEntityConfiguration<Profession>("Professions");
internal sealed class OccupationConfiguration() : CatalogEntityConfiguration<Occupation>("Occupations");
internal sealed class KinshipConfiguration() : CatalogEntityConfiguration<Kinship>("Kinships");

internal sealed class StateConfiguration()
    : HierarchicalCatalogConfiguration<State, Country>("States", "CountryId", c => c.States);

internal sealed class MunicipalityConfiguration()
    : HierarchicalCatalogConfiguration<Municipality, State>("Municipalities", "StateId", s => s.Municipalities);
