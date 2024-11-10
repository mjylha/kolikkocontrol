using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KolikkoControl.Web;

public partial class Command(ILogger<Command> logger) : IDisposable
{
    readonly object mutex = new();

    static readonly Regex ClearConsoleChars = ClearConsole();
    Process? process;
    public required string Wd { get; init; }
    public required string Exec { get; init; }
    public required string Args { get; init; }

    public bool IsRunning
    {
        get
        {
            try
            {
                return process != null;
            }
            catch (Exception e)
            {
                logger.LogInformation("Something went wrong"); //TODO remove me
                throw;
            }
        }
    }

    StreamWriter? output;
    StreamWriter? coloredOutput;

    void StoreOutput(string? data)
    {
        if (data == null) return;
        var clean = ClearConsoleChars.Replace(data, "");

        output?.WriteLine(clean);
        output?.Flush();

        coloredOutput?.WriteLine(data);
        coloredOutput?.Flush();
    }

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

    void Start()
    {
        if (IsRunning) throw new InvalidOperationException($"{this} Already running");

        var app = "";
        if (!string.IsNullOrEmpty(Wd)) app += Wd + "/";
        app += Exec;

        output = new StreamWriter(InitLogPath(".log."));
        coloredOutput = new StreamWriter(InitLogPath(".colorlog."));

        process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.Arguments = Args;
        process.StartInfo.FileName = app;
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, args) => StoreOutput(args.Data);
        var startStatus = process.Start();
        if (!startStatus) throw new Exception("Process did not start");
        process.BeginOutputReadLine();
        logger.LogInformation("Started process {this}", ToString());
    }

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

    void Stop()
    {
        if (!IsRunning)
        {
            logger.LogInformation("Stop called for a not running process {this}", ToString());
        }
        else
        {
            logger.LogInformation("Killing process {this}", ToString());
        }

        process?.Kill(true);
        process?.Dispose();
        output?.Dispose();
        coloredOutput?.Dispose();
        output = null;
        coloredOutput = null;
        process = null;
    }


    public override string ToString()
    {
        return Exec + " (" + (process?.Id.ToString() ?? "stopped") + ")";
    }

    [GeneratedRegex(@"\x1B\[[^@-~]*[@-~]")]
    private static partial Regex ClearConsole();

    public void Dispose() => Stop();
}