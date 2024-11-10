using System.Text;
using KolikkoControl.Web;
using MQTTnet;
using MQTTnet.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
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


var serverIp = app.Configuration["mqttserver"];
var port = Int32.Parse(app.Configuration["mqttport"] ?? throw new InvalidOperationException());
var user = app.Configuration["mqttuser"];
var password = app.Configuration["mqttpwd"];

var mqttFactory = new MqttFactory();
var mqttClient = mqttFactory.CreateMqttClient();
var mqttClientOptions = new MqttClientOptionsBuilder()
    .WithTcpServer(serverIp, port)
    .WithCredentials(user, password)
    .Build();


var logger = app.Services.GetRequiredService<ILogger<Command>>();
var observer = new MqttStatusObserver(mqttClient);
using var commands = CommandCollection.Init(app.Configuration, logger, observer);

mqttClient.ApplicationMessageReceivedAsync += async e =>
{
    Console.WriteLine("Received application message.");
    var payload = e.ApplicationMessage.PayloadSegment;
    var value = Encoding.UTF8.GetString(payload);
    try
    {
        // ReSharper disable once AccessToDisposedClosure - should not cause too many problems...
        await commands.Handle(value);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Unhandled exception in message handling." +
                                   "Message: {msg}. Topic: {topic}", value, e.ApplicationMessage.Topic);
    }
};

await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
    .WithTopicFilter(b => b.WithAtLeastOnceQoS().WithTopic("/kolikko1/heat"))
    .Build();

await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

Console.WriteLine("MQTT client subscribed to topic.");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}