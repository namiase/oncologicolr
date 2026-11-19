using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Constants;

namespace TrabajoSocial.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Completa los campos de auditoría y convierte los borrados en borrados lógicos
/// (IsDeleted = true y, si aplica, estatus Eliminado).
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUserService currentUser, TimeProvider time)
    : SaveChangesInterceptor
{
    public const string SystemUser = "system";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = time.GetUtcNow().UtcDateTime;
        var user = currentUser.Username ?? SystemUser;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    entry.Entity.UpdatedAt = null;
                    entry.Entity.UpdatedBy = null;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Deleted:
                    // Remove() directo sobre el DbContext: se convierte en borrado lógico. Se pasa
                    // por Unchanged para actualizar solo estas columnas (Modified marcaría todas
                    // y, con una entidad "stub", escribiría valores vacíos en la fila).
                    entry.State = EntityState.Unchanged;
                    entry.Entity.IsDeleted = true;
                    entry.Property(e => e.IsDeleted).IsModified = true;
                    if (entry.Entity is IHasStatus withStatus)
                    {
                        withStatus.StatusId = StatusIds.Deleted;
                        entry.Property(nameof(IHasStatus.StatusId)).IsModified = true;
                    }

                    MarkUpdated(entry, now, user);
                    break;

                case EntityState.Modified:
                    // Borrado lógico hecho por el repositorio (IsDeleted pasó a true).
                    if (entry.Entity.IsDeleted && entry.Property(e => e.IsDeleted).IsModified
                        && entry.Entity is IHasStatus deleted)
                    {
                        deleted.StatusId = StatusIds.Deleted;
                    }

                    // Un login (anónimo) que solo actualiza el último acceso o los intentos
                    // fallidos no es una edición del registro: se conserva quién lo editó.
                    if (currentUser.Username is null && OnlyNotAuditedChanges(entry))
                    {
                        break;
                    }

                    MarkUpdated(entry, now, user);
                    break;
            }
        }
    }

    private static bool OnlyNotAuditedChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry)
    {
        var modified = entry.Properties.Where(p => p.IsModified).ToList();
        return modified.Count > 0
               && modified.All(p => p.Metadata.PropertyInfo?.IsDefined(typeof(NotAuditedAttribute), inherit: true) == true);
    }

    private static void MarkUpdated(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry, DateTime now, string user)
    {
        entry.Entity.UpdatedAt = now;
        entry.Entity.UpdatedBy = user;

        // Los datos de creación nunca se sobrescriben.
        entry.Property(e => e.CreatedAt).IsModified = false;
        entry.Property(e => e.CreatedBy).IsModified = false;
    }
}
