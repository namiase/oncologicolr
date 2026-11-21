using TrabajoSocial.Application.Common.Models;

namespace TrabajoSocial.Application.Catalogs;

/// <summary>Operaciones CRUD comunes a todas las tablas maestras.</summary>
public interface ICatalogService
{
    CatalogDefinition Definition { get; }

    Task<PagedResult<CatalogItemDto>> GetPagedAsync(CatalogQuery query, CancellationToken ct = default);
    Task<CatalogItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(Guid? parentId, CancellationToken ct = default);
    Task<CatalogItemDto> CreateAsync(CatalogUpsertDto dto, CancellationToken ct = default);
    Task<CatalogItemDto> UpdateAsync(Guid id, CatalogUpsertDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Cantidad de registros (no eliminados) cuyo padre es <paramref name="parentId"/>.</summary>
    Task<int> CountByParentAsync(Guid parentId, CancellationToken ct = default);

    /// <summary>
    /// Id del registro con ese código (si se indica) o ese nombre, sin distinguir mayúsculas.
    /// Si no existe, lo crea activo al final de la lista con las mismas validaciones del CRUD.
    /// Es la opción "Otro" de los formularios: el valor nuevo queda en la tabla maestra.
    /// </summary>
    Task<Guid> FindOrCreateAsync(string name, string? code = null, CancellationToken ct = default);
}

public interface ICatalogServiceFactory
{
    /// <summary>Devuelve el servicio del catálogo o lanza NotFoundException si la clave no existe.</summary>
    ICatalogService Get(string key);
}
