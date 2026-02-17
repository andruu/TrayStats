using System.Timers;
using LibreHardwareMonitor.Hardware;
using TrayStats.Models;

namespace TrayStats.Services;

public sealed class HardwareContext : IDisposable
{
    private readonly Computer _computer;
    private readonly System.Timers.Timer _timer;
    private readonly UpdateVisitor _visitor = new();
    private int _isUpdating;
    private int _tickCount;

    public event Action? HardwareUpdated;

    public int TickCount => _tickCount;

    public HardwareContext()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true
        };

        _computer.Open();
        _computer.Accept(_visitor);

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    public IReadOnlyList<IHardware> GetHardware()
    {
        try { return _computer.Hardware.ToList(); }
        catch { return []; }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _isUpdating, 1, 0) != 0)
            return;

        try
        {
            try { _computer.Accept(_visitor); }
            catch { /* hardware topology changed, continue with stale data */ }

            Interlocked.Increment(ref _tickCount);
            HardwareUpdated?.Invoke();
        }
        catch
        {
            // Swallow sensor read errors
        }
        finally
        {
            Interlocked.Exchange(ref _isUpdating, 0);
        }
    }

    public string DumpAllSensors()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var hw in _computer.Hardware)
        {
            sb.AppendLine($"[{hw.HardwareType}] {hw.Name}");
            foreach (var sensor in hw.Sensors)
            {
                sb.AppendLine($"  [{sensor.SensorType}] {sensor.Name} (idx:{sensor.Index}) = {sensor.Value}");
            }
            foreach (var sub in hw.SubHardware)
            {
                sb.AppendLine($"  SubHW: [{sub.HardwareType}] {sub.Name}");
                foreach (var sensor in sub.Sensors)
                {
                    sb.AppendLine($"    [{sensor.SensorType}] {sensor.Name} (idx:{sensor.Index}) = {sensor.Value}");
                }
            }
        }
        return sb.Length > 0 ? sb.ToString() : "No hardware found";
    }

    public List<DriveData> GetStorageData()
    {
        var drives = new List<DriveData>();
        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Storage) continue;

            var drive = new DriveData { Name = hw.Name };
            foreach (var sensor in hw.Sensors)
            {
                if (sensor.Value is not { } val) continue;
                switch (sensor.SensorType)
                {
                    case SensorType.Temperature when sensor.Index == 0:
                        drive.Temperature = val;
                        break;
                    case SensorType.Throughput when sensor.Name.Contains("Read"):
                        drive.ReadRate = val;
                        break;
                    case SensorType.Throughput when sensor.Name.Contains("Write"):
                        drive.WriteRate = val;
                        break;
                }
            }
            drives.Add(drive);
        }
        return drives;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _computer.Close();
    }
}

internal sealed class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware)
            sub.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}
