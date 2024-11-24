namespace KolikkoControl.Web.Mqtt;

public interface IOutputPublisher
{
    Task PublishAsync(string topic, string message, CancellationToken ct);
}