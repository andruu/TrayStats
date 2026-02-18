using LibreHardwareMonitor.Hardware;
using TrayStats.Models;

namespace TrayStats.Services;

public sealed class GpuMonitorService : IMonitorService
{
    private const float SwitchThreshold = 10f;
    private const float IdleThreshold = 15f;

    private readonly HardwareContext _context;
    private IHardware? _currentGpu;

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

    private static int GpuPriority(HardwareType t) => t switch
    {
        HardwareType.GpuNvidia => 3,
        HardwareType.GpuAmd => 2,
        HardwareType.GpuIntel => 1,
        _ => 0
    };

    private static float GetGpuLoad(IHardware hw)
    {
        float coreLoad = 0f;
        float d3dLoad = 0f;
        try
        {
            foreach (var sensor in hw.Sensors)
            {
                if (sensor.Value is not { } val) continue;
                if (sensor.SensorType != SensorType.Load) continue;
                if (sensor.Name == "GPU Core")
                    coreLoad = val;
                else if (sensor.Name.StartsWith("D3D") && val > d3dLoad)
                    d3dLoad = val;
            }
        }
        catch { }
        return coreLoad > 0 ? coreLoad : d3dLoad;
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

        if (_currentGpu != null && !gpus.Contains(_currentGpu))
            _currentGpu = null;

        IHardware bestGpu;
        if (_currentGpu != null && gpus.Count > 1)
        {
            float currentLoad = GetGpuLoad(_currentGpu);

            IHardware? challenger = null;
            float challengerLoad = -1f;
            foreach (var hw in gpus)
            {
                if (ReferenceEquals(hw, _currentGpu)) continue;
                float load = GetGpuLoad(hw);
                if (load > challengerLoad)
                {
                    challenger = hw;
                    challengerLoad = load;
                }
            }

            bool shouldSwitch = false;
            if (challenger != null)
            {
                if (currentLoad < IdleThreshold && challengerLoad < IdleThreshold)
                    shouldSwitch = GpuPriority(challenger.HardwareType) > GpuPriority(_currentGpu.HardwareType);
                else
                    shouldSwitch = challengerLoad - currentLoad > SwitchThreshold;
            }

            bestGpu = shouldSwitch ? challenger! : _currentGpu;
        }
        else
        {
            IHardware? best = null;
            float bestLoad = -1f;
            foreach (var hw in gpus)
            {
                float load = GetGpuLoad(hw);
                bool isBetter = best == null
                    || load > bestLoad + SwitchThreshold
                    || (load < IdleThreshold && bestLoad < IdleThreshold && GpuPriority(hw.HardwareType) > GpuPriority(best.HardwareType));
                if (isBetter) { best = hw; bestLoad = load; }
            }
            bestGpu = best ?? gpus[0];
        }

        _currentGpu = bestGpu;

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
