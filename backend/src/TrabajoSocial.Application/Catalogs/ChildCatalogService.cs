using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Exceptions;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Application.Catalogs;

/// <summary>CRUD de catálogos que dependen de un catálogo padre (Estado → País, Municipio → Estado).</summary>
public sealed class ChildCatalogService<TEntity, TParent>(
    CatalogDefinition definition,
    IRepository<TEntity> repository,
    IRepository<TParent> parents,
    IUnitOfWork unitOfWork,
    ICatalogServiceFactory catalogs) : CatalogService<TEntity>(definition, repository, unitOfWork, catalogs)
    where TEntity : HierarchicalCatalogEntity<TParent>, new()
    where TParent : CatalogEntity
{
    public override Task<int> CountByParentAsync(Guid parentId, CancellationToken ct = default)
        => Repository.Query().CountAsync(e => e.ParentId == parentId, ct);

    /// <summary>Los catálogos con padre (estados, municipios) no admiten "Otro": el padre sería ambiguo.</summary>
    public override Task<Guid> FindOrCreateAsync(string name, string? code = null, CancellationToken ct = default)
        => throw new NotSupportedException($"{Definition.Name} no admite valores nuevos desde los formularios.");

    protected override IQueryable<TEntity> FilterByParent(IQueryable<TEntity> query, Guid parentId)
        => query.Where(e => e.ParentId == parentId);

    protected override async Task ValidateParentAsync(Guid? parentId, CancellationToken ct)
    {
        var parentName = ParentSingularName();
        if (parentId is null || parentId == Guid.Empty)
        {
            throw new ValidationException("parentId", $"Debe seleccionar el {parentName}.");
        }

        if (!await parents.Query().AnyAsync(p => p.Id == parentId, ct))
        {
            throw new ValidationException("parentId", $"El {parentName} seleccionado no existe.");
        }
    }

    protected override void AssignParent(TEntity entity, Guid? parentId) => entity.ParentId = parentId!.Value;

    protected override IQueryable<TEntity> UniquenessScope(IQueryable<TEntity> query, Guid? parentId)
        => query.Where(e => e.ParentId == parentId);

    protected override IQueryable<TEntity> ApplySort(IQueryable<TEntity> query, string? sortBy, bool desc)
        => sortBy?.ToLowerInvariant() switch
        {
            "parent" => Order(query, e => e.Parent.Name, desc).ThenBy(e => e.Name),
            // Sin orden explícito: agrupado por padre (p. ej. país), respetando su orden (Venezuela primero).
            null or "" or "sortorder" => OrderByParent(query),
            _ => base.ApplySort(query, sortBy, desc)
        };

    protected override IOrderedQueryable<TEntity> OrderForLookup(IQueryable<TEntity> query) => OrderByParent(query);

    private static IOrderedQueryable<TEntity> OrderByParent(IQueryable<TEntity> query)
        => query.OrderBy(e => e.Parent.SortOrder).ThenBy(e => e.Parent.Name).ThenBy(e => e.SortOrder).ThenBy(e => e.Name);

    protected override IQueryable<CatalogItemDto> Project(IQueryable<TEntity> query)
        => query.Select(e => new CatalogItemDto
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            SortOrder = e.SortOrder,
            StatusId = e.StatusId,
            StatusName = e.Status.Name,
            ParentId = e.ParentId,
            ParentName = e.Parent.Name,
            CreatedAt = e.CreatedAt,
            CreatedBy = e.CreatedBy,
            UpdatedAt = e.UpdatedAt,
            UpdatedBy = e.UpdatedBy
        });

    protected override IQueryable<LookupItemDto> ProjectLookup(IQueryable<TEntity> query)
        => query.Select(e => new LookupItemDto(e.Id, e.Name, e.Code, e.ParentId, e.Parent.Name));

    private string ParentSingularName()
        => CatalogRegistry.TryGet(Definition.ParentKey!, out var parent)
            ? parent.SingularName.ToLowerInvariant()
            : "registro padre";
}
