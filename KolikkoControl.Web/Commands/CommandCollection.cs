using KolikkoControl.Web.Input;
using KolikkoControl.Web.Output;

namespace KolikkoControl.Web.Commands;

public class CommandCollection(
    List<Command> commands,
    OutputBuffer outputBuffer,
    ILogger<GenericOsCommand> logger,
    InputBuffer inputBuffer
) : IDisposable
{
    /// <summary>
    /// Update command states according to state found in <see cref="InputBuffer"/>.
    /// </summary>
    public async Task UpdateAsync()
    {
        if (commands.Count == 0) logger.LogError("No commands. We doing nothing.");

        var state = inputBuffer.State;
        if (state.Equals(KolikkoState.Init))
        {
            logger.LogInformation("No message received. Waiting for initializing...");
            return;
        }

        foreach (var cmd in commands)
        {
            await cmd.Update(state.Equals(KolikkoState.On));
        }

        NotifyState();
    }

    void NotifyState()
    {
        var enabledCommands = commands.Where(c => c.Enabled).ToArray();
        if (enabledCommands.All(c => c.IsRunning))
            outputBuffer.Running(inputBuffer.State.ToString());
        else if (enabledCommands.All(c => !c.IsRunning))
            outputBuffer.NotRunning(inputBuffer.State.ToString());
        else
        {
            outputBuffer.Running(inputBuffer.State.ToString()); // at least something is running
            logger.LogError("Strange state mismatch: status...");
            LogStatus();
        }
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
        var commandDescriptions = commands.Select(c => c.ToString()).Aggregate((a, b) => $"{a}\n{b}");
        logger.LogDebug("{count} commands. Current state: {state}. Commands: \n{CommandText}", commands.Count,
            inputBuffer.State,
            commandDescriptions);
    }
}