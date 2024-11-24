using KolikkoControl.Web;
using KolikkoControl.Web.Commands;
using KolikkoControl.Web.Configs;
using KolikkoControl.Web.Input;
using KolikkoControl.Web.Mqtt;
using KolikkoControl.Web.Output;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddHostedService<CommandTimer>();
builder.Services.AddHostedService<MqttService>();
builder.Services.AddSingleton<KolikkoMqttClient>();
builder.Services.AddTransient<IOutputPublisher>(c => c.GetRequiredService<KolikkoMqttClient>());
builder.Services.AddHostedService<PublisherService>();
builder.Services.AddSingleton<InputBuffer>();
builder.Services.AddSingleton<OutputBuffer>();
builder.Services.AddSingleton<CommandCollection>();
builder.Services.AddSingleton<List<Command>>(c =>
    CommandParser.Parse(builder.Configuration, c.GetRequiredService<ILogger<GenericOsCommand>>()));
builder.Services.AddSingleton(new KolikkoMqttConfig
{
    Password = builder.Configuration["mqttpwd"],
    Port = int.Parse(builder.Configuration["mqttport"] ?? throw new InvalidOperationException()),
    User = builder.Configuration["mqttuser"],
    ServerIp = builder.Configuration["mqttserver"] ?? throw new InvalidOperationException()
});


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


var logger = app.Services.GetRequiredService<ILogger<GenericOsCommand>>();


app.Run();

namespace KolikkoControl.Web
{
    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}