namespace KolikkoControl.Web;

public class CommandCollection(List<Command> commands, IStatusObserver observer, ILogger<Command> logger) : IDisposable
{
    readonly IEnumerable<string> allowedStates = ["ON", "OFF"];

    public static CommandCollection Init(
        IConfiguration appConfiguration,
        ILogger<Command> logger,
        IStatusObserver observer)
    {
        return new CommandCollection(Parse(appConfiguration, logger, observer), observer, logger);
    }

    public async Task Handle(string msg)
    {
        if (!allowedStates.Contains(msg.Trim()))
        {
            await ReportBadState(msg);
            return;
        }

        var shouldRun = msg.Trim() == "ON";
        foreach (var cmd in commands)
        {
            await HandleMsg(cmd, shouldRun);
        }

        await NotifyState();
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
        if (commands.All(c => c.IsRunning))
            await observer.Running();
        if (commands.All(c => !c.IsRunning))
            await observer.NotRunning();
        else
            await observer.HaveProblem("Only one process running.");
    }

    static List<Command> Parse(IConfiguration appConfiguration, ILogger<Command> logger, IStatusObserver observer)
    {
        return
        [
            new Command(logger)
            {
                Wd = appConfiguration["command1:wd"] ?? throw new InvalidOperationException(),
                Exec = appConfiguration["command1:exec"] ?? throw new InvalidOperationException(),
                Args = appConfiguration["command1:args"] ?? throw new InvalidOperationException(),
            },
            new Command(logger)
            {
                Wd = appConfiguration["command2:wd"] ?? throw new InvalidOperationException(),
                Exec = appConfiguration["command2:exec"] ?? throw new InvalidOperationException(),
                Args = appConfiguration["command2:args"] ?? throw new InvalidOperationException(),
            },
        ];
    }

    public void Dispose()
    {
        foreach (var command in commands)
        {
            command.Dispose();
        }
    }
}