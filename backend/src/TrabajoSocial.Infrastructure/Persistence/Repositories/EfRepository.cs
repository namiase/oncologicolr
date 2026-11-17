using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Infrastructure.Persistence.Repositories;

internal sealed class EfRepository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
    private readonly DbSet<T> set = context.Set<T>();

    public IQueryable<T> Query() => set.AsNoTracking();

    public IQueryable<T> QueryTracked() => set;

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) => await set.AddAsync(entity, ct);

    /// <summary>
    /// Borrado lógico: marca IsDeleted en lugar de usar DbSet.Remove. Así EF no intenta
    /// cortar las relaciones con dependientes ya cargados (p. ej. los tokens de un usuario),
    /// que con borrado lógico nunca se eliminan físicamente. El interceptor completa el
    /// estatus "Eliminado" y la auditoría.
    /// </summary>
    public void Remove(T entity)
    {
        var entry = context.Entry(entity);
        switch (entry.State)
        {
            case EntityState.Added:
                // Aún no existe en la base de datos: basta con no insertarlo.
                entry.State = EntityState.Detached;
                return;

            case EntityState.Detached:
                // Solo esta entidad (Attach recorrería también sus relaciones cargadas).
                entry.State = EntityState.Unchanged;
                break;
        }

        entity.IsDeleted = true;
        entry.Property(e => e.IsDeleted).IsModified = true;
    }
}
