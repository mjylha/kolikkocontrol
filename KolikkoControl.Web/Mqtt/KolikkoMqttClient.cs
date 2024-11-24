using MQTTnet.Client;
using MQTTnet.Protocol;

namespace KolikkoControl.Web.Mqtt;

public class KolikkoMqttClient(ILogger<KolikkoMqttClient> logger) : IOutputPublisher
{
    IMqttClient? client;

    public void Init(IMqttClient c)
    {
        client = c;
        logger.LogInformation("MqttiClient initialized.");
    }

    public async Task PublishAsync(string topic, string message, CancellationToken ct)
    {
        if (client is null)
        {
            throw new Exception("mqttClient is null");
        }

        if (!client.IsConnected)
        {
            throw new Exception("mqttClient is not connected");
        }

        await client.PublishStringAsync(topic, message, MqttQualityOfServiceLevel.AtLeastOnce,
            cancellationToken: ct);
    }
}