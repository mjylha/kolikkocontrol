using MQTTnet.Client;

namespace KolikkoControl.Web;

class MqttStatusObserver(ILogger<MqttStatusObserver> logger) : IStatusObserver
{
    IMqttClient? mqttClient;

    public void Init(IMqttClient m)
    {
        mqttClient = m;
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