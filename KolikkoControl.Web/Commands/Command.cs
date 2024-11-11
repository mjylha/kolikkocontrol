using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KolikkoControl.Web.Commands;

public abstract partial class Command(ILogger<GenericOsCommand> baseLogger) : IDisposable
{
    static readonly Regex ClearConsoleChars = ClearConsole();

    protected Process? Process;
    readonly object mutex = new();
    StreamWriter? output;
    StreamWriter? coloredOutput;

    protected void StoreOutput(string? data)
    {
        if (data == null) return;
        var clean = ClearConsoleChars.Replace(data, "");

        output?.WriteLine(clean);
        output?.Flush();

        coloredOutput?.WriteLine(data);
        coloredOutput?.Flush();
    }

    public bool IsRunning
    {
        get
        {
            try
            {
                return Process != null;
            }
            catch (Exception e)
            {
                baseLogger.LogInformation("Something went wrong"); //TODO remove me
                throw;
            }
        }
    }

    public abstract bool Enabled { get; }
    public abstract required string Exec { get; init; }
    public required string Wd { get; init; }

    public Task Handle(bool shouldRun)
    {
        lock (mutex)
        {
            if (shouldRun)
            {
                if (IsRunning) return Task.CompletedTask;
                Start();
            }
            else
            {
                if (!IsRunning) return Task.CompletedTask;
                Stop();
            }
        }

        return Task.CompletedTask;
    }

    protected abstract void LogDisabled();

    void Start()
    {
        if (!Enabled)
        {
            LogDisabled();
            return;
        }

        output = new StreamWriter(InitLogPath(".log."));
        coloredOutput = new StreamWriter(InitLogPath(".colorlog."));
        DoStart();
    }

    void Stop()
    {
        if (!IsRunning)
        {
            baseLogger.LogInformation("Stop called for a not running process {this}", ToString());
        }
        else
        {
            baseLogger.LogInformation("Killing process {this}", ToString());
        }

        Process?.Kill(true);
        Process?.Dispose();
        output?.Dispose();
        coloredOutput?.Dispose();
        output = null;
        coloredOutput = null;
        Process = null;
    }


    public override string ToString()
    {
        return Exec + " (" + (Process?.Id.ToString() ?? "stopped") + ")";
    }


    public void Dispose() => Stop();

    [GeneratedRegex(@"\x1B\[[^@-~]*[@-~]")]
    private static partial Regex ClearConsole();

    string InitLogPath(string extension)
    {
        string logPath;
        if (!string.IsNullOrEmpty(Wd))
        {
            logPath = Path.Combine(Wd, Exec + extension + DateTime.Now.Day);
        }
        else
        {
            logPath = "/tmp/" + Exec + extension + DateTime.Now.Day;
        }

        if (File.Exists(logPath)) File.Delete(logPath);
        return logPath;
    }

    protected abstract void DoStart();
}