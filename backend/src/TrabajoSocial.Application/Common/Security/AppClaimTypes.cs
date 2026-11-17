namespace TrabajoSocial.Application.Common.Security;

/// <summary>Nombres de claims del JWT (sin mapeo a los tipos largos de WS-Federation).</summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Username = "unique_name";
    public const string FullName = "name";
    public const string Role = "role";
    public const string RoleId = "role_id";
    public const string SessionId = "sid";

    /// <summary>Presente ("true") mientras el usuario deba cambiar la contraseña que le asignaron.</summary>
    public const string MustChangePassword = "pwd_change";
}
