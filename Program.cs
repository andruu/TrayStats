using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TrayStats;

public static class Program
{
    private const int MaxRestarts = 3;
    private const int RestartWindowSeconds = 60;
    private const string GpuPrefKey = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string GpuPrefValue = "GpuPreference=1;";

    [STAThread]
    public static void Main(string[] args)
    {
        if (EnsureIntegratedGpuPreference())
            return;

        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

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

    /// <summary>
    /// Registers this exe as "Power saving" (integrated GPU) in the Windows GPU
    /// preference registry. This is the same mechanism as Windows Settings > Display
    /// > Graphics. When the preference was not yet set, the process restarts so the
    /// OS applies it from the start (the preference is read at process creation).
    /// Returns true if the caller should exit (restart was triggered).
    /// </summary>
    private static bool EnsureIntegratedGpuPreference()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            using var key = Registry.CurrentUser.CreateSubKey(GpuPrefKey, writable: true);
            var current = key.GetValue(exePath) as string;

            if (string.Equals(current, GpuPrefValue, StringComparison.Ordinal))
                return false;

            key.SetValue(exePath, GpuPrefValue, RegistryValueKind.String);

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false
            });
            return true;
        }
        catch
        {
            return false;
        }
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
