using KolikkoControl.Web.Mqtt;

namespace KolikkoControl.Web.Output;

class PublisherService(ILogger<PublisherService> logger, OutputBuffer outputBuffer, IOutputPublisher outputPublisher)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(10);
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // dont rush at startup
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var msg = outputBuffer.GetMessage();
                if (msg is not null)
                {
                    await Publish(stoppingToken, msg);
                }

                var errorMsg = outputBuffer.GetErrorMessage();
                if (errorMsg is not null)
                {
                    await Publish(stoppingToken, errorMsg);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "MqttPublisherService failed");
                // awaiting next loop...
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    async Task Publish(CancellationToken stoppingToken, OutputBuffer.Message msg)
    {
        await outputPublisher.PublishAsync(msg.Topic, msg.Text, stoppingToken);
        logger.LogDebug("{topic}, {message}", msg.Topic, msg.Text);
    }
}