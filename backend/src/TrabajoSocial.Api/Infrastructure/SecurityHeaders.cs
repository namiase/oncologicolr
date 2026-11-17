namespace TrabajoSocial.Api.Infrastructure;

/// <summary>
/// Cabeceras de seguridad en todas las respuestas del API. Se agregan al enviar la respuesta
/// (OnStarting), así también quedan en las respuestas de error.
/// </summary>
internal static class SecurityHeaders
{
    // El API solo devuelve JSON e imágenes: no necesita ejecutar ni cargar nada.
    private const string ApiCsp = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool isDevelopment)
        => app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var path = context.Request.Path;
                var headers = context.Response.Headers;

                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-site";

                // La documentación interactiva (solo en desarrollo) necesita sus propios scripts.
                var isDocs = isDevelopment && (path.StartsWithSegments("/scalar") || path.StartsWithSegments("/openapi"));
                if (!isDocs)
                {
                    headers.ContentSecurityPolicy = ApiCsp;
                }

                // Respuestas con datos personales o tokens: nunca guardarlas en caché.
                if (path.StartsWithSegments("/api"))
                {
                    headers.CacheControl = "no-store";
                    headers.Pragma = "no-cache";
                }

                return Task.CompletedTask;
            });

            await next();
        });
}
