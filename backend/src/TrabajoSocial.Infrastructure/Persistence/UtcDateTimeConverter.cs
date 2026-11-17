using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TrabajoSocial.Infrastructure.Persistence;

/// <summary>
/// Todas las fechas se guardan en UTC. SQL Server no almacena el "Kind", así que al leer
/// se marca como UTC para que la API serialice "…Z" y el cliente convierta a hora local.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
