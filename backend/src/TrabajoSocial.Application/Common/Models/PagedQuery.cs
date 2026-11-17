using System.ComponentModel.DataAnnotations;

namespace TrabajoSocial.Application.Common.Models;

/// <summary>Parámetros comunes de listado: búsqueda, estatus, paginación y orden.</summary>
public class PagedQuery
{
    public const int MaxPageSize = 100;

    [MaxLength(100)]
    public string? Search { get; set; }

    public Guid? StatusId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor o igual a 1.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int PageSize { get; set; } = 10;

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
