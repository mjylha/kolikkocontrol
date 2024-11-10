namespace KolikkoControl.Web;

public interface IStatusObserver
{
    public Task HaveProblem(string problem);
    public Task Running();
    public Task NotRunning();
}