namespace KolikkoControl.Web.Commands;

public class CommandCollection(
    List<Command> commands,
    IStatusObserver observer,
    ILogger<GenericOsCommand> logger) : IDisposable
{
    readonly IEnumerable<string> allowedStates = ["ON", "OFF"];

    public static CommandCollection Init(
        IConfiguration appConfiguration,
        ILogger<GenericOsCommand> logger,
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
        var enabledCommands = commands.Where(c => c.Enabled).ToArray();
        if (enabledCommands.All(c => c.IsRunning))
            await observer.Running();
        if (enabledCommands.All(c => !c.IsRunning))
            await observer.NotRunning();
        else
            await observer.HaveProblem("Process mismatch.");
    }

    static List<Command> Parse(IConfiguration appConfiguration,
        ILogger<GenericOsCommand> logger, IStatusObserver observer)
    {
        var conf1 = appConfiguration.GetSection("command1");
        var conf2 = appConfiguration.GetSection("command2");
        var first = ParseGeneric(logger, conf1);
        var second = ParseGeneric(logger, conf2);
        return [first, second];
    }

    static Command ParseGeneric(ILogger<GenericOsCommand> logger, IConfigurationSection conf)
    {
        return new GenericOsCommand(logger)
        {
            Wd = conf["wd"] ?? throw new InvalidOperationException(),
            Exec = conf["exec"] ?? throw new InvalidOperationException(),
            Args = conf["args"] ?? throw new InvalidOperationException(),
            AvoidOfficeHours = conf["avoid-office-hours"] == "true"
        };
    }

    public void Dispose()
    {
        foreach (var command in commands)
        {
            command.Dispose();
        }
    }
}