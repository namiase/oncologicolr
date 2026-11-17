using TrabajoSocial.Application.Auth;

namespace TrabajoSocial.Api.Infrastructure;

/// <summary>
/// Al arrancar, recarga en memoria las sesiones cerradas recientemente para que un reinicio
/// del API no vuelva a aceptar sus access tokens. Se ejecuta antes de que el servidor atienda
/// peticiones; si la base de datos no responde, reintenta y, si no lo logra, el API no arranca
/// (arrancar con la lista vacía "reviviría" sesiones cerradas).
/// </summary>
internal sealed class SessionRevocationWarmup(IServiceProvider services, ILogger<SessionRevocationWarmup> logger)
    : IHostedService
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20)];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var count = await scope.ServiceProvider.GetRequiredService<SessionRevocationLoader>().LoadAsync(cancellationToken);
                logger.LogInformation("Sesiones revocadas recargadas en memoria: {Count}.", count);
                return;
            }
            catch (Exception ex) when (attempt < RetryDelays.Length && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "No se pudieron recargar las sesiones revocadas (intento {Attempt}). Reintentando en {Delay}.",
                    attempt + 1, RetryDelays[attempt]);
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
