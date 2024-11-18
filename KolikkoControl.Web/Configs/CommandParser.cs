using KolikkoControl.Web.Commands;

namespace KolikkoControl.Web.Configs;

public static class CommandParser
{
    public static List<Command> Parse(IConfiguration appConfiguration,
        ILogger<GenericOsCommand> logger)
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

}