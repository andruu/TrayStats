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

    private readonly HardwareContext _hwContext;
    private readonly CpuMonitorService _cpuService;
    private readonly GpuMonitorService _gpuService;
    private readonly RamMonitorService _ramService;
    private readonly BatteryMonitorService _batteryService;
    private readonly NetworkMonitorService _netService;
    private readonly DiskMonitorService _diskService;
    private readonly WeatherService _weatherService;
    private readonly Dispatcher _dispatcher;

    public HardwareContext HardwareContext => _hwContext;

    // Sparkline data
    public List<double> CpuValues { get; } = new();
    public List<double> GpuValues { get; } = new();
    public List<double> RamValues { get; } = new();
    public List<double> NetDownValues { get; } = new();
    public List<double> NetUpValues { get; } = new();
    public List<double> BatteryValues { get; } = new();

    // Expose model objects
    public CpuData Cpu => _cpuService.Data;
    public GpuData Gpu => _gpuService.Data;
    public RamData Ram => _ramService.Data;
    public BatteryData Battery => _batteryService.Data;
    public NetData Net => _netService.Data;
    public DiskData Disk => _diskService.Data;
    public WeatherData Weather => _weatherService.Data;

    // Section visibility
    public SectionVisibility Sections { get; } = new();

    // Detail panel visibility
    [ObservableProperty] private bool _isWeatherDetailVisible;
    [ObservableProperty] private bool _isCpuDetailVisible;
    [ObservableProperty] private bool _isGpuDetailVisible;
    [ObservableProperty] private bool _isRamDetailVisible;
    [ObservableProperty] private bool _isDiskDetailVisible;
    [ObservableProperty] private bool _isBatteryDetailVisible;
    [ObservableProperty] private bool _isNetDetailVisible;

    // Formatted summary strings
    [ObservableProperty] private string _cpuSummary = "0%";
    [ObservableProperty] private string _gpuSummary = "0%";
    [ObservableProperty] private string _ramSummary = "0 / 0 GB";
    [ObservableProperty] private string _diskSummary = "0%";
    [ObservableProperty] private string _batterySummary = "--";
    [ObservableProperty] private string _netSummary = "-- / --";

    public DashboardViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;

        _hwContext = new HardwareContext();
        _cpuService = new CpuMonitorService(_hwContext);
        _gpuService = new GpuMonitorService(_hwContext);
        _ramService = new RamMonitorService(_hwContext);
        _batteryService = new BatteryMonitorService(_hwContext);
        _netService = new NetworkMonitorService();
        _diskService = new DiskMonitorService(_hwContext);
        _weatherService = new WeatherService();

        for (int i = 0; i < MaxDataPoints; i++)
        {
            CpuValues.Add(0);
            GpuValues.Add(0);
            RamValues.Add(0);
            NetDownValues.Add(0);
            NetUpValues.Add(0);
            BatteryValues.Add(0);
        }

        _cpuService.DataUpdated += OnCpuUpdated;
        _gpuService.DataUpdated += OnGpuUpdated;
        _ramService.DataUpdated += OnRamUpdated;
        _batteryService.DataUpdated += OnBatteryUpdated;
        _netService.DataUpdated += OnNetworkUpdated;
        _diskService.DataUpdated += OnDiskUpdated;
        _weatherService.DataUpdated += OnWeatherUpdated;

        _hwContext.Start();
        _cpuService.Start();
        _gpuService.Start();
        _ramService.Start();
        _batteryService.Start();
        _netService.Start();
        _diskService.Start();
        _weatherService.Start();
    }

    private void OnCpuUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            PushValue(CpuValues, Cpu.TotalLoad);
            CpuSummary = $"{Cpu.TotalLoad:F0}%";
            OnPropertyChanged(nameof(Cpu));
            InvalidateCharts?.Invoke();
        });
    }

    private void OnGpuUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            PushValue(GpuValues, Gpu.CoreLoad);
            GpuSummary = $"{Gpu.CoreLoad:F0}%";
            OnPropertyChanged(nameof(Gpu));
            InvalidateCharts?.Invoke();
        });
    }

    private void OnRamUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            PushValue(RamValues, Ram.Load);
            RamSummary = $"{Ram.UsedGb:F1} / {Ram.TotalGb:F1} GB";
            OnPropertyChanged(nameof(Ram));
            InvalidateCharts?.Invoke();
        });
    }

    private void OnBatteryUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            PushValue(BatteryValues, Battery.ChargeLevel);
            BatterySummary = $"{Battery.ChargeLevel:F0}%";
            OnPropertyChanged(nameof(Battery));
            InvalidateCharts?.Invoke();
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
            InvalidateCharts?.Invoke();
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

    private void OnWeatherUpdated()
    {
        _dispatcher.BeginInvoke(() =>
        {
            OnPropertyChanged(nameof(Weather));
        });
    }

    public event Action? InvalidateCharts;

    private static void PushValue(List<double> values, double newValue)
    {
        if (values.Count >= MaxDataPoints)
            values.RemoveAt(0);
        values.Add(newValue);
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
    private void ToggleBatteryDetail() => IsBatteryDetailVisible = !IsBatteryDetailVisible;

    [RelayCommand]
    private void ToggleNetDetail() => IsNetDetailVisible = !IsNetDetailVisible;

    public void Dispose()
    {
        _cpuService.Dispose();
        _gpuService.Dispose();
        _ramService.Dispose();
        _batteryService.Dispose();
        _netService.Dispose();
        _diskService.Dispose();
        _weatherService.Dispose();
        _hwContext.Dispose();
    }
}
