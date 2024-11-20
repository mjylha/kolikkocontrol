using MQTTnet.Client;

namespace KolikkoControl.Web.Observer;

class MqttStatusObserver(ILogger<MqttStatusObserver> logger) : IStatusObserver
{
    IMqttClient? mqttClient;
    MqttClientOptions? mqttClientOptions;

    public void Init(IMqttClient m, MqttClientOptions o)
    {
        mqttClient = m;
        mqttClientOptions = o;
    }

    public async Task HaveProblem(string problem)
    {
        AssertInitialized();
        await mqttClient.PublishStringAsync("/kolikko1/heat/statusmsg", problem);
    }

    void AssertInitialized()
    {
        if (mqttClient is null)
        {
            throw new Exception("mqttClient is null");
        }

        if (!mqttClient.IsConnected)
        {
            throw new Exception("mqttClient is not connected");
        }
    }

    public async Task Ping()
    {
        if (mqttClient is null) return; // cant ping yet
        
        if (!await mqttClient.TryPingAsync())
        {
            logger.LogInformation("Ping failed. Reconnecting... ");
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            logger.LogInformation("... connection re-establised.");
        }
    }

    public async Task Running()
    {
        AssertInitialized();
        await mqttClient.PublishStringAsync("/kolikko1/heat/status", "ON");
    }

    public async Task NotRunning()
    {
        AssertInitialized();
        await mqttClient.PublishStringAsync("/kolikko1/heat/status", "OFF");
    }
}