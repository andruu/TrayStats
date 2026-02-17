using LibreHardwareMonitor.Hardware;
using TrayStats.Models;

namespace TrayStats.Services;

public sealed class GpuMonitorService : IMonitorService
{
    private readonly HardwareContext _context;

    public GpuData Data { get; } = new();
    public event Action? DataUpdated;

    public GpuMonitorService(HardwareContext context)
    {
        _context = context;
    }

    public void Start() => _context.HardwareUpdated += OnHardwareUpdated;
    public void Stop() => _context.HardwareUpdated -= OnHardwareUpdated;

    private void OnHardwareUpdated()
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
        var hardware = _context.GetHardware();

        var gpus = new List<IHardware>();
        try
        {
            foreach (var hw in hardware)
            {
                if (hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
                    gpus.Add(hw);
            }
        }
        catch { return; }

        if (gpus.Count == 0) return;

        IHardware? bestGpu = null;
        float bestLoad = -1f;

        foreach (var hw in gpus)
        {
            try
            {
                float coreLoad = 0f;
                foreach (var sensor in hw.Sensors)
                {
                    if (sensor.Value is not { } val) continue;
                    if (sensor.SensorType == SensorType.Load)
                    {
                        if (sensor.Name == "GPU Core")
                        {
                            coreLoad = val;
                            break;
                        }
                        if (sensor.Name.StartsWith("D3D") && val > coreLoad)
                            coreLoad = val;
                    }
                }

                bool isBetter = false;
                if (bestGpu == null)
                    isBetter = true;
                else if (coreLoad > bestLoad)
                    isBetter = true;
                else if (coreLoad == bestLoad)
                {
                    int Priority(HardwareType t) => t switch
                    {
                        HardwareType.GpuNvidia => 3,
                        HardwareType.GpuAmd => 2,
                        HardwareType.GpuIntel => 1,
                        _ => 0
                    };
                    isBetter = Priority(hw.HardwareType) > Priority(bestGpu.HardwareType);
                }

                if (isBetter)
                {
                    bestGpu = hw;
                    bestLoad = coreLoad;
                }
            }
            catch { }
        }

        if (bestGpu == null) return;

        try
        {
            Data.Name = bestGpu.Name;

            Data.CoreLoad = 0;
            Data.Temperature = 0;
            Data.CoreClock = 0;
            Data.MemoryClock = 0;
            Data.FanSpeed = 0;
            Data.FanPercent = 0;
            Data.MemoryUsed = 0;
            Data.MemoryTotal = 0;
            Data.MemoryLoad = 0;
            Data.Power = 0;

            float bestD3DLoad = 0;

            foreach (var sensor in bestGpu.Sensors)
            {
                if (sensor.Value is not { } val) continue;

                switch (sensor.SensorType)
                {
                    case SensorType.Load when sensor.Name == "GPU Core":
                        Data.CoreLoad = val;
                        break;
                    case SensorType.Load when sensor.Name.StartsWith("D3D"):
                        if (val > bestD3DLoad) bestD3DLoad = val;
                        break;
                    case SensorType.Temperature when sensor.Name.Contains("GPU"):
                        Data.Temperature = val;
                        break;
                    case SensorType.Clock when sensor.Name == "GPU Core":
                        Data.CoreClock = val;
                        break;
                    case SensorType.Clock when sensor.Name == "GPU Memory":
                        Data.MemoryClock = val;
                        break;
                    case SensorType.Fan:
                        if (Data.FanSpeed == 0 || val > 0)
                            Data.FanSpeed = val;
                        break;
                    case SensorType.Control:
                        if (Data.FanPercent == 0 || val > 0)
                            Data.FanPercent = val;
                        break;
                    case SensorType.SmallData when sensor.Name.Contains("Memory Used"):
                        if (val > Data.MemoryUsed) Data.MemoryUsed = val;
                        break;
                    case SensorType.SmallData when sensor.Name.Contains("Memory Total"):
                        if (val > Data.MemoryTotal) Data.MemoryTotal = val;
                        break;
                    case SensorType.Load when sensor.Name == "GPU Memory":
                        Data.MemoryLoad = val;
                        break;
                    case SensorType.Power:
                        Data.Power = val;
                        break;
                }
            }

            if (Data.CoreLoad == 0 && bestD3DLoad > 0)
                Data.CoreLoad = bestD3DLoad;
        }
        catch { }
    }

    public void Dispose() => Stop();
}
