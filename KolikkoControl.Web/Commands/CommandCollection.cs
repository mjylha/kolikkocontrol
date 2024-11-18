namespace KolikkoControl.Web.Commands;

public class CommandCollection(
    List<Command> commands,
    IStatusObserver observer,
    ILogger<GenericOsCommand> logger) : IDisposable
{
    readonly IEnumerable<string> allowedStates = ["ON", "OFF"];
    string message = "INIT";
    string previousMessage = "INIT";

    public void Handle(string msg)
    {
        message = msg.Trim();
    }

    public async Task HandleAsync()
    {
        if (commands.Count == 0) logger.LogError("No commands. We doing nothing.");

        if (message == "INIT")
        {
            logger.LogInformation("No message received. Waiting for initializing...");
            return;
        }

        var msg = message;
        if (!allowedStates.Contains(msg))
        {
            await ReportBadState(msg);
            return;
        }

        var shouldRun = msg == "ON";
        foreach (var cmd in commands)
        {
            await HandleMsg(cmd, shouldRun);
        }

        if (message != previousMessage)
            await NotifyState();

        previousMessage = message;
    }

    async Task HandleMsg(Command cmd, bool shouldRun)
    {
        try
        {
            await cmd.Handle(shouldRun);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled error in {cmd}", cmd);
        }
    }

    async Task ReportBadState(string msg)
    {
        logger.LogWarning("Unknown state {status}", msg);
        await observer.HaveProblem($"Unknown state {msg}");
    }

    async Task NotifyState()
    {
        var enabledCommands = commands.Where(c => c.Enabled).ToArray();
        if (enabledCommands.All(c => c.IsRunning))
            await observer.Running();
        if (enabledCommands.All(c => !c.IsRunning))
            await observer.NotRunning();
        else
            await observer.HaveProblem("Process mismatch.");
    }

    public void Dispose()
    {
        foreach (var command in commands)
        {
            command.Dispose();
        }
    }

    public void LogStatus()
    {
        logger.LogInformation("{count} commands. Current state: {state}. Commands:", commands.Count, message);
        foreach (var command in commands)
        {
            logger.LogInformation(command.ToString());
        }
    }
}