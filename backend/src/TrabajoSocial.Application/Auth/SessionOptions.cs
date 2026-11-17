namespace TrabajoSocial.Application.Auth;

/// <summary>Política de sesión (sección "Session" de appsettings).</summary>
public sealed class SessionOptions
{
    public const string SectionName = "Session";

    /// <summary>Minutos sin actividad tras los cuales la sesión se cierra.</summary>
    public int IdleTimeoutMinutes { get; set; } = 30;

    /// <summary>Duración máxima de una sesión aunque haya actividad (luego se pide login de nuevo).</summary>
    public int AbsoluteTimeoutHours { get; set; } = 12;

    public TimeSpan IdleTimeout => TimeSpan.FromMinutes(IdleTimeoutMinutes);
    public TimeSpan AbsoluteTimeout => TimeSpan.FromHours(AbsoluteTimeoutHours);
}

/// <summary>Datos del cliente que inicia sesión (auditoría de sesiones).</summary>
public sealed record ClientInfo(string? IpAddress, string? UserAgent);
