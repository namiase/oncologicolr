namespace TrabajoSocial.Domain.Constants;

/// <summary>Identificadores fijos de la tabla Status (sembrados por el seeder).</summary>
public static class StatusIds
{
    public static readonly Guid Active = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid Inactive = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid Deleted = new("00000000-0000-0000-0000-000000000003");

    /// <summary>Estatus que el usuario puede asignar manualmente.</summary>
    public static readonly IReadOnlyList<Guid> Assignable = [Active, Inactive];
}
