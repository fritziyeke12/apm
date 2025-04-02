using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YourNamespace; // Ensure this points to where ApmOfflineLogger is defined

public class ApmLogRetryWorker : BackgroundService
{
    private readonly ILogger<ApmLogRetryWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public ApmLogRetryWorker(ILogger<ApmLogRetryWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Apm Log Retry Worker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Attempt to send queued transactions.
                await ApmOfflineLogger.RetrySendingQueuedTransactions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrying queued transactions.");
            }

            // Wait for the interval before trying again.
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
