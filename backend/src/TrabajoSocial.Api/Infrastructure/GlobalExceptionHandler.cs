using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TrabajoSocial.Application.Common.Exceptions;

namespace TrabajoSocial.Api.Infrastructure;

/// <summary>Traduce las excepciones a respuestas ProblemDetails (RFC 9457).</summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var problem = exception switch
        {
            ValidationException ve => new ValidationProblemDetails(ve.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos inválidos",
                Detail = ve.Errors.Count == 1 ? ve.Errors.First().Value.FirstOrDefault() : ve.Message
            },
            BusinessRuleException => Problem(StatusCodes.Status400BadRequest, "Operación no permitida", exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "No encontrado", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Registro duplicado", exception.Message),
            AuthenticationFailedException => Problem(StatusCodes.Status401Unauthorized, "No autorizado", exception.Message),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                Problem(StatusCodes.Status409Conflict, "Registro duplicado", "Ya existe un registro con esos datos."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Error interno",
                environment.IsDevelopment() ? exception.Message : "Ocurrió un error inesperado. Intente nuevamente.")
        };

        if (exception is ConflictException { Field: { } field })
        {
            problem.Extensions["field"] = field;
        }

        if (problem.Status >= 500)
        {
            logger.LogError(exception, "Error no controlado procesando {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail)
        => new() { Status = status, Title = title, Detail = detail };
}
