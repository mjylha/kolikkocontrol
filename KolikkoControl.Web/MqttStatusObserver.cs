using MQTTnet.Client;

namespace KolikkoControl.Web;

class MqttStatusObserver(IMqttClient mqttClient) : IStatusObserver
{
    public async Task HaveProblem(string problem)
    {
        await mqttClient.PublishStringAsync("/kolikko1/heat/statusmsg", problem);
    }

    public async Task Running()
    {
        await mqttClient.PublishStringAsync("/kolikko1/heat/status", "ON");
    }

    public async Task NotRunning()
    {
        await mqttClient.PublishStringAsync("/kolikko1/heat/status", "OFF");
    }
}