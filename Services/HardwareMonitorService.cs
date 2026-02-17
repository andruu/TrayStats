using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Timers;
using LibreHardwareMonitor.Hardware;
using TrayStats.Models;

namespace TrayStats.Services;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly System.Timers.Timer _timer;
    private readonly UpdateVisitor _visitor = new();
    private bool _needsCpuFallback;

    public CpuData Cpu { get; } = new();
    public GpuData Gpu { get; } = new();
    public RamData Ram { get; } = new();

    public event Action? DataUpdated;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true
        };

        _computer.Open();
        _computer.Accept(_visitor);

        InitCpuCores();
        DetectCpuFallbackNeeded();

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    private void DetectCpuFallbackNeeded()
    {
        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Cpu) continue;
            foreach (var sensor in hw.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                    return; // LHM can read temps, no fallback needed
            }
        }
        _needsCpuFallback = true;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void InitCpuCores()
    {
        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType == HardwareType.Cpu)
            {
                hw.Update();
                Cpu.Name = hw.Name;

                int coreCount = 0;
                foreach (var sensor in hw.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load && sensor.Name.StartsWith("CPU Core #"))
                        coreCount++;
                }

                Cpu.CoreCount = coreCount > 0 ? coreCount : Environment.ProcessorCount;
                Cpu.ThreadCount = Environment.ProcessorCount;

                for (int i = 0; i < Cpu.CoreCount; i++)
                    Cpu.Cores.Add(new CpuCoreData { CoreIndex = i });

                break;
            }
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            try { _computer.Accept(_visitor); }
            catch { /* hardware topology changed, continue with stale data */ }

            UpdateCpu();
            if (_needsCpuFallback) UpdateCpuFallback();
            UpdateGpu();
            UpdateRam();
            DataUpdated?.Invoke();
        }
        catch
        {
            // Swallow sensor read errors
        }
    }

    private void UpdateCpu()
    {
        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Cpu) continue;

            float maxClock = 0;
            float fallbackTemp = 0;
            float fallbackPower = 0;

            foreach (var sensor in hw.Sensors)
            {
                if (sensor.Value is not { } val) continue;

                switch (sensor.SensorType)
                {
                    case SensorType.Load when sensor.Name == "CPU Total":
                        Cpu.TotalLoad = val;
                        break;
                    case SensorType.Load when sensor.Name.StartsWith("CPU Core #"):
                        if (TryParseCoreIndex(sensor.Name, out int coreIdx) && coreIdx < Cpu.Cores.Count)
                            Cpu.Cores[coreIdx].Usage = val;
                        break;

                    // Temperature: prefer Package/Average, fall back to any CPU temp
                    case SensorType.Temperature when sensor.Name.Contains("Package") || sensor.Name.Contains("Average"):
                        Cpu.Temperature = val;
                        break;
                    case SensorType.Temperature when sensor.Name.StartsWith("CPU Core #"):
                        if (TryParseCoreIndex(sensor.Name, out int tempIdx) && tempIdx < Cpu.Cores.Count)
                            Cpu.Cores[tempIdx].Temperature = val;
                        if (val > fallbackTemp) fallbackTemp = val;
                        break;
                    case SensorType.Temperature:
                        if (val > fallbackTemp) fallbackTemp = val;
                        break;

                    // Clock: track per-core and overall max
                    case SensorType.Clock when sensor.Name.StartsWith("CPU Core #"):
                        if (TryParseCoreIndex(sensor.Name, out int clkIdx) && clkIdx < Cpu.Cores.Count)
                            Cpu.Cores[clkIdx].Clock = val;
                        if (val > maxClock) maxClock = val;
                        break;
                    case SensorType.Clock when sensor.Name.Contains("Core"):
                        if (val > maxClock) maxClock = val;
                        break;

                    // Power: prefer Package, fall back to any CPU power
                    case SensorType.Power when sensor.Name.Contains("Package"):
                        Cpu.PackagePower = val;
                        break;
                    case SensorType.Power:
                        if (Cpu.PackagePower == 0 && val > fallbackPower)
                            fallbackPower = val;
                        break;
                }
            }

            if (maxClock > 0) Cpu.Clock = maxClock;
            if (Cpu.Temperature == 0 && fallbackTemp > 0) Cpu.Temperature = fallbackTemp;
            if (Cpu.PackagePower == 0 && fallbackPower > 0) Cpu.PackagePower = fallbackPower;

            break;
        }
    }

    private void UpdateCpuFallback()
    {
        try
        {
            // Fallback clock speed via WMI
            if (Cpu.Clock == 0)
            {
                using var searcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed, MaxClockSpeed FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    var current = Convert.ToSingle(obj["CurrentClockSpeed"]);
                    if (current > Cpu.Clock) Cpu.Clock = current;
                }
            }

            // Fallback temperature via WMI thermal zone (requires admin)
            if (Cpu.Temperature == 0)
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(@"root\WMI",
                        "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                    float maxTemp = 0;
                    foreach (var obj in searcher.Get())
                    {
                        var raw = Convert.ToSingle(obj["CurrentTemperature"]);
                        var celsius = (raw / 10f) - 273.15f;
                        if (celsius > maxTemp && celsius < 150) maxTemp = celsius;
                    }
                    if (maxTemp > 0) Cpu.Temperature = maxTemp;
                }
                catch
                {
                    // WMI thermal zone not available on this system
                }
            }

            // Fallback power via RAPL performance counters (not available via standard APIs)
            // No reliable fallback exists for CPU power without hardware-level access
        }
        catch
        {
            // Swallow WMI fallback errors
        }
    }

    private void UpdateGpu()
    {
        // Snapshot GPU hardware to avoid collection-modified issues
        var gpus = new List<IHardware>();
        try
        {
            foreach (var hw in _computer.Hardware)
            {
                if (hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
                    gpus.Add(hw);
            }
        }
        catch { return; }

        if (gpus.Count == 0) return;

        // Find the GPU with the highest activity
        IHardware? bestGpu = null;
        float bestLoad = -1f;

        foreach (var hw in gpus)
        {
            try
            {
                // Check "GPU Core" load first, then fall back to highest D3D load
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
            catch { /* sensor read failed for this GPU, skip */ }
        }

        if (bestGpu == null) return;

        try
        {
            Gpu.Name = bestGpu.Name;

            // Reset values so stale data from a previous GPU doesn't persist
            Gpu.CoreLoad = 0;
            Gpu.Temperature = 0;
            Gpu.CoreClock = 0;
            Gpu.MemoryClock = 0;
            Gpu.FanSpeed = 0;
            Gpu.FanPercent = 0;
            Gpu.MemoryUsed = 0;
            Gpu.MemoryTotal = 0;
            Gpu.MemoryLoad = 0;
            Gpu.Power = 0;

            float bestD3DLoad = 0;

            foreach (var sensor in bestGpu.Sensors)
            {
                if (sensor.Value is not { } val) continue;

                switch (sensor.SensorType)
                {
                    case SensorType.Load when sensor.Name == "GPU Core":
                        Gpu.CoreLoad = val;
                        break;
                    case SensorType.Load when sensor.Name.StartsWith("D3D"):
                        if (val > bestD3DLoad) bestD3DLoad = val;
                        break;
                    case SensorType.Temperature when sensor.Name.Contains("GPU"):
                        Gpu.Temperature = val;
                        break;
                    case SensorType.Clock when sensor.Name == "GPU Core":
                        Gpu.CoreClock = val;
                        break;
                    case SensorType.Clock when sensor.Name == "GPU Memory":
                        Gpu.MemoryClock = val;
                        break;
                    case SensorType.Fan:
                        if (Gpu.FanSpeed == 0 || val > 0)
                            Gpu.FanSpeed = val;
                        break;
                    case SensorType.Control:
                        if (Gpu.FanPercent == 0 || val > 0)
                            Gpu.FanPercent = val;
                        break;
                    case SensorType.SmallData when sensor.Name.Contains("Memory Used"):
                        if (val > Gpu.MemoryUsed) Gpu.MemoryUsed = val;
                        break;
                    case SensorType.SmallData when sensor.Name.Contains("Memory Total"):
                        if (val > Gpu.MemoryTotal) Gpu.MemoryTotal = val;
                        break;
                    case SensorType.Load when sensor.Name == "GPU Memory":
                        Gpu.MemoryLoad = val;
                        break;
                    case SensorType.Power:
                        Gpu.Power = val;
                        break;
                }
            }

            // If no "GPU Core" load, use the best D3D load as fallback
            if (Gpu.CoreLoad == 0 && bestD3DLoad > 0)
                Gpu.CoreLoad = bestD3DLoad;
        }
        catch { /* sensor read failed during update */ }
    }

    private void UpdateRam()
    {
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref mem))
        {
            Ram.TotalGb = mem.ullTotalPhys / (1024f * 1024 * 1024);
            Ram.AvailableGb = mem.ullAvailPhys / (1024f * 1024 * 1024);
            Ram.UsedGb = Ram.TotalGb - Ram.AvailableGb;
            Ram.Load = Ram.TotalGb > 0 ? (Ram.UsedGb / Ram.TotalGb) * 100f : 0;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    private static bool TryParseCoreIndex(string name, out int index)
    {
        index = 0;
        int hashPos = name.IndexOf('#');
        if (hashPos < 0 || hashPos + 1 >= name.Length) return false;

        var numSpan = name.AsSpan(hashPos + 1);
        int spacePos = numSpan.IndexOf(' ');
        if (spacePos > 0)
            numSpan = numSpan[..spacePos];

        if (int.TryParse(numSpan, out int parsed))
        {
            index = parsed - 1;
            return index >= 0;
        }
        return false;
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
