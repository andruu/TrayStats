using System.Runtime.InteropServices;
using TrayStats.Models;

namespace TrayStats.Services;

public sealed class RamMonitorService : IMonitorService
{
    private readonly HardwareContext _context;

    public RamData Data { get; } = new();
    public event Action? DataUpdated;

    public RamMonitorService(HardwareContext context)
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
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref mem))
        {
            Data.TotalGb = mem.ullTotalPhys / (1024f * 1024 * 1024);
            Data.AvailableGb = mem.ullAvailPhys / (1024f * 1024 * 1024);
            Data.UsedGb = Data.TotalGb - Data.AvailableGb;
            Data.Load = Data.TotalGb > 0 ? (Data.UsedGb / Data.TotalGb) * 100f : 0;
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

    public void Dispose() => Stop();
}
