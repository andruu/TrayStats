namespace TrayStats.Services;

public interface IMonitorService : IDisposable
{
    void Start();
    void Stop();
    event Action? DataUpdated;
}
