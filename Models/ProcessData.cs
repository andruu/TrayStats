using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public class ProcessInfo
{
    public string Name { get; set; } = "";
    public int Pid { get; set; }
    public double CpuPercent { get; set; }
    public double MemoryMb { get; set; }
    public double MemoryPercent { get; set; }
    public int InstanceCount { get; set; } = 1;
}

public partial class ProcessData : ObservableObject
{
    [ObservableProperty] private string _topConsumerName = "--";
    [ObservableProperty] private double _topConsumerCpu;

    // Staged list built on background thread, applied to UI by ViewModel
    public List<ProcessInfo> LatestSnapshot { get; set; } = new();
}
