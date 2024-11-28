using System.Net;
using System.Text;
using KolikkoControl.Web.Input;
using MQTTnet;
using MQTTnet.Client;

namespace KolikkoControl.Web.Mqtt;

public class MqttService(
    ILogger<MqttService> logger,
    InputBuffer inputBuffer,
    KolikkoMqttConfig config,
    KolikkoMqttClient mqttClientWrapper) : BackgroundService
{
    readonly CancellationTokenSource cancelSource = new();
    readonly MqttFactory mqttFactory = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttClient = mqttFactory.CreateMqttClient();
        var mqttClientOptions = CreateOptions();
        mqttClient.ApplicationMessageReceivedAsync += MessageReceived;
        await Subscribe(mqttClient, mqttClientOptions, cancelSource);
        mqttClientWrapper.Init(mqttClient); // this dependency is ugly, but how else neatly init it?
        await PollConnection(mqttClient, mqttClientOptions, stoppingToken);
    }

    async Task Subscribe(IMqttClient mqttClient, MqttClientOptions mqttClientOptions,
        CancellationTokenSource mqttCancelSource)
    {
        try
        {
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(b => b.WithAtLeastOnceQoS().WithTopic("/kolikko1/heat"))
                .Build();
            await mqttClient.ConnectAsync(mqttClientOptions, mqttCancelSource.Token);
            await mqttClient.SubscribeAsync(mqttSubscribeOptions, mqttCancelSource.Token);
            logger.LogDebug("MQTT client subscribed to topic.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "MQTT connect failed");
            throw;
        }
    }

    Task MessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.ApplicationMessage.PayloadSegment;
        var value = Encoding.UTF8.GetString(payload);
        logger.LogDebug("Received application message. {value}", value);
        try
        {
            inputBuffer.Handle(value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception in message handling." +
                                       "Message: {msg}. Topic: {topic}", value, e.ApplicationMessage.Topic);
            throw;
        }

        return Task.CompletedTask;
    }

    MqttClientOptions CreateOptions()
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer(config.ServerIp, config.Port)
            .WithCredentials(config.User, config.Password)
            .WithClientId(Dns.GetHostName())
            .Build();
    }

    async Task PollConnection(IMqttClient mqttClient,
        MqttClientOptions mqttClientOptions, CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(3);
        await Task.Delay(interval, stoppingToken); // dont rush at startup
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await mqttClient.TryPingAsync(cancellationToken: stoppingToken))
                {
                    logger.LogInformation("Ping failed. Reconnecting... ");
                    await Subscribe(mqttClient, CreateOptions(), cancelSource);
                    logger.LogInformation("... connection re-established.");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Observer heartbeat failed");
                // awaiting next ping...
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        cancelSource.Cancel();
        return base.StopAsync(cancellationToken);
    }
}

public class KolikkoMqttConfig
{
    public required string ServerIp { get; init; }
    public required int? Port { get; init; }
    public required string? Password { get; init; }
    public required string? User { get; init; }
}