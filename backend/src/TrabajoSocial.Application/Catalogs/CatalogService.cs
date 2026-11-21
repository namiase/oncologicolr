using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Application.Catalogs;

/// <summary>
/// Implementación genérica del CRUD de tablas maestras. Una instancia por catálogo
/// (registrada como keyed service con la clave del catálogo).
/// </summary>
public class CatalogService<TEntity>(
    CatalogDefinition definition,
    IRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    ICatalogServiceFactory catalogs) : ICatalogService
    where TEntity : CatalogEntity, new()
{
    protected IRepository<TEntity> Repository { get; } = repository;

    public CatalogDefinition Definition { get; } = definition;

    public async Task<PagedResult<CatalogItemDto>> GetPagedAsync(CatalogQuery query, CancellationToken ct = default)
    {
        var q = Repository.Query();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(e => e.Name.Contains(search)
                             || (e.Code != null && e.Code.Contains(search))
                             || (e.Description != null && e.Description.Contains(search)));
        }

        if (query.StatusId is { } statusId)
        {
            q = q.Where(e => e.StatusId == statusId);
        }

        if (query.ParentId is { } parentId)
        {
            q = FilterByParent(q, parentId);
        }

        q = ApplySort(q, query.SortBy, query.SortDesc);
        return await Project(q).ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    public Task<CatalogItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Project(Repository.Query().Where(e => e.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(Guid? parentId, CancellationToken ct = default)
    {
        var q = Repository.Query().Where(e => e.StatusId == StatusIds.Active);
        if (parentId is { } id)
        {
            q = FilterByParent(q, id);
        }

        return await ProjectLookup(OrderForLookup(q)).ToListAsync(ct);
    }

    public async Task<CatalogItemDto> CreateAsync(CatalogUpsertDto dto, CancellationToken ct = default)
    {
        var entity = new TEntity();
        await ApplyAsync(entity, dto, currentId: null, ct);
        await Repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task<CatalogItemDto> UpdateAsync(Guid id, CatalogUpsertDto dto, CancellationToken ct = default)
    {
        var entity = await Repository.GetByIdAsync(id, ct) ?? throw NotFound();
        await ApplyAsync(entity, dto, currentId: id, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetByIdAsync(id, ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Repository.GetByIdAsync(id, ct) ?? throw NotFound();

        foreach (var child in CatalogRegistry.GetChildren(Definition.Key))
        {
            var count = await catalogs.Get(child.Key).CountByParentAsync(id, ct);
            if (count > 0)
            {
                throw new BusinessRuleException(
                    $"No se puede eliminar \"{entity.Name}\": tiene {count} registro(s) asociado(s) en {child.Name}.");
            }
        }

        Repository.Remove(entity);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct = default) => Repository.Query().CountAsync(ct);

    public virtual Task<int> CountByParentAsync(Guid parentId, CancellationToken ct = default) => Task.FromResult(0);

    public virtual async Task<Guid> FindOrCreateAsync(string name, string? code = null, CancellationToken ct = default)
    {
        var cleanName = NormalizeName(name);
        var cleanCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        if (cleanName.Length is 0 or > CatalogEntity.NameMaxLength)
        {
            throw new ValidationException("name", $"El valor debe tener entre 1 y {CatalogEntity.NameMaxLength} caracteres.");
        }

        // Se reutiliza aunque esté inactivo: es el valor que la usuaria escribió.
        var lowerName = cleanName.ToLower();
        var existing = await Repository.Query()
            .Where(e => cleanCode != null ? e.Code == cleanCode : e.Name.ToLower() == lowerName)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);
        if (existing is { } id)
        {
            return id;
        }

        var nextOrder = (await Repository.Query().MaxAsync(e => (int?)e.SortOrder, ct) ?? 0) + 1;
        var created = await CreateAsync(new CatalogUpsertDto
        {
            Name = cleanName,
            Code = cleanCode,
            SortOrder = Math.Min(nextOrder, 9999),
            StatusId = StatusIds.Active
        }, ct);
        return created.Id;
    }

    /// <summary>Sin espacios repetidos y con la primera letra en mayúscula ("hermano de crianza" → "Hermano de crianza").</summary>
    private static string NormalizeName(string value)
    {
        var compact = string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return compact.Length == 0 ? compact : char.ToUpper(compact[0]) + compact[1..];
    }

    // ---- Puntos de extensión para catálogos jerárquicos ----

    protected virtual IQueryable<TEntity> FilterByParent(IQueryable<TEntity> query, Guid parentId) => query;

    protected virtual Task ValidateParentAsync(Guid? parentId, CancellationToken ct) => Task.CompletedTask;

    protected virtual void AssignParent(TEntity entity, Guid? parentId)
    {
    }

    /// <summary>Ámbito donde Nombre y Código deben ser únicos (todo el catálogo, o por padre).</summary>
    protected virtual IQueryable<TEntity> UniquenessScope(IQueryable<TEntity> query, Guid? parentId) => query;

    protected virtual IQueryable<TEntity> ApplySort(IQueryable<TEntity> query, string? sortBy, bool desc)
        => sortBy?.ToLowerInvariant() switch
        {
            "code" => Order(query, e => e.Code, desc).ThenBy(e => e.Name),
            "name" => Order(query, e => e.Name, desc),
            "status" => Order(query, e => e.Status.Name, desc).ThenBy(e => e.Name),
            "createdat" => Order(query, e => e.CreatedAt, desc),
            "updatedat" => Order(query, e => e.UpdatedAt, desc),
            _ => Order(query, e => e.SortOrder, desc).ThenBy(e => e.Name)
        };

    protected virtual IQueryable<CatalogItemDto> Project(IQueryable<TEntity> query)
        => query.Select(e => new CatalogItemDto
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            SortOrder = e.SortOrder,
            StatusId = e.StatusId,
            StatusName = e.Status.Name,
            CreatedAt = e.CreatedAt,
            CreatedBy = e.CreatedBy,
            UpdatedAt = e.UpdatedAt,
            UpdatedBy = e.UpdatedBy
        });

    protected virtual IOrderedQueryable<TEntity> OrderForLookup(IQueryable<TEntity> query)
        => query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name);

    protected virtual IQueryable<LookupItemDto> ProjectLookup(IQueryable<TEntity> query)
        => query.Select(e => new LookupItemDto(e.Id, e.Name, e.Code, null));

    protected static IOrderedQueryable<TEntity> Order<TKey>(
        IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> key, bool desc)
        => desc ? query.OrderByDescending(key) : query.OrderBy(key);

    private async Task ApplyAsync(TEntity entity, CatalogUpsertDto dto, Guid? currentId, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();
        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (name.Length == 0)
        {
            throw new ValidationException("name", "El nombre es obligatorio.");
        }

        if (!StatusIds.Assignable.Contains(dto.StatusId))
        {
            throw new ValidationException("statusId", "El estatus seleccionado no es válido.");
        }

        await ValidateParentAsync(dto.ParentId, ct);

        var scope = UniquenessScope(Repository.Query(), dto.ParentId);
        if (await scope.AnyAsync(e => e.Name == name && e.Id != currentId, ct))
        {
            throw new ConflictException(
                $"Ya existe un registro con el nombre \"{name}\" en {Definition.Name}.", "name");
        }

        if (code is not null && await scope.AnyAsync(e => e.Code == code && e.Id != currentId, ct))
        {
            throw new ConflictException(
                $"Ya existe un registro con el código \"{code}\" en {Definition.Name}.", "code");
        }

        entity.Code = code;
        entity.Name = name;
        entity.Description = description;
        entity.SortOrder = dto.SortOrder;
        entity.StatusId = dto.StatusId;
        AssignParent(entity, dto.ParentId);
    }

    private NotFoundException NotFound()
        => new($"El registro solicitado no existe en {Definition.Name}.");
}
