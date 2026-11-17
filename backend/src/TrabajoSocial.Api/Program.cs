using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TrabajoSocial.Api.Authorization;
using TrabajoSocial.Api.Infrastructure;
using TrabajoSocial.Application;
using TrabajoSocial.Application.Auth;
using TrabajoSocial.Application.Common.Interfaces;
using TrabajoSocial.Application.Common.Security;
using TrabajoSocial.Infrastructure;
using TrabajoSocial.Infrastructure.Persistence;
using TrabajoSocial.Infrastructure.Persistence.Seed;
using TrabajoSocial.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ---- Capas ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddSingleton<IFileStorage, WebRootFileStorage>();
builder.Services.AddHostedService<SessionRevocationWarmup>();

// ---- API ----
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token obtenido en POST /api/auth/login"
    };
    document.Security ??= [];
    document.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    return Task.CompletedTask;
}));

// ---- Autenticación JWT ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.GetSigningKey(),
            ValidateLifetime = true,
            ClockSkew = JwtOptions.ClockSkew,
            NameClaimType = AppClaimTypes.Username,
            RoleClaimType = AppClaimTypes.Role
        };

        // Un token cuya sesión fue cerrada (logout, inactividad, robo) se rechaza aunque no haya vencido.
        // La verificación es en memoria: no consulta la base de datos en cada petición.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var revocations = context.HttpContext.RequestServices.GetRequiredService<ISessionRevocationStore>();
                var sid = context.Principal?.FindFirst(AppClaimTypes.SessionId)?.Value;
                if (!Guid.TryParse(sid, out var sessionId) || revocations.IsRevoked(sessionId))
                {
                    context.Fail("La sesión fue cerrada.");
                }

                return Task.CompletedTask;
            }
        };
    });

// ---- Autorización: por defecto todo exige sesión; los permisos finos usan [HasPermission] ----
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---- Rate limiting del login (por IP) ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Login, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// ---- CORS (solo si el frontend se sirve desde otro origen; en desarrollo se usa el proxy de Vite) ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

var app = builder.Build();

app.UseSecurityHeaders(app.Environment.IsDevelopment());
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options.WithTitle("TrabajoSocial API")).AllowAnonymous();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Archivos subidos (logo). Las cabeceras de seguridad (nosniff, CSP) las agrega UseSecurityHeaders.
app.UseStaticFiles();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequirePasswordChange();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

app.Run();

public partial class Program;
