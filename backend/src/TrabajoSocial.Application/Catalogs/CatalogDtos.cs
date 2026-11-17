using System.ComponentModel.DataAnnotations;
using TrabajoSocial.Application.Common.Models;
using TrabajoSocial.Domain.Common;

namespace TrabajoSocial.Application.Catalogs;

public sealed record CatalogDefinitionDto(
    string Key,
    string Name,
    string SingularName,
    string Group,
    string? ParentKey,
    string? ParentSingularName,
    bool Feminine);

/// <summary>Registro de una tabla maestra tal como lo ve el cliente.</summary>
public sealed record CatalogItemDto
{
    public Guid Id { get; init; }
    public string? Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public Guid StatusId { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>Datos para crear o editar un registro de una tabla maestra.</summary>
public sealed class CatalogUpsertDto
{
    [MaxLength(CatalogEntity.CodeMaxLength, ErrorMessage = "El código admite máximo {1} caracteres.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(CatalogEntity.NameMaxLength, ErrorMessage = "El nombre admite máximo {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(CatalogEntity.DescriptionMaxLength, ErrorMessage = "La descripción admite máximo {1} caracteres.")]
    public string? Description { get; set; }

    [Range(0, 9999, ErrorMessage = "El orden debe estar entre 0 y 9999.")]
    public int SortOrder { get; set; }

    [Required(ErrorMessage = "El estatus es obligatorio.")]
    public Guid StatusId { get; set; }

    /// <summary>Obligatorio solo en catálogos jerárquicos (Estado → País, Municipio → Estado).</summary>
    public Guid? ParentId { get; set; }
}

public sealed class CatalogQuery : PagedQuery
{
    public Guid? ParentId { get; set; }
}
