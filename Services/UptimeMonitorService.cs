using System.Timers;
using TrayStats.Models;

namespace TrayStats.Services;

public sealed class UptimeMonitorService : IMonitorService
{
    private readonly System.Timers.Timer _timer;

    public UptimeData Data { get; } = new();
    public event Action? DataUpdated;

    public UptimeMonitorService()
    {
        _timer = new System.Timers.Timer(60000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;

        // Static info that doesn't change
        Data.MachineName = Environment.MachineName;
        Data.UserName = Environment.UserName;
        Data.OsVersion = $"Windows {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor} (Build {Environment.OSVersion.Version.Build})";
    }

    public void Start()
    {
        Update();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            Update();
            DataUpdated?.Invoke();
        }
        catch { }
    }

    private void Update()
    {
        var uptimeMs = Environment.TickCount64;
        var uptime = TimeSpan.FromMilliseconds(uptimeMs);
        Data.BootTime = DateTime.Now - uptime;

        int days = uptime.Days;
        int hours = uptime.Hours;
        int minutes = uptime.Minutes;

        Data.Uptime = days > 0
            ? $"{days}d {hours}h {minutes}m"
            : hours > 0
                ? $"{hours}h {minutes}m"
                : $"{minutes}m";

        DataUpdated?.Invoke();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
