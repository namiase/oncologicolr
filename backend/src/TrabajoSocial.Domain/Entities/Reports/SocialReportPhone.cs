using TrabajoSocial.Domain.Common;
using TrabajoSocial.Domain.Entities.Catalogs;

namespace TrabajoSocial.Domain.Entities.Reports;

/// <summary>A quién pertenece un teléfono del informe.</summary>
public enum ReportPhoneKind
{
    /// <summary>Teléfonos del paciente.</summary>
    Patient = 1,

    /// <summary>Teléfonos del lugar de trabajo.</summary>
    Work = 2,

    /// <summary>Teléfonos de familiares o conocidos, con su parentesco.</summary>
    Contact = 3
}

/// <summary>Teléfono del informe: código de área (tabla maestra) + número de 7 dígitos.</summary>
public class SocialReportPhone : BaseEntity
{
    public Guid SocialReportId { get; set; }
    public SocialReport SocialReport { get; set; } = null!;

    public ReportPhoneKind Kind { get; set; }

    public Guid AreaCodeId { get; set; }
    public AreaCode AreaCode { get; set; } = null!;

    /// <summary>Número sin el código de área (7 dígitos).</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Parentesco (solo en los teléfonos de familiares o conocidos).</summary>
    public Guid? KinshipId { get; set; }
    public Kinship? Kinship { get; set; }

    /// <summary>Orden dentro de su grupo (como se escribieron).</summary>
    public int SortOrder { get; set; }
}
