namespace TrabajoSocial.Domain.Entities.Reports;

/// <summary>
/// Longitudes máximas del informe. Garantizan que el PDF quepa en 2 hojas Carta.
/// Se replican en el frontend (features/reports-create/reportSchema.ts): si cambia un valor,
/// cambiarlo en ambos lados.
/// </summary>
public static class SocialReportLimits
{
    // Identificación (hoja 1): cada valor debe caber en su línea del formulario.
    public const int MedicalRecordNumber = 20;
    public const int Names = 60;
    public const int IdentityCard = 15;
    public const int PassportNumber = 30;
    public const int BirthPlace = 40;
    /// <summary>Direcciones: dos renglones del formulario.</summary>
    public const int Address = 180;
    /// <summary>Número de teléfono sin el código de área.</summary>
    public const int PhoneNumber = 7;
    /// <summary>Cuántos teléfonos caben en cada línea del formulario impreso.</summary>
    public const int PatientPhones = 4;
    public const int WorkPhones = 2;
    public const int ContactPhones = 6;
    /// <summary>Valor escrito en "Otro" (se agrega a la tabla maestra).</summary>
    public const int OtherValue = 40;
    /// <summary>Parentesco escrito en "Otro": va junto a cada teléfono de familiares.</summary>
    public const int OtherKinship = 25;
    /// <summary>Póliza y "otros" de la seguridad social.</summary>
    public const int SocialSecurity = 30;
    /// <summary>Campos de una línea completa: referido por, lugar de trabajo, nombre del familiar.</summary>
    public const int FullLine = 100;
    public const int WorkSchedule = 60;
    public const int GuardianName = 50;

    // Evaluación social (hoja 2).
    public const int MedicalSocialArea = 700;
    public const int PsychosocialArea = 1000;
    public const int SocioeconomicArea = 1500;
    public const int PhysicalEnvironmentArea = 1000;
    public const int ClinicalDiagnosis = 250;
    public const int ReferralServices = 250;
    public const int Observations = 500;
}
