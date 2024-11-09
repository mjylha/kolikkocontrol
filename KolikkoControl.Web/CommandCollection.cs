namespace KolikkoControl.Web;

public class CommandCollection(List<Command> commands)
{
    public static CommandCollection Init(IConfiguration appConfiguration, ILogger<Command> logger)
    {
        return new CommandCollection(Parse(appConfiguration, logger));
    }

    public void Handle(string command)
    {
        foreach (var cmd in commands)
        {
            cmd.Handle(command);
        }
    }

    private static List<Command> Parse(IConfiguration appConfiguration, ILogger<Command> logger)
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
}