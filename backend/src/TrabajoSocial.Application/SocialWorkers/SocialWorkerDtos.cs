using System.ComponentModel.DataAnnotations;

namespace TrabajoSocial.Application.SocialWorkers;

public sealed record SocialWorkerDto
{
    public Guid Id { get; init; }
    public string FirstNames { get; init; } = string.Empty;
    public string LastNames { get; init; } = string.Empty;
    public string IdentityCard { get; init; } = string.Empty;
    public string AntsRegistrationNumber { get; init; } = string.Empty;
    public string SocialWorkersAssociation { get; init; } = string.Empty;
    public short GraduationYear { get; init; }
    public Guid? UserId { get; init; }
    public string? Username { get; init; }
    public Guid StatusId { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class SocialWorkerUpsertDto
{
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [MaxLength(100, ErrorMessage = "Los nombres admiten máximo {1} caracteres.")]
    public string FirstNames { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [MaxLength(100, ErrorMessage = "Los apellidos admiten máximo {1} caracteres.")]
    public string LastNames { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [MaxLength(15, ErrorMessage = "La cédula admite máximo {1} caracteres.")]
    public string IdentityCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "El N° de registro ANTS es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El N° de registro ANTS admite máximo {1} caracteres.")]
    public string AntsRegistrationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El Colegio de Trabajadores Sociales es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El colegio admite máximo {1} caracteres.")]
    public string SocialWorkersAssociation { get; set; } = string.Empty;

    [Range(1950, 2100, ErrorMessage = "El año de grado no es válido.")]
    public short GraduationYear { get; set; }

    [Required(ErrorMessage = "El estatus es obligatorio.")]
    public Guid StatusId { get; set; }

}
