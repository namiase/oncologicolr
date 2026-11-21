namespace TrabajoSocial.Application.Reports;

/// <summary>Edad del paciente a una fecha (la de ingreso). Se replica en el frontend (reportSchema.ts).</summary>
public static class PatientAge
{
    /// <summary>"45 años", "1 año", "8 meses", "12 días". Null si la fecha de nacimiento es posterior.</summary>
    public static string? Describe(DateOnly birthDate, DateOnly at)
    {
        if (birthDate > at)
        {
            return null;
        }

        var months = (at.Year - birthDate.Year) * 12 + at.Month - birthDate.Month;
        if (at.Day < birthDate.Day)
        {
            months--;
        }

        var years = months / 12;
        if (years >= 1)
        {
            return years == 1 ? "1 año" : $"{years} años";
        }

        if (months >= 1)
        {
            return months == 1 ? "1 mes" : $"{months} meses";
        }

        var days = at.DayNumber - birthDate.DayNumber;
        return days == 1 ? "1 día" : $"{days} días";
    }
}
