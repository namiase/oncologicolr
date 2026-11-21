using System.ComponentModel.DataAnnotations;
using L = TrabajoSocial.Domain.Entities.Reports.SocialReportLimits;

namespace TrabajoSocial.Application.Reports;

/// <summary>Informe completo, para editarlo.</summary>
public sealed record SocialReportDto
{
    public Guid Id { get; init; }

    public string MedicalRecordNumber { get; init; } = string.Empty;
    public DateOnly AdmissionDate { get; init; }
    public Guid? ReligionId { get; init; }

    public string LastNames { get; init; } = string.Empty;
    public string FirstNames { get; init; } = string.Empty;
    public string? IdentityCard { get; init; }
    public string? PassportNumber { get; init; }
    public string? BirthPlace { get; init; }
    public Guid? BirthCountryId { get; init; }
    public Guid? BirthStateId { get; init; }
    public Guid? BirthMunicipalityId { get; init; }
    public DateOnly? BirthDate { get; init; }
    public Guid? GenderId { get; init; }
    public Guid? RaceId { get; init; }
    public Guid? MaritalStatusId { get; init; }
    public Guid? EducationLevelId { get; init; }
    public Guid? ProfessionId { get; init; }
    public Guid? OccupationId { get; init; }
    public string? HomeAddress { get; init; }
    public IReadOnlyList<SocialReportPhoneDto> Phones { get; init; } = [];

    public bool? HasIvss { get; init; }
    public bool? HasIpasme { get; init; }
    public string? InsurancePolicy { get; init; }
    public string? OtherSocialSecurity { get; init; }
    public string? ReferredBy { get; init; }

    public string? Workplace { get; init; }
    public string? WorkAddress { get; init; }
    public IReadOnlyList<SocialReportPhoneDto> WorkPhones { get; init; } = [];
    public string? WorkSchedule { get; init; }

    public bool IsPediatric { get; init; }
    public string? GuardianFullName { get; init; }
    public string? GuardianIdentityCard { get; init; }
    public Guid? GuardianKinshipId { get; init; }

    public string? ContactAddress { get; init; }
    public string? ContactFullName { get; init; }
    public IReadOnlyList<SocialReportPhoneDto> ContactPhones { get; init; } = [];

    public string? MedicalSocialArea { get; init; }
    public string? PsychosocialArea { get; init; }
    public string? SocioeconomicArea { get; init; }
    public string? PhysicalEnvironmentArea { get; init; }
    public string? ClinicalDiagnosis { get; init; }
    public string? ReferralServices { get; init; }
    public string? Observations { get; init; }

    public Guid? SocialWorkerId { get; init; }
    public string? SocialWorkerName { get; init; }

    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>Fila del listado de informes.</summary>
public sealed record SocialReportListItemDto(
    Guid Id,
    string MedicalRecordNumber,
    string PatientName,
    string? IdentityCard,
    DateOnly AdmissionDate,
    Guid? SocialWorkerId,
    string? SocialWorkerName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Teléfono guardado: código de área + número (y parentesco, en los de familiares).</summary>
public sealed record SocialReportPhoneDto(Guid AreaCodeId, string AreaCode, string Number, Guid? KinshipId, string? Kinship);

/// <summary>Datos de la trabajadora social que firma el informe.</summary>
public sealed record SocialWorkerSummaryDto(
    Guid Id,
    string FullName,
    string IdentityCard,
    string AntsRegistrationNumber,
    string SocialWorkersAssociation);

/// <summary>Contexto del formulario para el usuario actual.</summary>
/// <param name="SocialWorker">Trabajadora social vinculada al usuario (se asigna sola como tratante).</param>
/// <param name="CanChooseSocialWorker">Administrador: elige quién firma al descargar el PDF y ve todos los informes.</param>
public sealed record SocialReportContextDto(SocialWorkerSummaryDto? SocialWorker, bool CanChooseSocialWorker);

/// <summary>PDF generado, listo para descargar.</summary>
public sealed record SocialReportFile(byte[] Content, string FileName);

/// <summary>Datos para crear o editar un informe.</summary>
public sealed class SocialReportUpsertDto
{
    [Required(ErrorMessage = "El N° de historia clínica es obligatorio.")]
    [MaxLength(L.MedicalRecordNumber, ErrorMessage = "El N° de historia clínica admite máximo {1} caracteres.")]
    public string MedicalRecordNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
    public DateOnly? AdmissionDate { get; set; }

    public Guid? ReligionId { get; set; }

    [MaxLength(L.OtherValue, ErrorMessage = "La religión admite máximo {1} caracteres.")]
    public string? ReligionOther { get; set; }

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [MaxLength(L.Names, ErrorMessage = "Los apellidos admiten máximo {1} caracteres.")]
    public string LastNames { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [MaxLength(L.Names, ErrorMessage = "Los nombres admiten máximo {1} caracteres.")]
    public string FirstNames { get; set; } = string.Empty;

    [MaxLength(L.IdentityCard, ErrorMessage = "La cédula admite máximo {1} caracteres.")]
    public string? IdentityCard { get; set; }

    [MaxLength(L.PassportNumber, ErrorMessage = "El N° de pasaporte admite máximo {1} caracteres.")]
    public string? PassportNumber { get; set; }

    [MaxLength(L.BirthPlace, ErrorMessage = "El lugar de nacimiento admite máximo {1} caracteres.")]
    public string? BirthPlace { get; set; }

    public Guid? BirthCountryId { get; set; }
    public Guid? BirthStateId { get; set; }
    public Guid? BirthMunicipalityId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Guid? GenderId { get; set; }
    public Guid? RaceId { get; set; }

    [MaxLength(L.OtherValue, ErrorMessage = "La raza admite máximo {1} caracteres.")]
    public string? RaceOther { get; set; }

    public Guid? MaritalStatusId { get; set; }

    [MaxLength(L.OtherValue, ErrorMessage = "El estado civil admite máximo {1} caracteres.")]
    public string? MaritalStatusOther { get; set; }

    public Guid? EducationLevelId { get; set; }

    [MaxLength(L.OtherValue, ErrorMessage = "El grado de instrucción admite máximo {1} caracteres.")]
    public string? EducationLevelOther { get; set; }

    public Guid? ProfessionId { get; set; }

    [MaxLength(L.OtherValue, ErrorMessage = "La profesión admite máximo {1} caracteres.")]
    public string? ProfessionOther { get; set; }

    public Guid? OccupationId { get; set; }

    [MaxLength(L.OtherValue, ErrorMessage = "El oficio admite máximo {1} caracteres.")]
    public string? OccupationOther { get; set; }

    [MaxLength(L.Address, ErrorMessage = "La dirección admite máximo {1} caracteres.")]
    public string? HomeAddress { get; set; }

    public List<SocialReportPhoneInput> Phones { get; set; } = [];

    public bool? HasIvss { get; set; }
    public bool? HasIpasme { get; set; }

    [MaxLength(L.SocialSecurity, ErrorMessage = "La póliza admite máximo {1} caracteres.")]
    public string? InsurancePolicy { get; set; }

    [MaxLength(L.SocialSecurity, ErrorMessage = "Otros admite máximo {1} caracteres.")]
    public string? OtherSocialSecurity { get; set; }

    [MaxLength(L.FullLine, ErrorMessage = "Referido por admite máximo {1} caracteres.")]
    public string? ReferredBy { get; set; }

    [MaxLength(L.FullLine, ErrorMessage = "El lugar de trabajo admite máximo {1} caracteres.")]
    public string? Workplace { get; set; }

    [MaxLength(L.Address, ErrorMessage = "La dirección del trabajo admite máximo {1} caracteres.")]
    public string? WorkAddress { get; set; }

    public List<SocialReportPhoneInput> WorkPhones { get; set; } = [];

    [MaxLength(L.WorkSchedule, ErrorMessage = "El horario admite máximo {1} caracteres.")]
    public string? WorkSchedule { get; set; }

    public bool IsPediatric { get; set; }

    [MaxLength(L.GuardianName, ErrorMessage = "El nombre del representante admite máximo {1} caracteres.")]
    public string? GuardianFullName { get; set; }

    [MaxLength(L.IdentityCard, ErrorMessage = "La cédula admite máximo {1} caracteres.")]
    public string? GuardianIdentityCard { get; set; }

    public Guid? GuardianKinshipId { get; set; }

    [MaxLength(L.OtherKinship, ErrorMessage = "El parentesco admite máximo {1} caracteres.")]
    public string? GuardianKinshipOther { get; set; }

    [MaxLength(L.Address, ErrorMessage = "La dirección admite máximo {1} caracteres.")]
    public string? ContactAddress { get; set; }

    [MaxLength(L.FullLine, ErrorMessage = "El nombre admite máximo {1} caracteres.")]
    public string? ContactFullName { get; set; }

    /// <summary>Teléfonos de familiares o conocidos, cada uno con su parentesco.</summary>
    public List<SocialReportPhoneInput> ContactPhones { get; set; } = [];

    [MaxLength(L.MedicalSocialArea, ErrorMessage = "El área médico social admite máximo {1} caracteres.")]
    public string? MedicalSocialArea { get; set; }

    [MaxLength(L.PsychosocialArea, ErrorMessage = "El área psicosocial admite máximo {1} caracteres.")]
    public string? PsychosocialArea { get; set; }

    [MaxLength(L.SocioeconomicArea, ErrorMessage = "El área socioeconómica admite máximo {1} caracteres.")]
    public string? SocioeconomicArea { get; set; }

    [MaxLength(L.PhysicalEnvironmentArea, ErrorMessage = "El área físico ambiental admite máximo {1} caracteres.")]
    public string? PhysicalEnvironmentArea { get; set; }

    [MaxLength(L.ClinicalDiagnosis, ErrorMessage = "El diagnóstico clínico admite máximo {1} caracteres.")]
    public string? ClinicalDiagnosis { get; set; }

    [MaxLength(L.ReferralServices, ErrorMessage = "Servicios a que refieren admite máximo {1} caracteres.")]
    public string? ReferralServices { get; set; }

    [MaxLength(L.Observations, ErrorMessage = "Las observaciones admiten máximo {1} caracteres.")]
    public string? Observations { get; set; }
}

/// <summary>
/// Teléfono a guardar. El código de área es uno de la tabla maestra (<see cref="AreaCodeId"/>)
/// o uno nuevo escrito en "Otro" (<see cref="AreaCodeOther"/>, p. ej. 0261), que se agrega a ella.
/// Lo mismo con el parentesco, que solo aplica a los teléfonos de familiares.
/// </summary>
public sealed class SocialReportPhoneInput
{
    public Guid? AreaCodeId { get; set; }

    [MaxLength(4, ErrorMessage = "El código de área tiene 4 dígitos.")]
    public string? AreaCodeOther { get; set; }

    [MaxLength(20, ErrorMessage = "El número admite máximo {1} caracteres.")]
    public string? Number { get; set; }

    public Guid? KinshipId { get; set; }

    [MaxLength(L.OtherKinship, ErrorMessage = "El parentesco admite máximo {1} caracteres.")]
    public string? KinshipOther { get; set; }
}
