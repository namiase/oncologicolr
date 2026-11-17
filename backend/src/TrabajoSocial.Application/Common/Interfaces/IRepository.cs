using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Application.Common.Interfaces;

/// <summary>Repositorio genérico sobre EF Core. Los borrados son lógicos (soft delete).</summary>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>Consulta de solo lectura (AsNoTracking). Excluye registros eliminados.</summary>
    IQueryable<T> Query();

    /// <summary>Consulta con seguimiento de cambios, para modificar entidades.</summary>
    IQueryable<T> QueryTracked();

    /// <summary>Obtiene una entidad con seguimiento por Id (null si no existe o está eliminada).</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Marca la entidad para borrado lógico (lo resuelve el interceptor al guardar).</summary>
    void Remove(T entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Ejecuta <paramref name="action"/> cuando el próximo guardado se confirme. Si el guardado
    /// falla, la acción se descarta (p. ej. no revocar en memoria una sesión que en la base
    /// de datos sigue activa).
    /// </summary>
    void AfterSaveChanges(Action action);
}
