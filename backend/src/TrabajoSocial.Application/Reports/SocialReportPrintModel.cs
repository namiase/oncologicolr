namespace TrabajoSocial.Application.Reports;

/// <summary>Genera el PDF de la entrevista preliminar: siempre 2 hojas Carta.</summary>
public interface ISocialReportPdfRenderer
{
    byte[] Render(SocialReportPrintModel model);
}

/// <summary>Opción marcable del formulario impreso, p. ej. "F _X_  M ___".</summary>
public sealed record PrintOption(string Label, bool Selected);

/// <summary>
/// Informe listo para imprimir: los catálogos ya traen su nombre y las fechas su formato.
/// El renderer solo se encarga de la diagramación.
/// </summary>
public sealed record SocialReportPrintModel
{
    // ---- Encabezado ----
    public string CompanyName { get; init; } = string.Empty;
    /// <summary>Membrete (PNG/JPG). Si es null se usa <see cref="Logo"/> y el nombre de la empresa.</summary>
    public byte[]? HeaderImage { get; init; }
    public byte[]? Logo { get; init; }

    public string MedicalRecordNumber { get; init; } = string.Empty;
    public string AdmissionDate { get; init; } = string.Empty;
    public string? Religion { get; init; }

    // ---- Identificación ----
    public string PatientName { get; init; } = string.Empty;
    /// <summary>Número de cédula sin la letra (la nacionalidad va en <see cref="Nationality"/>).</summary>
    public string? IdentityNumber { get; init; }
    public IReadOnlyList<PrintOption> Nationality { get; init; } = [];
    public string? PassportNumber { get; init; }
    public string? BirthPlace { get; init; }
    public string? BirthCountry { get; init; }
    public string? BirthState { get; init; }
    public string? BirthMunicipality { get; init; }
    public string? BirthDate { get; init; }
    public string? Age { get; init; }
    public IReadOnlyList<PrintOption> Gender { get; init; } = [];
    public string? Race { get; init; }
    public IReadOnlyList<PrintOption> MaritalStatus { get; init; } = [];
    public string? EducationLevel { get; init; }
    public string? ProfessionOrOccupation { get; init; }
    public string? HomeAddress { get; init; }
    /// <summary>"0414-1234567 / 0212-8701234": código de área + número, separados por "/".</summary>
    public string? Phones { get; init; }

    // ---- Seguridad social y referencia ----
    public IReadOnlyList<PrintOption> Ivss { get; init; } = [];
    public string? Ipasme { get; init; }
    public string? InsurancePolicy { get; init; }
    public string? OtherSocialSecurity { get; init; }
    public string? ReferredBy { get; init; }

    // ---- Trabajo ----
    public string? Workplace { get; init; }
    public string? WorkAddress { get; init; }
    public string? WorkPhones { get; init; }
    public string? WorkSchedule { get; init; }

    // ---- Pediatría ----
    public bool IsPediatric { get; init; }
    public string? GuardianFullName { get; init; }
    public string? GuardianIdentityCard { get; init; }
    public string? GuardianKinship { get; init; }

    // ---- Familiares o conocidos ----
    public string? ContactAddress { get; init; }
    public string? ContactFullName { get; init; }
    /// <summary>"0414-1234567 (Hermano(a)) / 0416-5551234 (Vecina)".</summary>
    public string? ContactPhoneAndKinship { get; init; }

    // ---- Evaluación social ----
    public string? MedicalSocialArea { get; init; }
    public string? PsychosocialArea { get; init; }
    public string? SocioeconomicArea { get; init; }
    public string? PhysicalEnvironmentArea { get; init; }
    public string? ClinicalDiagnosis { get; init; }
    public string? ReferralServices { get; init; }
    public string? Observations { get; init; }

    // ---- Trabajador(a) social tratante (null: la firma queda en blanco para llenarla a mano) ----
    public string? SocialWorkerName { get; init; }
    public string? SocialWorkerIdentityCard { get; init; }
    public string? SocialWorkerAntsNumber { get; init; }
    public string? SocialWorkerAssociation { get; init; }
}
