using TrabajoSocial.Domain.Constants;
using TrabajoSocial.Domain.Entities.Catalogs;

namespace TrabajoSocial.Domain.Common;

/// <summary>
/// Estructura genérica de las tablas maestras (catálogos).
/// Cada catálogo tiene su propia tabla, pero todas comparten estas columnas.
/// </summary>
public abstract class CatalogEntity : BaseEntity, IHasStatus
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 500;

    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    public Guid StatusId { get; set; } = StatusIds.Active;
    public Status Status { get; set; } = null!;
}

/// <summary>
/// Catálogo que depende de otro catálogo (p. ej. Estado → País, Municipio → Estado).
/// <see cref="ParentId"/> se mapea a una columna con nombre propio (CountryId, StateId).
/// </summary>
public abstract class HierarchicalCatalogEntity<TParent> : CatalogEntity
    where TParent : CatalogEntity
{
    public Guid ParentId { get; set; }
    public TParent Parent { get; set; } = null!;
}
