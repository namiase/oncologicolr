using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Infrastructure.Persistence.Configurations;

internal static class EntityTypeBuilderExtensions
{
    /// <summary>Columnas comunes de BaseEntity (clave y auditoría).</summary>
    public static EntityTypeBuilder<T> ConfigureBase<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        return builder;
    }

    /// <summary>Filtro SQL de índice único que ignora los registros eliminados.</summary>
    public const string NotDeleted = "[IsDeleted] = 0";

    public static string NotNullAndNotDeleted(string column) => $"[{column}] IS NOT NULL AND [IsDeleted] = 0";
}
