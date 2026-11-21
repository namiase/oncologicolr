using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Entities.Catalogs;
using TrabajoSocial.Domain.Entities.SocialWorkers;

namespace TrabajoSocial.Domain.Entities.Reports;

/// <summary>
/// Informe social: la "Entrevista preliminar" del paciente oncológico. Reproduce el
/// formulario en papel (2 hojas): la identificación del paciente y la evaluación social.
/// </summary>
public class SocialReport : BaseEntity
{
    // ---- Encabezado ----

    /// <summary>N° de historia clínica.</summary>
    public string MedicalRecordNumber { get; set; } = string.Empty;

    /// <summary>Fecha de ingreso.</summary>
    public DateOnly AdmissionDate { get; set; }

    public Guid? ReligionId { get; set; }
    public Religion? Religion { get; set; }

    // ---- Identificación del paciente ----

    public string LastNames { get; set; } = string.Empty;
    public string FirstNames { get; set; } = string.Empty;

    /// <summary>Cédula, formato V-12345678 / E-12345678.</summary>
    public string? IdentityCard { get; set; }
    public string? PassportNumber { get; set; }

    /// <summary>Lugar de nacimiento (ciudad, localidad).</summary>
    public string? BirthPlace { get; set; }
    public Guid? BirthCountryId { get; set; }
    public Country? BirthCountry { get; set; }
    public Guid? BirthStateId { get; set; }
    public State? BirthState { get; set; }
    public Guid? BirthMunicipalityId { get; set; }
    public Municipality? BirthMunicipality { get; set; }

    /// <summary>Fecha de nacimiento. La edad se calcula contra la fecha de ingreso.</summary>
    public DateOnly? BirthDate { get; set; }

    public Guid? GenderId { get; set; }
    public Gender? Gender { get; set; }
    public Guid? RaceId { get; set; }
    public Race? Race { get; set; }
    public Guid? MaritalStatusId { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public Guid? EducationLevelId { get; set; }
    public EducationLevel? EducationLevel { get; set; }

    /// <summary>Profesión u oficio: se llena uno de los dos.</summary>
    public Guid? ProfessionId { get; set; }
    public Profession? Profession { get; set; }
    public Guid? OccupationId { get; set; }
    public Occupation? Occupation { get; set; }

    /// <summary>Dirección de habitación.</summary>
    public string? HomeAddress { get; set; }

    /// <summary>
    /// Teléfonos del paciente, del trabajo y de familiares (código de área + número).
    /// Reemplazan a las columnas Phone1, Phone2, WorkPhone, ContactPhone y ContactKinshipId,
    /// que se conservan en la tabla sin uso (ver SocialReportConfiguration).
    /// </summary>
    public ICollection<SocialReportPhone> Phones { get; set; } = [];

    // ---- Seguridad social y referencia ----

    public bool? HasIvss { get; set; }
    public bool? HasIpasme { get; set; }
    public string? InsurancePolicy { get; set; }
    public string? OtherSocialSecurity { get; set; }
    public string? ReferredBy { get; set; }

    // ---- Trabajo ----

    public string? Workplace { get; set; }
    public string? WorkAddress { get; set; }
    public string? WorkSchedule { get; set; }

    // ---- Paciente de pediatría ----

    public bool IsPediatric { get; set; }
    public string? GuardianFullName { get; set; }
    public string? GuardianIdentityCard { get; set; }
    public Guid? GuardianKinshipId { get; set; }
    public Kinship? GuardianKinship { get; set; }

    // ---- Familiares o conocidos en el Distrito Capital o en su localidad ----

    public string? ContactAddress { get; set; }
    public string? ContactFullName { get; set; }

    // ---- Evaluación social (hoja 2) ----

    public string? MedicalSocialArea { get; set; }
    public string? PsychosocialArea { get; set; }
    public string? SocioeconomicArea { get; set; }
    public string? PhysicalEnvironmentArea { get; set; }
    public string? ClinicalDiagnosis { get; set; }
    public string? ReferralServices { get; set; }
    public string? Observations { get; set; }

    // ---- Trabajador(a) social tratante ----

    /// <summary>
    /// La vinculada al usuario que creó el informe. Puede faltar si lo creó un administrador sin
    /// trabajadora vinculada: al descargar el PDF, el administrador elige quién firma.
    /// </summary>
    public Guid? SocialWorkerId { get; set; }
    public SocialWorker? SocialWorker { get; set; }
}
