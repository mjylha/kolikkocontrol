using System.Net;
using System.Text;
using KolikkoControl.Web;
using KolikkoControl.Web.Commands;
using KolikkoControl.Web.Configs;
using KolikkoControl.Web.Observer;
using MQTTnet;
using MQTTnet.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddHostedService<CommandTimer>();
builder.Services.AddHostedService<ObserverHeartbeat>();
builder.Services.AddSingleton<IStatusObserver, MqttStatusObserver>();
builder.Services.AddSingleton<CommandCollection>();
builder.Services.AddSingleton<List<Command>>(c =>
    CommandParser.Parse(builder.Configuration, c.GetRequiredService<ILogger<GenericOsCommand>>()));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

var serverIp = builder.Configuration["mqttserver"];
var port = Int32.Parse(builder.Configuration["mqttport"] ?? throw new InvalidOperationException());
var user = builder.Configuration["mqttuser"];
var password = builder.Configuration["mqttpwd"];

var mqttFactory = new MqttFactory();
var mqttClient = mqttFactory.CreateMqttClient();
var mqttClientOptions = new MqttClientOptionsBuilder()
    .WithTcpServer(serverIp, port)
    .WithCredentials(user, password)
    .WithClientId(Dns.GetHostName())
    .Build();

var logger = app.Services.GetRequiredService<ILogger<GenericOsCommand>>();
((MqttStatusObserver)app.Services.GetRequiredService<IStatusObserver>()).Init(mqttClient, mqttClientOptions);
var commands = app.Services.GetRequiredService<CommandCollection>();

mqttClient.ApplicationMessageReceivedAsync += e =>
{
    var payload = e.ApplicationMessage.PayloadSegment;
    var value = Encoding.UTF8.GetString(payload);
    logger.LogDebug("Received application message. {value}", value);
    try
    {
        // ReSharper disable once AccessToDisposedClosure
        commands.Handle(value);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Unhandled exception in message handling." +
                                   "Message: {msg}. Topic: {topic}", value, e.ApplicationMessage.Topic);
        throw;
    }

    return Task.CompletedTask;
};

mqttClient.DisconnectedAsync += async e =>
{
    if (e.ClientWasConnected)
    {
        await mqttClient.ConnectAsync(mqttClient.Options);
    }
};

var mqttCancelSource = new CancellationTokenSource();
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

app.Run();
mqttCancelSource.Cancel();

namespace KolikkoControl.Web
{
    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}