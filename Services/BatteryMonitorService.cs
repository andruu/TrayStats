using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using TrayStats.Models;

namespace TrayStats.Services;

public sealed class BatteryMonitorService : IMonitorService
{
    private readonly HardwareContext _context;

    public BatteryData Data { get; } = new();
    public event Action? DataUpdated;

    public BatteryMonitorService(HardwareContext context)
    {
        _context = context;
        DetectBattery();
    }

    public void Start() => _context.HardwareUpdated += OnHardwareUpdated;
    public void Stop() => _context.HardwareUpdated -= OnHardwareUpdated;

    private void OnHardwareUpdated()
    {
        if (!Data.HasBattery) return;

        try
        {
            Update();
            DataUpdated?.Invoke();
        }
        catch { }
    }

    private void DetectBattery()
    {
        foreach (var hw in _context.GetHardware())
        {
            if (hw.HardwareType == HardwareType.Battery)
            {
                Data.HasBattery = true;
                return;
            }
        }
        Data.HasBattery = false;
    }

    private void Update()
    {
        try
        {
            float? powerW = null;
            float? currentA = null;

            foreach (var hw in _context.GetHardware())
            {
                if (hw.HardwareType != HardwareType.Battery) continue;

                foreach (var sensor in hw.Sensors)
                {
                    if (sensor.Value is not { } val) continue;

                    switch (sensor.SensorType)
                    {
                        case SensorType.Level when sensor.Name.Contains("Charge"):
                            Data.ChargeLevel = val;
                            break;
                        case SensorType.Voltage:
                            Data.Voltage = val;
                            break;
                        case SensorType.Current:
                            currentA = val;
                            break;
                        case SensorType.Power:
                            powerW = val;
                            break;
                        case SensorType.Energy when sensor.Name.Contains("Designed"):
                            Data.DesignedCapacity = val;
                            break;
                        case SensorType.Energy when sensor.Name.Contains("Charged") || sensor.Name.Contains("Full Charge") || sensor.Name.Contains("FullCharge"):
                            Data.FullChargeCapacity = val;
                            break;
                    }
                }
                break;
            }

            if (powerW.HasValue)
                Data.ChargeDischargeRate = powerW.Value;
            else if (currentA.HasValue && Data.Voltage > 0)
                Data.ChargeDischargeRate = currentA.Value * Data.Voltage;
            else if (currentA.HasValue)
                Data.ChargeDischargeRate = currentA.Value;

            var status = new SYSTEM_POWER_STATUS();
            if (GetSystemPowerStatus(ref status))
            {
                Data.IsPluggedIn = status.ACLineStatus == 1;
                Data.IsCharging = (status.BatteryFlag & 8) != 0;

                if (Data.ChargeLevel == 0 && status.BatteryLifePercent != 255)
                    Data.ChargeLevel = status.BatteryLifePercent;

                if (Data.IsPluggedIn && Data.ChargeLevel >= 99.5f)
                {
                    Data.TimeRemaining = "Fully charged";
                    Data.StatusText = "Full";
                }
                else if (Data.IsCharging)
                {
                    Data.StatusText = "Charging";
                    if (Data.ChargeDischargeRate > 0 && Data.FullChargeCapacity > 0 && Data.ChargeLevel < 100)
                    {
                        float remainingMwh = Data.FullChargeCapacity * (100f - Data.ChargeLevel) / 100f;
                        float hoursToFull = remainingMwh / (Data.ChargeDischargeRate * 1000f);
                        int totalMin = (int)(hoursToFull * 60);
                        if (totalMin > 0 && totalMin < 1440)
                        {
                            int hours = totalMin / 60;
                            int minutes = totalMin % 60;
                            Data.TimeRemaining = hours > 0 ? $"{hours}h {minutes}m to full" : $"{minutes}m to full";
                        }
                        else
                        {
                            Data.TimeRemaining = "Calculating...";
                        }
                    }
                    else
                    {
                        Data.TimeRemaining = "Calculating...";
                    }
                }
                else if (status.BatteryLifeTime != -1 && status.BatteryLifeTime > 0)
                {
                    int totalSec = status.BatteryLifeTime;
                    int hours = totalSec / 3600;
                    int minutes = (totalSec % 3600) / 60;
                    Data.TimeRemaining = hours > 0 ? $"{hours}h {minutes}m remaining" : $"{minutes}m remaining";
                    Data.StatusText = "Discharging";
                }
                else
                {
                    Data.TimeRemaining = Data.IsPluggedIn ? "Calculating..." : "Estimating...";
                    Data.StatusText = Data.IsPluggedIn ? "Plugged in" : "On battery";
                }
            }

            if (Data.DesignedCapacity > 0 && Data.FullChargeCapacity > 0)
                Data.BatteryHealth = (Data.FullChargeCapacity / Data.DesignedCapacity) * 100f;
        }
        catch { }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    public void Dispose() => Stop();
}
