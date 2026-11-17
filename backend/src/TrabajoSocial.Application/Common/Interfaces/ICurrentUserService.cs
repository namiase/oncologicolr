namespace TrabajoSocial.Application.Common.Interfaces;

/// <summary>Usuario autenticado de la petición actual (null fuera de una petición HTTP).</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    Guid? RoleId { get; }
}
