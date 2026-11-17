namespace TrabajoSocial.Application.Common.Exceptions;

/// <summary>Excepciones de aplicación; el GlobalExceptionHandler las traduce a ProblemDetails.</summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>404 — recurso inexistente.</summary>
public sealed class NotFoundException(string message) : AppException(message);

/// <summary>409 — conflicto (p. ej. registro duplicado). <see cref="Field"/> indica el campo afectado.</summary>
public sealed class ConflictException(string message, string? field = null) : AppException(message)
{
    public string? Field { get; } = field;
}

/// <summary>400 — se incumple una regla de negocio.</summary>
public sealed class BusinessRuleException(string message) : AppException(message);

/// <summary>400 — errores de validación por campo (nombres en camelCase).</summary>
public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string message) : base(message)
        => Errors = new Dictionary<string, string[]> { [field] = [message] };

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Se encontraron errores de validación.")
        => Errors = errors;
}

/// <summary>401 — credenciales o sesión inválidas.</summary>
public sealed class AuthenticationFailedException(string message) : AppException(message);
