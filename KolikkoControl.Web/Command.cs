using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KolikkoControl.Web;

public partial class Command(ILogger<Command> logger)
{
    private static readonly Regex ClearConsoleChars = ClearConsole();
    private Process? process;
    public required string Wd { get; init; }
    public required string Exec { get; init; }
    public required string Args { get; init; }

    private bool IsRunning { get; set; }

    private StreamWriter? output;
    private StreamWriter? coloredOutput;

    void StoreOutput(string? data)
    {
        if (data == null) return;
        var clean = ClearConsoleChars.Replace(data, "");

        output?.WriteLine(clean);
        output?.Flush();

        coloredOutput?.WriteLine(data);
        coloredOutput?.Flush();
    }

    public void Handle(string status)
    {
        var shouldRun = status.Trim() == "1";
        if (shouldRun)
        {
            if (IsRunning) return; // ok
            Start();
        }
        else
        {
            if (!IsRunning) return; // ok
            Stop();
        }
    }

    private void Start()
    {
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
        process.OutputDataReceived += (sender, args) => StoreOutput(args.Data);
        process.Start();
        process.BeginOutputReadLine();
        logger.LogInformation($"Started process {Exec} with id {process.Id}");
        IsRunning = true;
    }

    private string InitLogPath(string extension)
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

    private void Stop()
    {
        logger.LogInformation($"Killing process {process?.Id}");
        process?.Kill(true);
        output?.Dispose();
        output = null;
        IsRunning = false;
    }


    [GeneratedRegex(@"\x1B\[[^@-~]*[@-~]")]
    private static partial Regex ClearConsole();
}