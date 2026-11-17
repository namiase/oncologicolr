using Microsoft.AspNetCore.Mvc;
using TrabajoSocial.Application.Common.Security;

namespace TrabajoSocial.Api.Infrastructure;

/// <summary>
/// Mientras el usuario tenga una contraseña asignada por un administrador (claim "pwd_change"),
/// solo puede cambiarla, consultar su sesión o salir. Todo lo demás responde 403.
/// Se resuelve con el token, sin consultar la base de datos.
/// </summary>
internal static class RequirePasswordChange
{
    public const string Message = "Debe cambiar la contraseña asignada antes de continuar.";

    private static readonly PathString[] Allowed =
    [
        "/api/auth/change-password",
        "/api/auth/me",
        "/api/auth/refresh",
        "/api/auth/logout",
        "/api/settings/branding"
    ];

    public static IApplicationBuilder UseRequirePasswordChange(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            var mustChange = context.User.HasClaim(AppClaimTypes.MustChangePassword, "true");
            if (mustChange
                && context.Request.Path.StartsWithSegments("/api")
                && !Allowed.Any(path => context.Request.Path.StartsWithSegments(path)))
            {
                var problems = context.RequestServices.GetRequiredService<IProblemDetailsService>();
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Cambio de contraseña pendiente",
                        Detail = Message,
                        Extensions = { ["code"] = "password_change_required" }
                    }
                });
                return;
            }

            await next();
        });
}
