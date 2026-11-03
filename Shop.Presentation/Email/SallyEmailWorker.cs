using Microsoft.Extensions.Options;

namespace Shop.Presentation.Email;

// SMTP delivery also works when Hangfire is disabled. No schema is created at startup.
public sealed class SallyEmailWorker(IServiceScopeFactory scopes, IOptions<SallyMailOptions> options,
    ILogger<SallyEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (!options.Value.IsConfigured) continue;
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<SallyEmailDispatcher>().ProcessPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogWarning(ex, "Email outbox processing is unavailable; queued messages remain pending."); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
