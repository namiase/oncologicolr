namespace TrabajoSocial.Domain.Common;

/// <summary>
/// Dato operativo que el sistema actualiza por su cuenta (p. ej. el último acceso o los
/// intentos fallidos de login). Si un cambio anónimo solo toca propiedades así marcadas,
/// no se registra como edición del registro (UpdatedAt / UpdatedBy no cambian).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotAuditedAttribute : Attribute;
