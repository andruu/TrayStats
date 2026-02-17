using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public class BluetoothDeviceInfo
{
    public string Name { get; set; } = "";
    public int? BatteryPercent { get; set; }
    public bool IsConnected { get; set; }
    public string DeviceType { get; set; } = "Other";
    public string Icon { get; set; } = "\uE702";
}

public partial class BluetoothData : ObservableObject
{
    [ObservableProperty] private int _connectedCount;

    // Staged list built on background thread, applied to UI by ViewModel
    public List<BluetoothDeviceInfo> LatestDevices { get; set; } = new();
}
