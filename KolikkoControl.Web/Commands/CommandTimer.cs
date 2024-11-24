namespace KolikkoControl.Web.Commands;

/// <summary>
/// Periodically calls <see cref="CommandCollection"/> to do its job.
/// </summary>
/// <param name="logger"></param>
/// <param name="commandCollection"></param>
public class CommandTimer(ILogger<CommandTimer> logger, CommandCollection commandCollection) : BackgroundService
{
    bool initialized;

    DateTime lastLogged = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // dont rush at startup

        var interval = TimeSpan.FromSeconds(3);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!initialized)
            {
                logger.LogInformation("Starting first run.");
                initialized = true;
            }

            try
            {
                await commandCollection.UpdateAsync();
                if (DateTime.Now - lastLogged >= TimeSpan.FromMinutes(30))
                {
                    commandCollection.LogStatus();
                    lastLogged = DateTime.Now;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "unhandled exception handling a command.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}