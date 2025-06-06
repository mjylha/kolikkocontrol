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
    bool disableLogged;

    protected void StoreOutput(string? data)
    {
        if (data == null) return;
        var clean = ClearConsoleChars.Replace(data, "");

        output?.WriteLine(clean);
        output?.Flush();

        coloredOutput?.WriteLine(data);
        coloredOutput?.Flush();
    }

    public bool IsRunning => Process != null;

    public abstract bool Enabled { get; }
    public abstract required string Exec { get; init; }
    public required string Wd { get; init; }

    public Task Update(bool shouldRun)
    {
        try
        {
            if (IsDisabled()) return Task.CompletedTask;
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
        }
        catch (Exception e)
        {
            baseLogger.LogError(e, "Unhandled error in {cmd}", ToString());
        }

        return Task.CompletedTask;
    }

    bool IsDisabled()
    {
        return string.IsNullOrEmpty(Exec);
    }

    protected abstract void LogDisabled();

    void Start()
    {
        if (!Enabled)
        {
            if (disableLogged) return;
            LogDisabled();
            disableLogged = true;
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
        disableLogged = false;
    }


    public override string ToString()
    {
        double processorSecs = -1;
        string processId;

        if (Process == null)
        {
            processId = "un-initialized";
        }
        else if (Process.HasExited)
        {
            processId = "stopped";
        }
        else
        {
            processId = Process.Id.ToString();
            processorSecs = Process.TotalProcessorTime.TotalSeconds;
        }

        return Exec + " (" + processId + ", " + (int)processorSecs + ")";
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