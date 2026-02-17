using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public partial class BatteryData : ObservableObject
{
    [ObservableProperty] private float _chargeLevel;
    [ObservableProperty] private bool _isCharging;
    [ObservableProperty] private bool _isPluggedIn;
    [ObservableProperty] private float _chargeDischargeRate;
    [ObservableProperty] private float _voltage;
    [ObservableProperty] private string _timeRemaining = "--";
    [ObservableProperty] private float _designedCapacity;
    [ObservableProperty] private float _fullChargeCapacity;
    [ObservableProperty] private float _batteryHealth;
    [ObservableProperty] private int _cycleCount;
    [ObservableProperty] private bool _hasBattery;
    [ObservableProperty] private string _statusText = "Unknown";
}
