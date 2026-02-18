using System.Diagnostics;
using System.IO;

namespace TrayStats;

public static class Program
{
    private const int MaxRestarts = 3;
    private const int RestartWindowSeconds = 60;

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            RestartSelf(args);
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogCrash(ex);

        if (e.IsTerminating)
            RestartSelf([]);
    }

    private static void RestartSelf(string[] args)
    {
        try
        {
            var countFile = Path.Combine(AppContext.BaseDirectory, ".restart_count");
            int count = 0;

            if (File.Exists(countFile))
            {
                var parts = File.ReadAllText(countFile).Split('|');
                if (parts.Length == 2
                    && int.TryParse(parts[0], out int prev)
                    && long.TryParse(parts[1], out long ticks)
                    && (DateTime.UtcNow - new DateTime(ticks)).TotalSeconds < RestartWindowSeconds)
                {
                    count = prev;
                }
            }

            count++;
            File.WriteAllText(countFile, $"{count}|{DateTime.UtcNow.Ticks}");

            if (count > MaxRestarts)
                return;

            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false
                });
            }
        }
        catch { }
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, entry);
        }
        catch { }
    }
}
