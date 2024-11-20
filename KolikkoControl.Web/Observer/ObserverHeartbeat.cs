namespace KolikkoControl.Web.Observer;

public class ObserverHeartbeat(ILogger<ObserverHeartbeat> logger, IStatusObserver observer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(3);
        await Task.Delay(interval, stoppingToken); // dont rush at startup
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await observer.Ping();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Observer heartbeat failed");
                // awaiting next ping...
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}