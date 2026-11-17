namespace TrabajoSocial.Application.Common.Models;

/// <summary>
/// Elemento para listas desplegables. ParentName permite distinguir nombres repetidos
/// (p. ej. el estado "Amazonas" existe en Venezuela, Colombia y Brasil).
/// </summary>
public sealed record LookupItemDto(Guid Id, string Name, string? Code = null, Guid? ParentId = null, string? ParentName = null);
