using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TrayStats.Models;
using TrayStats.Services;

namespace TrayStats.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private const int MaxDataPoints = 60;

    private readonly HardwareMonitorService _hwService;
    public HardwareMonitorService HardwareService => _hwService;
    private readonly NetworkMonitorService _netService;
    private readonly DiskMonitorService _diskService;
    private readonly WeatherService _weatherService;
    private readonly Dispatcher _dispatcher;

    // Sparkline data
    public ObservableCollection<double> CpuValues { get; } = new();
    public ObservableCollection<double> GpuValues { get; } = new();
    public ObservableCollection<double> RamValues { get; } = new();
    public ObservableCollection<double> NetDownValues { get; } = new();
    public ObservableCollection<double> NetUpValues { get; } = new();

    // Expose model objects
    public CpuData Cpu => _hwService.Cpu;
    public GpuData Gpu => _hwService.Gpu;
    public RamData Ram => _hwService.Ram;
    public NetData Net => _netService.Data;
    public DiskData Disk => _diskService.Data;
    public WeatherData Weather => _weatherService.Data;

    // Detail panel visibility
    [ObservableProperty] private bool _isWeatherDetailVisible;
    [ObservableProperty] private bool _isCpuDetailVisible;
    [ObservableProperty] private bool _isGpuDetailVisible;
    [ObservableProperty] private bool _isRamDetailVisible;
    [ObservableProperty] private bool _isDiskDetailVisible;
    [ObservableProperty] private bool _isNetDetailVisible;

    // Formatted summary strings
    [ObservableProperty] private string _cpuSummary = "0%";
    [ObservableProperty] private string _gpuSummary = "0%";
    [ObservableProperty] private string _ramSummary = "0 / 0 GB";
    [ObservableProperty] private string _diskSummary = "0%";
    [ObservableProperty] private string _netSummary = "-- / --";

    public DashboardViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;

        _hwService = new HardwareMonitorService();
        _netService = new NetworkMonitorService();
        _diskService = new DiskMonitorService(_hwService);
        _weatherService = new WeatherService();

        for (int i = 0; i < MaxDataPoints; i++)
        {
            CpuValues.Add(0);
            GpuValues.Add(0);
            RamValues.Add(0);
            NetDownValues.Add(0);
            NetUpValues.Add(0);
        }

        _hwService.DataUpdated += OnHardwareUpdated;
        _netService.DataUpdated += OnNetworkUpdated;
        _diskService.DataUpdated += OnDiskUpdated;
        _weatherService.DataUpdated += OnWeatherUpdated;

        _hwService.Start();
        _netService.Start();
        _diskService.Start();
        _weatherService.Start();
    }

    private void OnHardwareUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            PushValue(CpuValues, Cpu.TotalLoad);
            PushValue(GpuValues, Gpu.CoreLoad);
            PushValue(RamValues, Ram.Load);

            CpuSummary = $"{Cpu.TotalLoad:F0}%";
            GpuSummary = $"{Gpu.CoreLoad:F0}%";
            RamSummary = $"{Ram.UsedGb:F1} / {Ram.TotalGb:F1} GB";

            OnPropertyChanged(nameof(Cpu));
            OnPropertyChanged(nameof(Gpu));
            OnPropertyChanged(nameof(Ram));
        });
    }

    private void OnNetworkUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            PushValue(NetDownValues, Net.DownloadBytesPerSec / 1024.0);
            PushValue(NetUpValues, Net.UploadBytesPerSec / 1024.0);

            NetSummary = $"{Net.DownloadFormatted} / {Net.UploadFormatted}";
            OnPropertyChanged(nameof(Net));
        });
    }

    private void OnDiskUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            DiskSummary = $"{Disk.TotalUsagePercent:F0}% used";
            OnPropertyChanged(nameof(Disk));
        });
    }

    private static void PushValue(ObservableCollection<double> values, double newValue)
    {
        if (values.Count >= MaxDataPoints)
            values.RemoveAt(0);
        values.Add(newValue);
    }

    private void OnWeatherUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            OnPropertyChanged(nameof(Weather));
        });
    }

    [RelayCommand]
    private void ToggleWeatherDetail() => IsWeatherDetailVisible = !IsWeatherDetailVisible;

    [RelayCommand]
    private void ToggleCpuDetail() => IsCpuDetailVisible = !IsCpuDetailVisible;

    [RelayCommand]
    private void ToggleGpuDetail() => IsGpuDetailVisible = !IsGpuDetailVisible;

    [RelayCommand]
    private void ToggleRamDetail() => IsRamDetailVisible = !IsRamDetailVisible;

    [RelayCommand]
    private void ToggleDiskDetail() => IsDiskDetailVisible = !IsDiskDetailVisible;

    [RelayCommand]
    private void ToggleNetDetail() => IsNetDetailVisible = !IsNetDetailVisible;

    public void Dispose()
    {
        _hwService.DataUpdated -= OnHardwareUpdated;
        _netService.DataUpdated -= OnNetworkUpdated;
        _diskService.DataUpdated -= OnDiskUpdated;
        _weatherService.DataUpdated -= OnWeatherUpdated;

        _hwService.Dispose();
        _netService.Dispose();
        _diskService.Dispose();
        _weatherService.Dispose();
    }
}
