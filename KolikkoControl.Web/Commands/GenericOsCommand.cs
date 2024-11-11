using System.Diagnostics;

namespace KolikkoControl.Web.Commands;

public class GenericOsCommand(ILogger<GenericOsCommand> l) : Command(l)
{
    readonly ILogger<GenericOsCommand> logger = l;

    public override bool Enabled => !AvoidOfficeHours || !IsOfficeHour();

    protected override void LogDisabled()
    {
        logger.LogInformation("Disabled because its office hour. {this}", this);
    }

    bool IsOfficeHour()
    {
        var hour = DateTime.Now.Hour;
        return hour is >= 7 and <= 19;
    }

    public override required string Exec { get; init; }
    public required string Args { get; init; }
    public required bool AvoidOfficeHours { get; init; }

    protected override void DoStart()
    {
        if (IsRunning) throw new InvalidOperationException($"{this} Already running");
        logger.LogInformation("Starting process {exec}...", Exec);

        var app = "";
        if (!string.IsNullOrEmpty(Wd)) app += Wd + "/";
        app += Exec;

        Process = new Process();
        Process.StartInfo.UseShellExecute = false;
        Process.StartInfo.RedirectStandardOutput = true;
        Process.StartInfo.RedirectStandardError = true;
        Process.StartInfo.Arguments = Args;
        Process.StartInfo.FileName = app;
        Process.EnableRaisingEvents = true;
        Process.OutputDataReceived += (_, args) => StoreOutput(args.Data);
        var startStatus = Process.Start();
        if (!startStatus) throw new Exception("Process did not start");
        Process.BeginOutputReadLine();
        logger.LogInformation("Started process {this}", ToString());
    }
}