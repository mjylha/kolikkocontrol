namespace KolikkoControl.Web.Observer;

public interface IStatusObserver
{
    public Task HaveProblem(string problem);
    public Task Running();
    public Task NotRunning();
    Task Ping();
}