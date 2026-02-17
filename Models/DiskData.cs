using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public partial class DriveData : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private double _totalGb;
    [ObservableProperty] private double _usedGb;
    [ObservableProperty] private double _freeGb;
    [ObservableProperty] private double _usagePercent;
    [ObservableProperty] private float _temperature;
    [ObservableProperty] private float _readRate;
    [ObservableProperty] private float _writeRate;
}

public partial class DiskData : ObservableObject
{
    [ObservableProperty] private double _totalUsagePercent;

    public List<DriveData> Drives { get; set; } = new();
}
